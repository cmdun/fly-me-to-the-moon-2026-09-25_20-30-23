using UnityEngine;
using TMPro;

namespace FlyMeToTheMoon.Demo
{
    public enum DemoTargetKind { WalkFragment, ShotFragment, Resonator, PuzzleFragment, Practice }

    public sealed class DemoWorldLabel : MonoBehaviour
    { public string Caption; }

    public sealed class DemoTarget : MonoBehaviour
    {
        public DemoQuest Quest;
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
            Label(shape.transform, label, position + Vector2.up * 0.45f, game.Font, 2.5f);
            return node;
        }
        public static DemoQuest CreateQuest(DemoGame game, GravityBody body, int index)
        {
            var q = new DemoQuest { Body = body, Index = index, Instrument = DemoGame.Instruments[index + 1] };
            var home = game.Planets.respawnPlanet;
            q.Altar = q.Surface(0, 0.48f, home);
            q.Pedestal = q.Surface(105, 0.48f, home);
            var altar = Shape(game.WorldRoot, q.Instrument + " altar", q.Altar, new Vector2(1.0f, 0.45f),
                DemoGame.Accent, game.Material);
            altar.transform.rotation = Quaternion.FromToRotation(Vector3.up, body.UpAt(q.Altar));
            Label(game.WorldRoot, (index + 1) + " / " + q.Instrument + "\n[E] perform", q.Altar + body.UpAt(q.Altar) * 0.9f, game.Font);
            q.Walk = Target(game, q, DemoTargetKind.WalkFragment, 0, q.Surface(-55, 0.4f, home), new Color(1, 0.84f, 0.38f), "PAGE 1");
            q.Shot = Target(game, q, DemoTargetKind.ShotFragment, 1, q.Surface(48, 1.3f, home), new Color(0.5f, 0.85f, 1), "PAGE 2\nshoot");
            q.Reward = Target(game, q, DemoTargetKind.PuzzleFragment, 2, q.Pedestal, new Color(1, 0.7f, 0.9f), "PAGE 3");
            q.Reward.gameObject.SetActive(false);
            Vector2 up = body.UpAt(q.Pedestal), tangent = new Vector2(up.y, -up.x);
            for (int i = 0; i < 3; i++)
                q.Resonators[i] = Target(game, q, DemoTargetKind.Resonator, i,
                    q.Pedestal + up * 1.15f + tangent * ((i - 1) * 0.95f),
                    new Color(0.5f + i * 0.15f, 0.6f, 1f - i * 0.2f), (i + 1).ToString());
            Label(game.WorldRoot, "[E] hear sequence", q.Pedestal + up * 2.3f, game.Font);
            // Two simple ruins hide the first fragment until the explorer comes close.
            Vector2 archUp = body.UpAt(q.Walk.transform.position);
            Vector2 archTangent = new Vector2(archUp.y, -archUp.x);
            for (int i = -1; i <= 1; i += 2)
            {
                var column = Shape(game.WorldRoot, "Ruined arch", (Vector2)q.Walk.transform.position + archTangent * i * 0.65f,
                    new Vector2(0.18f, 0.9f), new Color(0.4f, 0.45f, 0.6f), game.Material);
                column.transform.rotation = Quaternion.FromToRotation(Vector3.up, archUp);
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
                if (target == null || !target.gameObject.activeInHierarchy) continue;
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
