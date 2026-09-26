using UnityEngine;
using TMPro;

namespace FlyMeToTheMoon.Demo
{
    public enum DemoTargetKind { Altar, FragmentStation, Creature }
    public sealed class DemoTarget : MonoBehaviour
    {
        [System.NonSerialized] public DemoQuest Quest;
        public GravityBody Body;
        public DemoTargetKind Kind;
        public int Index;
        public Color BaseColor;
        public PrototypeShape Shape;
        public void Tint(Color color) { Shape.color = color; Shape.Rebuild(); }
    }
    public sealed class DemoQuest
    {
        public const int FragmentCount = 5;
        public GravityBody Body;
        public int Index;
        public string Instrument;
        public readonly bool[] Fragments = new bool[FragmentCount];
        public readonly DemoTarget[] Stations = new DemoTarget[FragmentCount];
        public readonly GravityBody[] FragmentBodies = new GravityBody[FragmentCount];
        public Vector2 Altar;
        public DemoTarget AltarTarget;
        public bool Unlocked;
        public int Count { get { int count = 0; foreach (bool found in Fragments) if (found) count++; return count; } }
    }

    public static class DemoWorld
    {
        public static PrototypeShape Shape(Transform parent, string name, Vector2 position, Vector2 size,
            Color color, Material material, bool circle = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(position.x, position.y, -0.5f);
            var shape = go.AddComponent<PrototypeShape>();
            shape.shape = circle ? PrototypeShape.Shape.Circle : PrototypeShape.Shape.Rectangle;
            shape.size = size; shape.color = color; shape.material = material; shape.Rebuild();
            return shape;
        }
        public static DemoTarget Target(DemoGame game, DemoQuest quest, DemoTargetKind kind,
            int index, Vector2 position, Color color, string label)
        {
            var shape = Shape(game.WorldRoot, label, position, Vector2.one * 0.38f, color, game.Material, true);
            var node = shape.gameObject.AddComponent<DemoTarget>();
            node.Quest = quest; node.Kind = kind; node.Index = index; node.Shape = shape; node.BaseColor = color;
            game.Targets.Add(node);

            return node;
        }
        public static DemoQuest CreateJourney(DemoGame game, GravityBody[] bodies, int seed)
        {
            var home = game.Planets.respawnPlanet;
            var random = new System.Random(seed ^ 0x45a31);
            var q = new DemoQuest { Body = home, Index = 0, Instrument = "Piano" };
            // Shuffle distinct hosts across the whole field; no duplicate pages or home challenges.
            var hosts = new System.Collections.Generic.List<GravityBody>();
            for (int i = 1; i < bodies.Length; i++) hosts.Add(bodies[i]);
            for (int i = hosts.Count - 1; i > 0; i--) { int j = random.Next(i + 1); var swap = hosts[i]; hosts[i] = hosts[j]; hosts[j] = swap; }
            for (int i = 0; i < DemoQuest.FragmentCount; i++) q.FragmentBodies[i] = hosts[i];
            q.Altar = home.Center + (Vector2)(Quaternion.Euler(0,0,-7) * Vector2.up) * (home.radius + .2f);
            q.AltarTarget = Target(game, q, DemoTargetKind.Altar, 0, q.Altar, DemoGame.Accent, "Home altar");
            q.AltarTarget.Body = home; q.AltarTarget.Shape.shape = PrototypeShape.Shape.Rectangle;
            q.AltarTarget.Shape.size = new Vector2(1.0f,.28f); q.AltarTarget.Shape.Rebuild();
            q.AltarTarget.transform.rotation = Quaternion.FromToRotation(Vector3.up,home.UpAt(q.Altar));
            q.AltarTarget.transform.position = new Vector3(q.Altar.x,q.Altar.y,-.05f);
            for (int i = 0; i < DemoQuest.FragmentCount; i++)
            {
                var body = q.FragmentBodies[i];
                Vector2 up = Quaternion.Euler(0, 0, (float)random.NextDouble() * 160 - 80) * (home.Center - body.Center).normalized;
                Vector2 site = body.Center + up * (body.radius + .45f);
                var station = Target(game, q, DemoTargetKind.FragmentStation, i, site, new Color(1, .8f, .28f), DemoChallenge.Names[i]);
                station.Body = body; q.Stations[i] = station;
                station.Shape.shape = PrototypeShape.Shape.Rectangle; station.Shape.size = new Vector2(1.1f, .24f); station.Shape.Rebuild();
                station.transform.rotation = Quaternion.FromToRotation(Vector3.up, up);
                station.transform.position = new Vector3(site.x, site.y, -.05f);
                // Large gold sheet and staff remain visible until this station is completed.
                var page = Shape(station.transform, "Score fragment " + (i + 1), site + up * 1.35f, new Vector2(.85f, 1.1f), new Color(1, .87f, .42f), game.Material);
                page.transform.rotation = station.transform.rotation;
                var beam = Shape(station.transform, "Beacon", site + up * .7f, new Vector2(.13f, 1.1f), new Color(.9f, .65f, .2f), game.Material);
                beam.transform.rotation = station.transform.rotation;
                beam.transform.position = new Vector3(beam.transform.position.x, beam.transform.position.y, -.04f);
                Vector2 tangent = new Vector2(up.y, -up.x);
                for (int line = 0; line < 5; line++)
                {
                    var staff = Shape(page.transform, "Staff", (Vector2)page.transform.position + up * ((line - 2) * .13f), new Vector2(.62f, .025f), new Color(.2f, .15f, .1f), game.Material);
                    staff.transform.rotation = station.transform.rotation;
                    staff.transform.position += Vector3.back * .02f;
                }
                var note = Shape(page.transform, "Note", (Vector2)page.transform.position + tangent * .12f, new Vector2(.15f, .12f), new Color(.2f, .15f, .1f), game.Material, true);
                note.transform.position += Vector3.back * .04f;
            }
            return q;
        }
    }

    public sealed class DemoProjectile : MonoBehaviour
    {
        public DemoGame Game;
        public Vector2 Direction;
        private float remaining = 1.6f;
        private void Update()
        {
            if (Game == null) { Destroy(gameObject); return; }
            if (Game.State != DemoState.Explore) return;
            Vector2 start = transform.position;
            Vector2 end = start + Direction * (12f * Time.deltaTime);
            foreach (var body in Game.Player.gravityManager.bodies)
                if (body.SurfaceDistance(end) < -0.02f) { Destroy(gameObject); return; }
            transform.position = new Vector3(end.x, end.y, -0.8f);
            remaining -= Time.deltaTime;
            if (remaining <= 0) Destroy(gameObject);
        }
    }
}
