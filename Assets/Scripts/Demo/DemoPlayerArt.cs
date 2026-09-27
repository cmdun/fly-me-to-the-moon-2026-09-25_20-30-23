using UnityEngine;
using UnityEngine.InputSystem;

namespace FlyMeToTheMoon.Demo
{
    /// <summary>Feet stay anchored to local gravity; equipment eases between carried and played poses.</summary>
    public sealed class DemoPlayerArt : MonoBehaviour
    {
        public SpriteRenderer Traveler { get; private set; }
        public SpriteRenderer Instrument { get; private set; }
        public string Pose => Traveler.sprite.name;
        public Transform Piano { get; private set; }
        DemoGame game;
        float actionUntil, landingUntil, walkClock, actionStarted;
        bool grounded, facingLeft;
        Vector2 aim;
        readonly PrototypeShape[] pianoKeys = new PrototypeShape[7];
        int note;
        public void Build(DemoGame owner)
        {
            game = owner; PixelArtLibrary.HideMeshes(transform);
            Traveler = PixelArtLibrary.Create(transform, "Traveler pixel art", "hero/0", 20);
            Traveler.transform.localPosition = new Vector3(0, -.29f, -.2f);
            Instrument = PixelArtLibrary.Create(transform, "Orbiting flute", "flute", 21);
            grounded = game.Player.IsGrounded;
            Piano = new GameObject("Companion pocket piano").transform; Piano.SetParent(transform, false);
            var frame = DemoWorld.Shape(Piano, "Brass edged case", Vector2.zero, new Vector2(.92f,.29f), new Color(.28f,.12f,.045f), game.Material);
            frame.transform.localPosition = Vector3.zero; frame.GetComponent<MeshRenderer>().sortingOrder = 21;
            for (int i=0;i<7;i++)
            {
                pianoKeys[i] = DemoWorld.Shape(Piano, "Ivory key", Vector2.zero, new Vector2(.105f,.21f), new Color(.95f,.9f,.74f), game.Material);
                pianoKeys[i].transform.localPosition = new Vector3((i-3)*.12f,-.005f,-.02f);
                pianoKeys[i].GetComponent<MeshRenderer>().sortingOrder = 22;
                if (i==2 || i==6) continue;
                var black = DemoWorld.Shape(Piano, "Ebony key", Vector2.zero,new Vector2(.045f,.12f),new Color(.006f,.009f,.018f),game.Material);
                black.transform.localPosition = new Vector3((i-2.5f)*.12f,.04f,-.04f); black.GetComponent<MeshRenderer>().sortingOrder = 23;
            }
            Piano.localScale=Vector3.one*.82f;Piano.gameObject.SetActive(false);
        }
        public void Crouch(){landingUntil=Time.time+.13f;}
        public void PlayNote(int pitch = 0)
        { note = pitch % 7; actionStarted = Time.time; actionUntil = Time.time + .35f; aim = (Vector2)transform.right * (facingLeft ? -1 : 1); }
        public void Aim(Vector2 direction)
        { aim = direction; actionStarted = Time.time; actionUntil = Time.time + .3f; note = game.Shots % 7; }
        void LateUpdate()
        {
            if (game == null || Time.timeScale == 0) return;
            var player = game.Player;
            float speed = Vector2.Dot(player.Body.linearVelocity, transform.right);
            bool acting = Time.time < actionUntil;
            bool calling = game.State == DemoState.Challenge && Keyboard.current != null && Keyboard.current.qKey.isPressed;
            bool playingFlute = (acting || calling) && game.Equipped == 0;
            if (acting) facingLeft = Vector2.Dot(aim, transform.right) < 0;
            else if (Mathf.Abs(speed) > .15f) facingLeft = speed < 0;
            if (player.IsGrounded && !grounded)
            {
                landingUntil = Time.time + .16f;
                game.Art.Effect("02", player.Body.position - player.Up * .23f, .3f, transform.rotation);
            }
            grounded = player.IsGrounded;
            string key;
            if (playingFlute) key = "01/actor/" + (Time.time-actionStarted < .065f ? 1 : 2 + (int)(Time.time*5)%2);
            else if (acting) key = "09/actor/1";
            else if (grounded && Time.time < landingUntil) key = "hero/9";
            else if (!grounded) key = "hero/5";
            else if (Mathf.Abs(speed) > .15f)
            {
                walkClock += Mathf.Abs(speed) * Time.deltaTime * 2.6f;
                key = "03/actor/" + ((int)walkClock % 6);
            }
            else key = "hero/0";
            Traveler.sprite = PixelArtLibrary.Get(key); Traveler.flipX = facingLeft;
            float lean = acting || calling ? 0 : Mathf.Clamp(-speed * 1.15f,-6,6);
            Traveler.transform.localRotation = Quaternion.Slerp(Traveler.transform.localRotation,Quaternion.Euler(0,0,lean),1-Mathf.Exp(-16*Time.deltaTime));
            float breath = grounded && !acting && Mathf.Abs(speed)<.15f ? Mathf.Sin(Time.time*2.5f)*.008f : 0;
            Traveler.transform.localScale = new Vector3(1-breath,1+breath,1);
            // The playing strip already contains the flute at the mouth. In recovery it returns to orbit.
            Instrument.enabled = game.Equipped == 0 && !playingFlute;
            Instrument.transform.localPosition = Vector3.Lerp(Instrument.transform.localPosition,
                new Vector3(facingLeft?.63f:-.63f,.55f+Mathf.Sin(Time.time*2.5f)*.07f,-.3f),1-Mathf.Exp(-12*Time.deltaTime));
            Instrument.transform.localRotation = Quaternion.Euler(0,0,facingLeft?-12:12);
            Instrument.flipX = facingLeft; PixelArtLibrary.Size(Instrument,new Vector2(.55f,.11f));
            Piano.gameObject.SetActive(game.Equipped == 1);
            Piano.localPosition = Vector3.Lerp(Piano.localPosition,new Vector3(facingLeft?-.96f:.96f,acting?.32f:.27f+Mathf.Sin(Time.time*2)*.055f,-.35f),1-Mathf.Exp(-14*Time.deltaTime));
            Piano.localRotation = Quaternion.Euler(0,0,acting?0:Mathf.Sin(Time.time*1.4f)*4);
            for(int i=0;i<pianoKeys.Length;i++)
            {
                var color = acting && note == i ? DemoGame.Accent : new Color(.95f,.9f,.74f);
                if(pianoKeys[i].color != color) { pianoKeys[i].color=color;pianoKeys[i].Rebuild(); }
                var p=pianoKeys[i].transform.localPosition;p.y=acting && note==i?-.035f:-.005f;pianoKeys[i].transform.localPosition=p;
            }
        }
    }
}
