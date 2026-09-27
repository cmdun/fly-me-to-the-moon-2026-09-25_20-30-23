using UnityEngine;
using UnityEngine.InputSystem;

namespace FlyMeToTheMoon.Demo
{
    /// <summary>Presentation only: reads movement and actions; never writes to the player's physics.</summary>
    public sealed class DemoPlayerArt : MonoBehaviour
    {
        public SpriteRenderer Traveler { get; private set; }
        public SpriteRenderer Instrument { get; private set; }
        public string Pose => Traveler.sprite.name;
        DemoGame game;
        float actionUntil, landingUntil, walkClock;
        bool grounded, facingLeft;
        Vector2 aim;
        public void Build(DemoGame owner)
        {
            game = owner; PixelArtLibrary.HideMeshes(transform);
            Traveler = PixelArtLibrary.Create(transform, "Traveler pixel art", "hero/0", 20);
            Traveler.transform.localPosition = new Vector3(0, -.29f, -.2f);
            Instrument = PixelArtLibrary.Create(transform, "Removable instrument", "flute", 21);
            Instrument.enabled = false; grounded = game.Player.IsGrounded;
        }
        public void PlayNote() { actionUntil = Time.time + .35f; aim = (Vector2)transform.right * (facingLeft ? -1 : 1); }
        public void Aim(Vector2 direction) { aim = direction; actionUntil = Time.time + .23f; }
        void LateUpdate()
        {
            if (game == null || Time.timeScale == 0) return;
            var player = game.Player;
            float speed = Vector2.Dot(player.Body.linearVelocity, transform.right);
            bool acting = Time.time < actionUntil;
            bool calling = game.State == DemoState.Challenge && Keyboard.current != null && Keyboard.current.qKey.isPressed;
            if (acting) facingLeft = Vector2.Dot(aim, transform.right) < 0;
            else if (Mathf.Abs(speed) > .15f) facingLeft = speed < 0;
            if (player.IsGrounded && !grounded)
            {
                landingUntil = Time.time + .12f;
                game.Art.Effect("02", player.Body.position - player.Up * .23f, .3f, transform.rotation);
            }
            grounded = player.IsGrounded;
            string key;
            if (acting) key = "09/actor/1"; // Empty hands: the flute is a separate, aimable sprite.
            else if (calling && game.Equipped == 0) key = "01/actor/" + (1 + (int)(Time.time * 6) % 3);
            else if (grounded && Time.time < landingUntil) key = "hero/9";
            else if (!grounded) key = "hero/5";
            else if (Mathf.Abs(speed) > .15f)
            {
                walkClock += Mathf.Abs(speed) * Time.deltaTime * 2.6f;
                key = "03/actor/" + ((int)walkClock % 6);
            }
            else key = "hero/0";
            Traveler.sprite = PixelArtLibrary.Get(key); Traveler.flipX = facingLeft;
            // Different sheets have measured PPU, so crouching/jumping never stretch to standing height.
            Traveler.transform.localScale = Vector3.one;
            Instrument.enabled = acting;
            if (acting)
            {
                Instrument.sprite = PixelArtLibrary.Get(game.Equipped == 0 ? "flute" : "03/object/2");
                var direction = aim.sqrMagnitude > .01f ? aim.normalized : (Vector2)transform.right;
                Vector2 hand = player.Body.position + player.Up * .35f + direction * .23f;
                Instrument.transform.position = new Vector3(hand.x, hand.y, -.9f);
                Instrument.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
                PixelArtLibrary.Size(Instrument, game.Equipped == 0 ? new Vector2(.52f, .105f) : new Vector2(.6f, .24f));
                Instrument.flipY = facingLeft;
            }
        }
    }
}
