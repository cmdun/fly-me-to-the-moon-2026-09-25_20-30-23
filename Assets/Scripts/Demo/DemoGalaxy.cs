using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlyMeToTheMoon.Demo
{
    public struct MoonPlacement
    {
        public Vector2 Center;
        public float Radius;
        public int Ring;
    }

    public static class DemoGalaxy
    {
        public const int Rings = 4, MoonsPerRing = 6;
        public const float MinimumGap = 6.5f, MaximumRouteGap = 8f;
        // Each moon has an inward route; bounded rejection prevents accidental close pairs.
        // Ring depths describe connectivity, not evenly spaced circles.
        public static MoonPlacement[] Layout(int seed)
        {
            var random = new System.Random(seed);
            for (int attempt = 0; attempt < 100; attempt++)
            {
                var layout = new MoonPlacement[Rings * MoonsPerRing];
                float rotation = (float)random.NextDouble() * 360;
                bool complete = true;
                for (int i = 0; i < layout.Length; i++)
                {
                    int ring = i / MoonsPerRing, slot = i % MoonsPerRing;
                    Vector2 parent = ring == 0 ? Vector2.zero : layout[i - MoonsPerRing].Center;
                    float parentRadius = ring == 0 ? 12 : layout[i - MoonsPerRing].Radius;
                    bool placed = false;
                    for (int trial = 0; trial < 160; trial++)
                    {
                        float radius = 3.2f + (float)random.NextDouble() * 2.6f;
                        float baseAngle = ring == 0 ? rotation + slot * 60 : Mathf.Atan2(parent.x, parent.y) * Mathf.Rad2Deg;
                        float angle = (baseAngle + ((float)random.NextDouble() - .5f) * (ring == 0 ? 16 : 44)) * Mathf.Deg2Rad;
                        float gap = MinimumGap + (float)random.NextDouble() * (MaximumRouteGap - MinimumGap);
                        Vector2 center = parent + new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * (parentRadius + radius + gap);
                        bool clear = center.magnitude - 12 - radius >= MinimumGap - .001f;
                        for (int j = 0; j < i && clear; j++)
                            clear = Vector2.Distance(center, layout[j].Center) - radius - layout[j].Radius >= MinimumGap - .001f;
                        if (!clear) continue;
                        layout[i] = new MoonPlacement { Center = center, Radius = radius, Ring = ring };
                        placed = true; break;
                    }
                    if (!placed) { complete = false; break; }
                }
                if (complete) return layout;
            }
            throw new InvalidOperationException("Could not generate a connected moon field.");
        }

        public static GravityBody[] Generate(DemoGame game, int seed)
        {
            var home = game.Planets.respawnPlanet;
            foreach (var old in game.Player.gravityManager.bodies)
                if (old != home) old.gameObject.SetActive(false);
            home.gravity = 8;
            var bodies = new List<GravityBody> { home };
            var layout = Layout(seed);
            for (int i = 0; i < layout.Length; i++)
            {
                var moon = layout[i];
                var shape = DemoWorld.Shape(game.WorldRoot, "Moon " + (i + 1), home.Center + moon.Center,
                    Vector2.one * moon.Radius * 2, Color.Lerp(new Color(.4f,.48f,.65f), new Color(.55f,.42f,.7f), moon.Ring / 3f), game.Material, true);
                shape.transform.position = new Vector3(shape.transform.position.x, shape.transform.position.y, 0);
                var body = shape.gameObject.AddComponent<GravityBody>();
                body.planetId = "Moon " + (i + 1); body.radius = moon.Radius; body.gravity = 5;
                body.captureHeight = .7f; body.releaseHeight = 1.0f;
                body.GetComponent<CircleCollider2D>().radius = moon.Radius;
                bodies.Add(body);
            }
            game.Player.gravityManager.bodies = bodies.ToArray();
            return bodies.ToArray();
        }
    }
}
