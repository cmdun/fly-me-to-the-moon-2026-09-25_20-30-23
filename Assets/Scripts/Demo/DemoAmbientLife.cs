using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlyMeToTheMoon.Demo
{
    /// <summary>Collision-free rim scenery. Its creatures and musical plants answer the player's music.</summary>
    public sealed class DemoAmbientLife : MonoBehaviour
    {
        public sealed class Resident
        {
            public GravityBody Body;
            public Transform Mount;
            public SpriteRenderer Art, Note;
            public string Kind;
            public int Theme, Variant, Responses;
            public float Angle, Phase, ReactionUntil;
        }
        public readonly List<Resident> Residents = new List<Resident>();
        DemoGame game;
        float clock, nextCall, nextAnswer;
        readonly Dictionary<GravityBody, Transform> roots = new Dictionary<GravityBody, Transform>();
        Material material;

        public void Build(DemoGame owner, GravityBody[] bodies, List<Transform> landmarks)
        {
            game = owner;
            material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")) { name = "Musical rim scenery" };
            var random = new System.Random(game.Seed ^ 0x31ec74);
            for (int b = 0; b < bodies.Length; b++)
            {
                var body = bodies[b];
                var root = new GameObject("Musical wildlife and plants").transform;
                root.SetParent(body.transform, false); roots.Add(body, root);
                int count = Mathf.FloorToInt(body.radius * Mathf.PI * 2 / 1.05f), placed = 0;
                float phase = (float)random.NextDouble() * 360;
                for (int i = 0; i < count; i++)
                {
                    float angle = phase + (i + (float)random.NextDouble() * .4f) * 360 / count;
                    Vector2 up = Quaternion.Euler(0,0,-angle) * Vector2.up;
                    if (!Clear(body, up, landmarks)) continue;
                    string kind = placed % 10 == 0 ? "creature" : placed % 10 == 4 ? "shrine" : "plant";
                    int theme = b == 0 ? placed % 6 : (b - 1) % 6;
                    var mount = new GameObject(kind == "shrine" ? "Musical sculpture" : kind == "creature" ? "Moon songling" : "Musical flora").transform;
                    mount.SetParent(root,false);
                    var resident = new Resident { Body=body, Mount=mount, Kind=kind, Theme=theme, Angle=angle,
                        Variant=random.Next(3), Phase=(float)random.NextDouble()*Mathf.PI*2 };
                    resident.Art = Sprite(mount,kind,theme,kind=="plant"?resident.Variant:0,-3);
                    resident.Note = Sprite(mount,"note",theme,0,-2);resident.Note.enabled=false;
                    Residents.Add(resident);Place(resident,0,0);placed++;
                }
            }
        }
        SpriteRenderer Sprite(Transform parent,string kind,int theme,int frame,int order)
        {
            var go=new GameObject(kind);go.transform.SetParent(parent,false);
            var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=DemoAmbientSprites.Get(kind,theme,frame);
            renderer.sharedMaterial=material;renderer.sortingOrder=order;return renderer;
        }
        bool Clear(GravityBody body,Vector2 up,List<Transform> landmarks)
        {
            Vector2 point=body.Center+up*body.radius;
            // Reserve the full activity, plus space for the creature's small patrol.
            if(game.Discoveries.Reserved(body,point,1.0f))return false;
            if(body==game.Planets.respawnPlanet && Arc(body,up,game.Discoveries.Garden.position)<6.5f)return false;
            foreach(var target in game.Targets)
                if(target.Body==body && Arc(body,up,target.transform.position)<(target.Kind==DemoTargetKind.FragmentStation?3f:2f))return false;
            foreach(var landmark in landmarks)
                if(landmark.GetComponentInParent<GravityBody>()==body && Arc(body,up,landmark.position)<1.6f)return false;
            return true;
        }
        static float Arc(GravityBody body,Vector2 up,Vector2 position)
            => Mathf.Abs(Vector2.SignedAngle(up,position-body.Center))*Mathf.Deg2Rad*body.radius;
        void Place(Resident r,float drift,float hop)
        {
            Vector2 up=Quaternion.Euler(0,0,-r.Angle-drift/r.Body.radius*Mathf.Rad2Deg)*Vector2.up;
            r.Mount.localPosition=(Vector3)(up*(r.Body.radius-.015f+hop))+Vector3.back*.06f;
            r.Mount.localRotation=Quaternion.FromToRotation(Vector3.up,up);
        }
        public void MusicPlayed(int note)
        {
            if(game==null || game.State!=DemoState.Explore || game.MapVisible)return;
            foreach(var r in Residents)
                if(r.Body==game.Player.gravityManager.CurrentBody && Vector2.Distance(r.Mount.position,game.Player.Body.position)<4.5f)
                    Answer(r,note);
        }
        public void NotePassed(Vector2 from,Vector2 to)
        {
            if(game==null || game.State!=DemoState.Explore)return;
            Vector2 line=to-from;
            foreach(var r in Residents)
            {
                if(!r.Mount.gameObject.activeInHierarchy || r.ReactionUntil>clock+.7f)continue;
                Vector2 point=(Vector2)r.Mount.position+(Vector2)r.Mount.up*(r.Kind=="shrine"?.7f:.4f);
                float t=line.sqrMagnitude<.0001f?0:Mathf.Clamp01(Vector2.Dot(point-from,line)/line.sqrMagnitude);
                if((point-from-line*t).sqrMagnitude<.4f*.4f)Answer(r,r.Theme);
            }
        }
        void Answer(Resident r,int note)
        {
            if(r.ReactionUntil>clock+.7f)return;
            r.Responses++;r.ReactionUntil=clock+1.5f;
            r.Note.color=Color.white;
            // One quiet response nearby, rather than a chorus of overlapping AudioSources.
            if(r.Kind=="creature" && clock>=nextAnswer && Vector2.Distance(r.Mount.position,game.Player.Body.position)<4.5f)
            { nextAnswer=clock+1.2f;game.Audio.Note(0,(note+2)%7,.09f); }
        }
        public void ShowEncounter(DemoChallenge challenge)
        {
            foreach(var pair in roots)
            {
                bool involved=pair.Key==challenge.Encounter.Body;
                if(challenge.Encounter is RelayEncounter relay)foreach(var body in relay.Route)involved|=pair.Key==body;
                pair.Value.gameObject.SetActive(!involved);
            }
        }
        public void HideEncounter(){foreach(var root in roots.Values)root.gameObject.SetActive(true);}
        void Update()
        {
            if(game==null || game.State!=DemoState.Explore || game.MapVisible || Time.timeScale==0)return;
            clock+=Time.deltaTime;
            if(Keyboard.current!=null && Keyboard.current.qKey.isPressed && clock>=nextCall)
            {nextCall=clock+1.5f;MusicPlayed(4);}
            var camera=Camera.main;
            float range=camera==null?22:Mathf.Max(12,camera.orthographicSize*(1+camera.aspect)+3);
            foreach(var r in Residents)
            {
                if(!r.Mount.gameObject.activeInHierarchy || (camera!=null && Vector2.Distance(r.Mount.position,camera.transform.position)>range))continue;
                float elapsed=1.5f-(r.ReactionUntil-clock);bool responding=r.ReactionUntil>clock;
                float beat=Mathf.Sin(clock*2+r.Phase);
                if(r.Kind=="creature")
                {
                    float step=Mathf.Max(0,Mathf.Sin(clock*3+r.Phase));
                    float drift=Mathf.Sin(clock*.6f+r.Phase)*.22f;
                    Place(r,drift,responding?Mathf.Abs(Mathf.Sin(elapsed*8))*.12f:step*.035f);
                    int frame=responding?3:step>.8f?2:Mathf.Sin(clock*.5f+r.Phase)>.985f?1:0;
                    r.Art.sprite=DemoAmbientSprites.Get("creature",r.Theme,frame);
                    r.Art.flipX=responding?Vector2.Dot(game.Player.Body.position-(Vector2)r.Mount.position,r.Mount.right)<0:Mathf.Cos(clock*.6f+r.Phase)<0;
                }
                else r.Art.transform.localRotation=Quaternion.Euler(0,0,beat*(responding?5:r.Kind=="shrine"?.6f:1.5f));
                r.Art.color=responding?Color.Lerp(Color.white,DemoAmbientSprites.Accent(r.Theme),.2f+beat*.1f):Color.white;
                r.Note.enabled=responding;
                if(responding)
                {
                    r.Note.transform.localPosition=new Vector3(Mathf.Sin(elapsed*3)*.12f,.65f+elapsed*.35f,-.01f);
                    var c=r.Note.color;c.a=Mathf.Clamp01((1.5f-elapsed)*1.8f);r.Note.color=c;
                }
            }
        }
        void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
