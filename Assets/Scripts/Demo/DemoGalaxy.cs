using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlyMeToTheMoon.Demo
{
    public struct MoonPlacement
    {
        public Vector2 Center;
        public float Radius;
        // Layout index of a reachable neighbor; -1 means the home planet.
        public int ParentIndex;
        public int Depth;
    }

    public static class DemoGalaxy
    {
        public const int MoonCount = 24;
        public const float MinimumGap = 6.5f, MaximumRouteGap = 8f;
        public static MoonPlacement[] Layout(int seed)
        {
            var random = new System.Random(seed);
            for (int attempt = 0; attempt < 100; attempt++)
            {
                var layout = new MoonPlacement[MoonCount];
                bool complete = true;
                float extent = 0;
                Vector2 minimum = Vector2.zero, maximum = Vector2.zero;
                int deepest = 0;
                for (int i = 0; i < layout.Length; i++)
                {
                    bool placed = false;
                    for (int trial = 0; trial < 700; trial++)
                    {
                        // Grow from any existing world, in any direction. No angular slots,
                        // rings, grid, or fixed number of neighbors around the home planet.
                        int parentIndex = random.Next(i + 1) - 1;
                        Vector2 parent = parentIndex < 0 ? Vector2.zero : layout[parentIndex].Center;
                        float parentRadius = parentIndex < 0 ? 12 : layout[parentIndex].Radius;
                        float radius = 3.2f + (float)random.NextDouble() * 2.6f;
                        float angle = (float)random.NextDouble() * Mathf.PI * 2;
                        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                        float gap = MinimumGap + (float)random.NextDouble() * (MaximumRouteGap - MinimumGap);
                        if (parentIndex >= 0)
                        {
                            int ancestor = layout[parentIndex].ParentIndex;
                            Vector2 incoming = parent - (ancestor < 0 ? Vector2.zero : layout[ancestor].Center);
                            // Avoid long straight strings of moons, while allowing arbitrary bends.
                            if (Vector2.Angle(incoming, direction) < 25) continue;
                        }
                        Vector2 center = parent + direction * (parentRadius + radius + gap);
                        bool clear = center.magnitude - 12 - radius >= MinimumGap - .001f;
                        for (int j = 0; j < i && clear; j++)
                            clear = Vector2.Distance(center, layout[j].Center) - radius - layout[j].Radius >= MinimumGap - .001f;
                        if (!clear) continue;
                        int depth = parentIndex < 0 ? 1 : layout[parentIndex].Depth + 1;
                        layout[i] = new MoonPlacement { Center = center, Radius = radius, ParentIndex = parentIndex, Depth = depth };
                        minimum = Vector2.Min(minimum, center); maximum = Vector2.Max(maximum, center);
                        extent = Mathf.Max(extent, center.magnitude + radius); deepest = Mathf.Max(deepest, depth);
                        placed = true; break;
                    }
                    if (!placed) { complete = false; break; }
                }
                // Reject cramped or narrow fields, without imposing a circular outer boundary.
                if (complete && extent >= 70 && maximum.x - minimum.x >= 85 && maximum.y - minimum.y >= 85 && deepest >= 4)
                    return layout;
            }
            throw new InvalidOperationException("Could not generate a connected moon field.");
        }

        public static int[] RelayPath(MoonPlacement[] layout, int seed)
        {
            var destinations = new List<int>();
            for (int i = 0; i < layout.Length; i++) if (layout[i].Depth == 4) destinations.Add(i);
            if (destinations.Count == 0) throw new InvalidOperationException("Moon field needs a three-transfer relay route.");
            var random = new System.Random(seed ^ 0x72b51);
            int cursor = destinations[random.Next(destinations.Count)];
            var path = new int[4];
            for (int i = path.Length - 1; i >= 0; i--) { path[i] = cursor; cursor = layout[cursor].ParentIndex; }
            return path;
        }

        public static GravityBody[] Generate(DemoGame game, MoonPlacement[] layout)
        {
            var home = game.Planets.respawnPlanet;
            foreach (var old in game.Player.gravityManager.bodies)
                if (old != home) old.gameObject.SetActive(false);
            home.gravity = 8;
            var bodies = new List<GravityBody> { home };
            for (int i = 0; i < layout.Length; i++)
            {
                var moon = layout[i];
                var shape = DemoWorld.Shape(game.WorldRoot, "Moon " + (i + 1), home.Center + moon.Center,
                    Vector2.one * moon.Radius * 2, Color.Lerp(new Color(.4f,.48f,.65f), new Color(.55f,.42f,.7f), (moon.Radius - 3.2f) / 2.6f), game.Material, true);
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
