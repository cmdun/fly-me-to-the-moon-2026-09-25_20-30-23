using System.Collections.Generic;
using UnityEngine;

namespace FlyMeToTheMoon.Demo
{
    public enum DemoArtRegion { Background, HomeTerrain, HomeLandmark, MoonDetail, QuestDetail }

    public sealed class DemoArtMarker : MonoBehaviour
    {
        public DemoArtRegion Region;
        public int MoonIndex = -1;
    }

    /// <summary>
    /// Adds a visual-only layer to the generated demo world. Nothing here owns
    /// colliders, gravity, quest positions, or game state.
    /// </summary>
    public sealed class DemoArt : MonoBehaviour
    {
        private static readonly Color Abyss = Hex(0x071426);
        private static readonly Color SpaceBlue = Hex(0x112849);
        private static readonly Color Shadow = Hex(0x243044);
        private static readonly Color DeepTeal = Hex(0x1F4C5B);
        private static readonly Color Teal = Hex(0x2E8B7C);
        private static readonly Color Mint = Hex(0x72E5C2);
        private static readonly Color Coral = Hex(0xF16F61);
        private static readonly Color Gold = Hex(0xFFD166);
        private static readonly Color Lavender = Hex(0xB69CFF);
        private static readonly Color Ice = Hex(0x79D7FF);
        private static readonly Color Blossom = Hex(0xF58FC6);
        private static readonly Color Paper = Hex(0xEAF2D7);

        private DemoGame game;
        private Transform root;
        private bool built;

        public int Count(DemoArtRegion region)
        {
            int count = 0;
            foreach (var marker in GetComponentsInChildren<DemoArtMarker>(true))
                if (marker.Region == region) count++;
            return count;
        }

        public int CountMoon(int moonIndex)
        {
            int count = 0;
            foreach (var marker in GetComponentsInChildren<DemoArtMarker>(true))
                if (marker.Region == DemoArtRegion.MoonDetail && marker.MoonIndex == moonIndex) count++;
            return count;
        }

        public void Build(DemoGame owner)
        {
            if (built || owner == null || owner.Material == null || owner.Planets == null) return;
            built = true;
            game = owner;
            root = new GameObject("Art direction layer").transform;
            root.SetParent(transform, false);
            BuildBackground();
            BuildHome(owner.Planets.respawnPlanet);
            foreach (var quest in owner.Quests) BuildMoon(quest);
            foreach (var target in owner.Targets) BuildTarget(target);
        }

        private void BuildBackground()
        {
            var background = new GameObject("Deep space parallax").transform;
            background.SetParent(root, false);

            var oldState = Random.state;
            Random.InitState(6122026);
            for (int i = 0; i < 140; i++)
            {
                var position = new Vector2(Random.Range(-42f, 42f), Random.Range(-42f, 42f));
                float size = i % 11 == 0 ? 0.22f : Random.Range(0.08f, 0.14f);
                Color color = i % 9 == 0 ? Gold : i % 5 == 0 ? Ice : Paper;
                LocalShape(background, "Distant star", position, Vector2.one * size, Alpha(color, 0.65f + Random.value * 0.35f),
                    i % 11 == 0, 2.5f, DemoArtRegion.Background);
                if (i % 11 == 0)
                {
                    LocalShape(background, "Star ray", position, new Vector2(size * 2.8f, size * 0.35f),
                        Alpha(color, 0.8f), false, 2.45f, DemoArtRegion.Background);
                    LocalShape(background, "Star ray", position, new Vector2(size * 0.35f, size * 2.8f),
                        Alpha(color, 0.8f), false, 2.45f, DemoArtRegion.Background);
                }
            }
            Random.state = oldState;
        }

        private void BuildHome(GravityBody home)
        {
            Shape("Home atmosphere", home.Center, Vector2.one * (home.radius * 2f + 0.7f), Alpha(Mint, 0.16f), true,
                0.22f, DemoArtRegion.HomeTerrain);

            // Broad land and sea patches make the large planet readable while the camera is close.
            var oldState = Random.state;
            Random.InitState(612);
            for (int i = 0; i < 14; i++)
            {
                float angle = i * 360f / 14f + Random.Range(-8f, 8f);
                float distance = Random.Range(home.radius * 0.35f, home.radius * 0.72f);
                Vector2 direction = Rotate(Vector2.up, angle);
                Shape("Home terrain patch", home.Center + direction * distance,
                    new Vector2(Random.Range(1.3f, 3.1f), Random.Range(0.7f, 1.6f)),
                    Alpha(i % 3 == 0 ? SpaceBlue : DeepTeal, 0.72f), true, -0.035f, DemoArtRegion.HomeTerrain,
                    Random.Range(-30f, 30f));
            }
            Random.state = oldState;

            // Dense surface life: the home world deliberately has far more detail than any moon.
            for (int i = 0; i < 18; i++)
            {
                float angle = i * 20f + (i % 2 == 0 ? 4f : -3f);
                var cluster = SurfaceGroup(home, "Home flora cluster", angle, 0.02f);
                LocalShape(cluster, "Grass left", new Vector2(-0.13f, 0.12f), new Vector2(0.08f, 0.34f),
                    i % 3 == 0 ? Mint : Teal, false, -0.12f, DemoArtRegion.HomeTerrain, 18f);
                LocalShape(cluster, "Grass right", new Vector2(0.12f, 0.1f), new Vector2(0.07f, 0.28f),
                    i % 4 == 0 ? Gold : Mint, false, -0.12f, DemoArtRegion.HomeTerrain, -20f);
                if (i % 3 == 0)
                    LocalShape(cluster, "Glow flower", new Vector2(0f, 0.34f), Vector2.one * 0.12f,
                        i % 2 == 0 ? Blossom : Gold, true, -0.14f, DemoArtRegion.HomeTerrain);
            }

            BuildObservatory(home, 0f);
            BuildMusicStage(home, -31f);
            BuildVillage(home, 34f, Coral);
            BuildVillage(home, 66f, Lavender);
            BuildBeacon(home, -67f);
        }

        private void BuildObservatory(GravityBody body, float angle)
        {
            var g = SurfaceGroup(body, "612-B observatory", angle, 0.05f);
            LocalShape(g, "Foundation", new Vector2(0, 0.16f), new Vector2(2.3f, 0.32f), Shadow, false, -0.2f, DemoArtRegion.HomeLandmark);
            LocalShape(g, "Observatory", new Vector2(0, 0.62f), new Vector2(1.55f, 1.12f), Paper, true, -0.22f, DemoArtRegion.HomeLandmark);
            LocalShape(g, "Dome shade", new Vector2(-0.28f, 0.62f), new Vector2(0.82f, 0.88f), SpaceBlue, true, -0.24f, DemoArtRegion.HomeLandmark);
            LocalShape(g, "Door", new Vector2(0.2f, 0.38f), new Vector2(0.35f, 0.55f), DeepTeal, false, -0.25f, DemoArtRegion.HomeLandmark);
            LocalShape(g, "Telescope", new Vector2(0.66f, 1.13f), new Vector2(1.2f, 0.22f), Ice, false, -0.25f, DemoArtRegion.HomeLandmark, -24f);
            LocalShape(g, "Telescope lens", new Vector2(1.16f, 1.36f), new Vector2(0.22f, 0.34f), Mint, false, -0.26f, DemoArtRegion.HomeLandmark, -24f);
            LocalShape(g, "Antenna mast", new Vector2(-0.72f, 1.27f), new Vector2(0.08f, 0.8f), Gold, false, -0.24f, DemoArtRegion.HomeLandmark);
            LocalShape(g, "Signal light", new Vector2(-0.72f, 1.72f), Vector2.one * 0.2f, Coral, true, -0.26f, DemoArtRegion.HomeLandmark);
        }

        private void BuildMusicStage(GravityBody body, float angle)
        {
            var g = SurfaceGroup(body, "Home music stage", angle, 0.04f);
            LocalShape(g, "Stage", new Vector2(0, 0.18f), new Vector2(2.1f, 0.28f), Gold, false, -0.2f, DemoArtRegion.HomeLandmark);
            LocalShape(g, "Stage inset", new Vector2(0, 0.22f), new Vector2(1.75f, 0.11f), Shadow, false, -0.23f, DemoArtRegion.HomeLandmark);
            for (int i = -2; i <= 2; i++)
            {
                float height = 0.42f + (2 - Mathf.Abs(i)) * 0.16f;
                LocalShape(g, "Equalizer light", new Vector2(i * 0.24f, 0.35f + height * 0.5f), new Vector2(0.12f, height),
                    i % 2 == 0 ? Mint : Blossom, false, -0.24f, DemoArtRegion.HomeLandmark);
            }
            LocalShape(g, "Speaker left", new Vector2(-0.9f, 0.52f), Vector2.one * 0.4f, SpaceBlue, false, -0.24f, DemoArtRegion.HomeLandmark);
            LocalShape(g, "Speaker right", new Vector2(0.9f, 0.52f), Vector2.one * 0.4f, SpaceBlue, false, -0.24f, DemoArtRegion.HomeLandmark);
        }

        private void BuildVillage(GravityBody body, float angle, Color roof)
        {
            var g = SurfaceGroup(body, "Home village", angle, 0.02f);
            for (int i = -1; i <= 1; i++)
            {
                float x = i * 0.58f;
                float height = i == 0 ? 0.7f : 0.5f;
                LocalShape(g, "Habitat", new Vector2(x, 0.18f + height * 0.5f), new Vector2(0.48f, height),
                    i == 0 ? Paper : DeepTeal, false, -0.18f, DemoArtRegion.HomeLandmark);
                LocalShape(g, "Roof light", new Vector2(x, 0.22f + height), new Vector2(0.52f, 0.16f), roof,
                    false, -0.21f, DemoArtRegion.HomeLandmark);
                LocalShape(g, "Window", new Vector2(x, 0.2f + height * 0.55f), Vector2.one * 0.13f, Gold,
                    true, -0.23f, DemoArtRegion.HomeLandmark);
            }
        }

        private void BuildBeacon(GravityBody body, float angle)
        {
            var g = SurfaceGroup(body, "Interplanetary beacon", angle, 0.02f);
            LocalShape(g, "Beacon base", new Vector2(0, 0.2f), new Vector2(1.1f, 0.28f), Shadow, false, -0.18f, DemoArtRegion.HomeLandmark);
            LocalShape(g, "Beacon mast", new Vector2(0, 0.88f), new Vector2(0.12f, 1.5f), Paper, false, -0.2f, DemoArtRegion.HomeLandmark);
            LocalShape(g, "Beacon dish", new Vector2(0.18f, 1.45f), new Vector2(0.75f, 0.18f), Ice, false, -0.22f, DemoArtRegion.HomeLandmark, -28f);
            LocalShape(g, "Beacon pulse", new Vector2(0.48f, 1.72f), Vector2.one * 0.22f, Mint, true, -0.24f, DemoArtRegion.HomeLandmark);
        }

        private void BuildMoon(DemoQuest quest)
        {
            var body = quest.Body;
            var theme = MoonTheme(quest.Index);
            Shape(quest.Instrument + " moon halo", body.Center, Vector2.one * (body.radius * 2f + 0.28f), Alpha(theme, 0.12f),
                true, 0.16f, DemoArtRegion.MoonDetail, 0, quest.Index);

            // Three broad craters and one instrument landmark keep moons readable and simple.
            for (int i = 0; i < 3; i++)
            {
                Vector2 direction = Rotate(Vector2.up, 80f + i * 82f + quest.Index * 13f);
                Shape(quest.Instrument + " crater", body.Center + direction * body.radius * 0.48f,
                    Vector2.one * (0.28f + body.radius * (0.12f + i * 0.035f)), Alpha(Shadow, 0.65f), true,
                    -0.035f, DemoArtRegion.MoonDetail, 0, quest.Index);
            }
            var landmark = SurfaceGroup(body, quest.Instrument + " landmark", 180f, 0.03f);
            BuildMoonLandmark(landmark, quest.Index, theme);
        }

        private void BuildMoonLandmark(Transform g, int index, Color theme)
        {
            switch (index)
            {
                case 0: // Piano keys
                    LocalShape(g, "Piano plinth", new Vector2(0, 0.14f), new Vector2(1.45f, 0.25f), Shadow, false, -0.17f, DemoArtRegion.MoonDetail, 0, index);
                    for (int i = -2; i <= 2; i++)
                        LocalShape(g, "Piano key", new Vector2(i * 0.23f, 0.45f), new Vector2(0.18f, 0.55f),
                            i % 2 == 0 ? Paper : Lavender, false, -0.2f, DemoArtRegion.MoonDetail, i * 3f, index);
                    break;
                case 1: // Bell chimes
                    LocalShape(g, "Bell arch", new Vector2(0, 0.62f), new Vector2(1.25f, 0.1f), Ice, false, -0.17f, DemoArtRegion.MoonDetail, 0, index);
                    for (int i = -1; i <= 1; i++)
                    {
                        LocalShape(g, "Bell cord", new Vector2(i * 0.38f, 0.42f), new Vector2(0.05f, 0.45f), Paper, false, -0.19f, DemoArtRegion.MoonDetail, 0, index);
                        LocalShape(g, "Bell", new Vector2(i * 0.38f, 0.18f), new Vector2(0.28f, 0.22f), Gold, true, -0.21f, DemoArtRegion.MoonDetail, 0, index);
                    }
                    break;
                case 2: // Volcanic drums
                    for (int i = -1; i <= 1; i++)
                    {
                        LocalShape(g, "Drum", new Vector2(i * 0.42f, 0.34f), new Vector2(0.36f, 0.55f + (i == 0 ? 0.18f : 0)), Coral, false, -0.18f, DemoArtRegion.MoonDetail, i * 5f, index);
                        LocalShape(g, "Drum head", new Vector2(i * 0.42f, 0.66f + (i == 0 ? 0.09f : 0)), new Vector2(0.4f, 0.12f), Gold, false, -0.21f, DemoArtRegion.MoonDetail, i * 5f, index);
                    }
                    break;
                case 3: // Harp ruin
                    LocalShape(g, "Harp pillar", new Vector2(-0.5f, 0.56f), new Vector2(0.12f, 1.15f), Gold, false, -0.18f, DemoArtRegion.MoonDetail, -8f, index);
                    LocalShape(g, "Harp crown", new Vector2(0, 1.08f), new Vector2(1.05f, 0.12f), Paper, false, -0.19f, DemoArtRegion.MoonDetail, -12f, index);
                    for (int i = 0; i < 4; i++)
                        LocalShape(g, "Harp string", new Vector2(-0.28f + i * 0.2f, 0.56f), new Vector2(0.035f, 0.82f - i * 0.08f),
                            i % 2 == 0 ? Gold : Paper, false, -0.21f, DemoArtRegion.MoonDetail, 0, index);
                    break;
                default: // Synth garden
                    LocalShape(g, "Synth stem", new Vector2(0, 0.55f), new Vector2(0.1f, 1.05f), Mint, false, -0.18f, DemoArtRegion.MoonDetail, 0, index);
                    for (int i = -1; i <= 1; i++)
                        LocalShape(g, "Synth light", new Vector2(i * 0.38f, 0.6f + (1 - Mathf.Abs(i)) * 0.35f), Vector2.one * 0.28f,
                            i == 0 ? Mint : Blossom, true, -0.21f, DemoArtRegion.MoonDetail, 0, index);
                    LocalShape(g, "Synth console", new Vector2(0, 0.18f), new Vector2(1.25f, 0.28f), theme, false, -0.2f, DemoArtRegion.MoonDetail, 0, index);
                    break;
            }
        }

        private void BuildTarget(DemoTarget target)
        {
            if (target == null) return;
            if (target.Kind == DemoTargetKind.Resonator)
            {
                LocalShape(target.transform, "Resonator ring", Vector2.zero, Vector2.one * 0.58f, Alpha(Paper, 0.28f), true,
                    0.05f, DemoArtRegion.QuestDetail);
                return;
            }
            if (target.Kind == DemoTargetKind.Practice)
            {
                LocalShape(target.transform, "Practice music stem", new Vector2(0.12f, 0.22f), new Vector2(0.06f, 0.4f), Mint,
                    false, -0.04f, DemoArtRegion.QuestDetail);
                return;
            }

            target.Shape.color = Alpha(target.BaseColor, 0.28f);
            target.Shape.Rebuild();
            var page = new GameObject("Floating score page").transform;
            page.SetParent(target.transform, false);
            page.gameObject.AddComponent<DemoArtBob>().Phase = target.Index * 1.7f;
            LocalShape(page, "Gold page edge", Vector2.zero, new Vector2(0.42f, 0.54f), Gold, false, -0.08f, DemoArtRegion.QuestDetail);
            LocalShape(page, "Score paper", Vector2.zero, new Vector2(0.33f, 0.45f), Paper, false, -0.1f, DemoArtRegion.QuestDetail);
            for (int i = 0; i < 3; i++)
                LocalShape(page, "Staff line", new Vector2(0, -0.1f + i * 0.1f), new Vector2(0.25f, 0.025f), SpaceBlue,
                    false, -0.12f, DemoArtRegion.QuestDetail);
            LocalShape(page, "Note", new Vector2(-0.035f, -0.04f), new Vector2(0.11f, 0.08f), Shadow, true,
                -0.14f, DemoArtRegion.QuestDetail);
            LocalShape(page, "Note stem", new Vector2(0.025f, 0.06f), new Vector2(0.03f, 0.24f), Shadow, false,
                -0.14f, DemoArtRegion.QuestDetail);
        }

        private Transform SurfaceGroup(GravityBody body, string name, float degrees, float height)
        {
            Vector2 direction = Rotate(Vector2.up, degrees);
            Vector2 position = body.Center + direction * (body.radius + height);
            var group = new GameObject(name).transform;
            group.SetParent(root, false);
            group.position = new Vector3(position.x, position.y, 0);
            group.rotation = Quaternion.FromToRotation(Vector3.up, direction);
            return group;
        }

        private PrototypeShape Shape(string name, Vector2 position, Vector2 size, Color color, bool circle,
            float z, DemoArtRegion region, float angle = 0, int moonIndex = -1)
        {
            var shape = LocalShape(root, name, position, size, color, circle, z, region, angle, moonIndex);
            shape.transform.position = new Vector3(position.x, position.y, z);
            return shape;
        }

        private PrototypeShape LocalShape(Transform parent, string name, Vector2 position, Vector2 size, Color color,
            bool circle, float z, DemoArtRegion region, float angle = 0, int moonIndex = -1)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(position.x, position.y, z);
            go.transform.localRotation = Quaternion.Euler(0, 0, angle);
            var shape = go.AddComponent<PrototypeShape>();
            shape.shape = circle ? PrototypeShape.Shape.Circle : PrototypeShape.Shape.Rectangle;
            shape.size = size;
            shape.color = color;
            shape.material = game.Material;
            shape.segments = circle ? 32 : 4;
            shape.Rebuild();
            var marker = go.AddComponent<DemoArtMarker>();
            marker.Region = region;
            marker.MoonIndex = moonIndex;
            return shape;
        }

        private static Vector2 Rotate(Vector2 value, float degrees) => Quaternion.Euler(0, 0, degrees) * value;
        private static Color Alpha(Color color, float alpha) { color.a = alpha; return color; }
        private static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);
        private static Color MoonTheme(int index) => index switch
        {
            0 => Lavender,
            1 => Ice,
            2 => Coral,
            3 => Gold,
            _ => Blossom
        };
    }

    public sealed class DemoArtBob : MonoBehaviour
    {
        public float Phase;
        private Vector3 origin;
        private void Start() => origin = transform.localPosition;
        private void Update()
        {
            float wave = Mathf.Sin(Time.unscaledTime * 2.6f + Phase);
            transform.localPosition = origin + Vector3.up * (wave * 0.055f);
            transform.localRotation = Quaternion.Euler(0, 0, wave * 3f);
        }
    }
}
