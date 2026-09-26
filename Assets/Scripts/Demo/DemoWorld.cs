using UnityEngine;
using TMPro;

namespace FlyMeToTheMoon.Demo
{
    public enum DemoTargetKind { WalkFragment, ShotFragment, Resonator, PuzzleFragment, Practice, Altar, EchoPedestal }

    public sealed class DemoWorldLabel : MonoBehaviour
    { public string Caption; }

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
        public GravityBody Body;
        public int Index;
        public string Instrument;
        public readonly bool[] Fragments = new bool[3];
        public readonly DemoTarget[] Resonators = new DemoTarget[3];
        public DemoTarget Walk, Shot, Reward;
        public Vector2 Altar, Pedestal;
        public readonly GravityBody[] FragmentBodies = new GravityBody[3];
        public DemoTarget AltarTarget, PedestalTarget;
        public bool Unlocked, PuzzleSolved, PlayingSequence;
        public int SequenceStep;
        public int Count => (Fragments[0] ? 1 : 0) + (Fragments[1] ? 1 : 0) + (Fragments[2] ? 1 : 0);
        public Vector2 Surface(float degrees, float height, GravityBody home)
        {
            Vector2 towardHome = (home.Center - Body.Center).normalized;
            Vector2 up = Quaternion.Euler(0, 0, degrees) * towardHome;
            return Body.Center + up * (Body.radius + height);
        }
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
        public static void Label(Transform parent, string text, Vector2 position, TMP_FontAsset font, float width = 4f)
        {
            var go = new GameObject(text);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(position.x, position.y, -1f);
            go.AddComponent<DemoWorldLabel>().Caption = text;
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
            for (int i = 0; i < 3; i++) q.FragmentBodies[i] = bodies[1 + i * DemoGalaxy.MoonsPerRing + random.Next(DemoGalaxy.MoonsPerRing)];
            q.Altar = home.Center + (Vector2)(Quaternion.Euler(0,0,-7) * Vector2.up) * (home.radius + .2f);
            q.AltarTarget = Target(game, q, DemoTargetKind.Altar, 0, q.Altar, DemoGame.Accent, "Home altar");
            q.AltarTarget.Body = home; q.AltarTarget.Shape.shape = PrototypeShape.Shape.Rectangle;
            q.AltarTarget.Shape.size = new Vector2(1.0f,.28f); q.AltarTarget.Shape.Rebuild();
            q.AltarTarget.transform.rotation = Quaternion.FromToRotation(Vector3.up,home.UpAt(q.Altar));
            q.AltarTarget.transform.position = new Vector3(q.Altar.x,q.Altar.y,-.05f);
            Vector2[] sites = new Vector2[3];
            for (int i = 0; i < 3; i++)
            {
                var body = q.FragmentBodies[i];
                Vector2 up = Quaternion.Euler(0,0,(float)random.NextDouble()*120-60) * (home.Center-body.Center).normalized;
                sites[i] = body.Center + up * (body.radius + .4f);
            }
            q.Walk = Target(game, q, DemoTargetKind.WalkFragment, 0, sites[0], new Color(1,.84f,.38f), "Ruins page");
            q.Walk.Body = q.FragmentBodies[0];
            Vector2 shotUp = q.FragmentBodies[1].UpAt(sites[1]);
            q.Shot = Target(game, q, DemoTargetKind.ShotFragment, 1, sites[1] + shotUp * .9f, new Color(.5f,.85f,1), "Floating page");
            q.Shot.Body = q.FragmentBodies[1];
            q.Pedestal = sites[2];
            q.PedestalTarget = Target(game, q, DemoTargetKind.EchoPedestal, 0, q.Pedestal, new Color(.75f,.65f,1), "Echo pedestal");
            q.PedestalTarget.Body = q.FragmentBodies[2];
            q.PedestalTarget.transform.position = new Vector3(q.Pedestal.x,q.Pedestal.y,-.05f);
            q.Reward = Target(game, q, DemoTargetKind.PuzzleFragment, 2, q.Pedestal, new Color(1,.7f,.9f), "Echo page");
            q.Reward.Body = q.FragmentBodies[2]; q.Reward.gameObject.SetActive(false);
            Vector2 upEcho = q.FragmentBodies[2].UpAt(q.Pedestal), tangent = new Vector2(upEcho.y,-upEcho.x);
            for(int i=0;i<3;i++)
            {
                q.Resonators[i] = Target(game,q,DemoTargetKind.Resonator,i,q.Pedestal+upEcho*1.15f+tangent*((i-1)*.95f),new Color(.5f+i*.15f,.6f,1-i*.2f),"Resonator "+(i+1));
                q.Resonators[i].Body = q.FragmentBodies[2];
            }
            Vector2 archUp = q.FragmentBodies[0].UpAt(sites[0]), archTangent = new Vector2(archUp.y,-archUp.x);
            for(int i=-1;i<=1;i+=2)
            {
                var column = Shape(game.WorldRoot,"Ruined arch",sites[0]+archTangent*i*.65f,new Vector2(.18f,.9f),new Color(.4f,.45f,.6f),game.Material);
                column.transform.rotation = Quaternion.FromToRotation(Vector3.up,archUp);
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
            DemoTarget first = null;
            float firstTime = 2;
            Vector2 delta = end - start;
            foreach (var target in Game.Targets)
            {
                if (target == null || !target.gameObject.activeInHierarchy || target.Kind == DemoTargetKind.Altar || target.Kind == DemoTargetKind.EchoPedestal) continue;
                float t = delta.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot((Vector2)target.transform.position - start, delta) / delta.sqrMagnitude) : 0;
                if (Vector2.Distance(start + delta * t, target.transform.position) < 0.34f && t < firstTime)
                { first = target; firstTime = t; }
            }
            if (first != null) { Game.HitTarget(first); Destroy(gameObject); return; }
            foreach (var body in Game.Player.gravityManager.bodies)
                if (body.SurfaceDistance(end) < -0.02f) { Destroy(gameObject); return; }
            transform.position = new Vector3(end.x, end.y, -0.8f);
            remaining -= Time.deltaTime;
            if (remaining <= 0) Destroy(gameObject);
        }
    }
}
