using System.Collections.Generic;
using UnityEngine;

namespace FlyMeToTheMoon.Demo
{
    /// <summary>Builds the art layer over the existing generated world and encounter objects.</summary>
    public sealed class DemoArt : MonoBehaviour
    {
        public DemoPlayerArt PlayerArt { get; private set; }
        public DemoAmbientLife Ambient { get; private set; }
        public DemoSpaceBackdrop Space { get; private set; }
        public readonly List<SpriteRenderer> Worlds = new List<SpriteRenderer>();
        readonly List<DemoLandmarkArt> landmarks = new List<DemoLandmarkArt>();
        readonly List<Transform> scenery = new List<Transform>();
        DemoGame game;
        static readonly string[] Themes = { "bell", "piano", "woodwinds", "drum", "harp", "crystals" };

        public void Build(DemoGame owner, GravityBody[] bodies)
        {
            game = owner;
            for (int i = 0; i < bodies.Length; i++)
            {
                var body = bodies[i]; PixelArtLibrary.HideMeshes(body.transform);
                var world = PixelArtLibrary.Create(body.transform, "Illustrated walkable world", "world/" + (i == 0 ? 0 : 1 + (i - 1) % 6), -20);
                PixelArtLibrary.Size(world, Vector2.one * body.radius * 2); Worlds.Add(world);
                world.gameObject.AddComponent<DemoWorldSurface>().Build(world,body,i,game.Seed);
                if (i == 0 || game.Discoveries.HasSite(body)) continue;
                float angle = Mathf.Repeat(i * 137.51f + (game.Seed & 255), 360);
                angle = ClearAngle(body, angle, 2.8f);
                var landmark = SurfaceArt(body, Themes[(i - 1) % 6], angle, 1.7f);
                var reaction = landmark.gameObject.AddComponent<DemoLandmarkArt>();
                reaction.Build(game, body, Themes[(i - 1) % 6], landmark); landmarks.Add(reaction);
                for(int j=0;j<3;j++)
                {
                    var plant = SurfaceArt(body, "garden", ClearAngle(body, angle + 77 + j*89, 1.3f), .48f + j*.08f);
                    var life=plant.gameObject.AddComponent<DemoLandmarkArt>();life.Build(game,body,"garden",plant);landmarks.Add(life);
                }
            }
            var home = bodies[0];
            SurfaceArt(home, "observatory", game.Discoveries.ObservatoryAngle, 3.1f);
            SurfaceArt(home, "house", ClearAngle(home, 27, 3), 2.3f);
            var homeGarden=SurfaceArt(home, "garden", 52, 1.15f);
            homeGarden.gameObject.AddComponent<DemoHomeGarden>().Build(game);
            var homeDrum=SurfaceArt(home,"drum",ClearAngle(home,285,2),.75f);
            var drum=homeDrum.gameObject.AddComponent<DemoLandmarkArt>();drum.Build(game,home,"drum",homeDrum);landmarks.Add(drum);
            SurfaceArt(home, "garden", 60, .7f);
            SurfaceArt(home, "woodwinds", 303, 1.5f);
            SurfaceArt(home, "crystals", 75, .8f);
            Ambient = gameObject.AddComponent<DemoAmbientLife>(); Ambient.Build(game, bodies, scenery);
            foreach (var target in game.Targets) SkinTarget(target);
            PlayerArt = game.Player.gameObject.AddComponent<DemoPlayerArt>(); PlayerArt.Build(game);
            Camera.main.backgroundColor = new Color(.018f, .032f, .072f);
            var sky = new GameObject("Layered space background");
            sky.transform.SetParent(game.WorldRoot, false);
            Space = sky.AddComponent<DemoSpaceBackdrop>(); Space.Build(Camera.main, game.Seed);
        }
        float ClearAngle(GravityBody body, float angle, float clearance)
        {
            for (int attempt = 0; attempt < 24; attempt++, angle += 15)
            {
                Vector2 up = Quaternion.Euler(0, 0, -angle) * Vector2.up;
                Vector2 point = body.Center + up * body.radius; bool clear = true;
                foreach (var target in game.Targets)
                    if (target.Body == body && Vector2.Distance(point, target.transform.position) < clearance) { clear = false; break; }
                if (clear) return angle;
            }
            return angle;
        }
        SpriteRenderer SurfaceArt(GravityBody body, string kind, float angle, float height)
        {
            var mount = new GameObject("Scenery — " + kind).transform; mount.SetParent(body.transform, false);
            Vector2 up = Quaternion.Euler(0, 0, -angle) * Vector2.up;
            mount.localPosition = (Vector3)(up * (body.radius - .04f)) + Vector3.back * .1f;
            mount.localRotation = Quaternion.FromToRotation(Vector3.up, up);
            string key = kind == "piano" ? "03/object/0" : "prop/" + kind;
            var sprite = PixelArtLibrary.Create(mount, kind, key, 0);
            PixelArtLibrary.Height(sprite, height); scenery.Add(mount); return sprite;
        }
        void SkinTarget(DemoTarget target)
        {
            PixelArtLibrary.HideMeshes(target.transform);
            string key = target.Kind == DemoTargetKind.Altar ? "prop/altar" : target.Kind == DemoTargetKind.FragmentStation ? "prop/page" : "06/object/0";
            var art = PixelArtLibrary.Create(target.transform, "Quest pixel art", key, 4);
            float height = target.Kind == DemoTargetKind.FragmentStation ? 2 : target.Kind == DemoTargetKind.Altar ? 1.55f : .9f;
            PixelArtLibrary.Height(art, height);
            art.transform.localPosition = new Vector3(0, target.Kind == DemoTargetKind.FragmentStation ? -.45f : target.Kind == DemoTargetKind.Altar ? -.2f : -.3f, -.1f);
            if (target.Kind == DemoTargetKind.Creature)
                art.color = Color.Lerp(Color.white, target.BaseColor, .3f);
        }
        public void ShowEncounter(DemoChallenge session)
        {
            Ambient.ShowEncounter(session);
            foreach (var prop in scenery)
            {
                var body = prop.GetComponentInParent<GravityBody>();
                bool involved = body == session.Encounter.Body;
                if (session.Encounter is RelayEncounter relay) foreach (var route in relay.Route) involved |= body == route;
                prop.gameObject.SetActive(!involved);
            }
            int giantIndex = 0;
            foreach (var shape in session.Root.GetComponentsInChildren<PrototypeShape>(true))
            {
                string name = shape.name;
                if (name == "Eye" || name == "Listening eye") { PixelArtLibrary.HideMeshes(shape.transform); continue; }
                if (name == "Sleeping giant" && giantIndex++ != 1) { PixelArtLibrary.HideMeshes(shape.transform); continue; }
                var art = shape.gameObject.AddComponent<DemoEncounterArt>();
                if (!art.Build(game, shape)) Destroy(art);
            }
        }
        public void HideEncounter()
        { foreach (var prop in scenery) if (prop != null) prop.gameObject.SetActive(true); Ambient.HideEncounter(); }
        public void Projectile(PrototypeShape shape, Vector2 direction, bool echo)
        {
            PixelArtLibrary.HideMeshes(shape.transform);
            var art = PixelArtLibrary.Create(shape.transform, "Flying note", "note", 25);
            PixelArtLibrary.Height(art, .35f); art.color = echo ? new Color(.55f, .9f, 1, .65f) : Color.white;
            art.transform.rotation = Quaternion.FromToRotation(Vector3.up, game.Player.Up);
            if (!echo) PlayerArt.Aim(direction);
        }
        public DemoLandmarkArt NearestLandmark()
        {
            DemoLandmarkArt closest=null;float distance=1.6f;
            foreach(var landmark in landmarks)
            {
                if(!landmark.isActiveAndEnabled || !landmark.CanInteract)continue;
                float d=Vector2.Distance(game.Player.Body.position,landmark.transform.position);
                if(d<distance){distance=d;closest=landmark;}
            }
            return closest;
        }
        public void NotePassed(Vector2 from, Vector2 to)
        { foreach (var landmark in landmarks) if (landmark.isActiveAndEnabled) landmark.NotePassed(from, to); Ambient.NotePassed(from, to); }
        public void Recoil(Vector2 shotDirection)
        {
            // This source burst points left; rotate its leading edge toward the shot, away from the recoil.
            Effect("10", game.Player.Body.position + shotDirection * .55f, .55f,
                Quaternion.FromToRotation(Vector3.left, shotDirection), 3, 1);
        }
        public void BoostNotes()
        {
            // Keep the original NoteBurst lifetime and counters; replace only its temporary strokes.
            foreach (var stroke in FindObjectsByType<PrototypeShape>(FindObjectsSortMode.None))
            {
                var root = stroke.transform.parent;
                if (root == null || root.name != "Flute note" || root.GetComponentInChildren<SpriteRenderer>() != null) continue;
                PixelArtLibrary.HideMeshes(root);
                var note = PixelArtLibrary.Create(root, "Boost note", "note", 25);
                PixelArtLibrary.Height(note, .35f);
                note.transform.rotation = Quaternion.FromToRotation(Vector3.up, game.Player.Up);
            }
        }
        public void Effect(string sheet, Vector2 position, float size, Quaternion rotation, int firstFrame = 0, int frameCount = 6)
        {
            var art = PixelArtLibrary.Create(game.WorldRoot, "Musical response", sheet + "/fx/" + (frameCount == 1 ? firstFrame : 2), 30);
            art.transform.position = new Vector3(position.x, position.y, -.95f); art.transform.rotation = rotation;
            PixelArtLibrary.Height(art, size); art.gameObject.AddComponent<DemoArtEffect>().Build(art, sheet, firstFrame, frameCount);
        }
    }

    public sealed class DemoArtEffect : MonoBehaviour
    {
        SpriteRenderer sprite; string sheet; float age; int first, count;
        public void Build(SpriteRenderer renderer, string prefix, int firstFrame, int frameCount)
        { sprite = renderer; sheet = prefix; first = firstFrame; count = frameCount; }
        void Update()
        {
            age += Time.deltaTime; if (age >= .42f) { Destroy(gameObject); return; }
            sprite.sprite = PixelArtLibrary.Get(sheet + "/fx/" + (first + Mathf.Min((int)(age / .42f * count), count - 1)));
            sprite.color = new Color(1, 1, 1, 1 - age / .5f);
        }
    }

}
