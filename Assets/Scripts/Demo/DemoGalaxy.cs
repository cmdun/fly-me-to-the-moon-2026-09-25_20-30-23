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
        const float FieldRadius = 78f;
        public static MoonPlacement[] Layout(int seed)
        {
            var random = new System.Random(seed);
            for (int attempt = 0; attempt < 256; attempt++)
            {
                var layout = new MoonPlacement[MoonCount];
                bool complete = true;
                Vector2 sum = Vector2.zero;
                for (int i = 0; i < layout.Length; i++)
                {
                    int candidates = 0;
                    float bestScore = float.PositiveInfinity;
                    for (int trial = 0; trial < 900; trial++)
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
                        float distance = center.magnitude;
                        bool clear = distance - 12 - radius >= MinimumGap - .001f && distance + radius <= FieldRadius;
                        for (int j = 0; j < i && clear; j++)
                            clear = Vector2.Distance(center, layout[j].Center) - radius - layout[j].Radius >= MinimumGap - .001f;
                        if (!clear) continue;
                        // Choose among random valid positions, favoring empty directions and
                        // open space. Noise preserves irregular spacing instead of equal slots.
                        float score = Crowding(layout, i, center) + (float)random.NextDouble() * 1.5f;
                        if (i > 2) score += ((sum + center) / (i + 1)).sqrMagnitude * .035f;
                        if (score < bestScore)
                        {
                            bestScore = score;
                            layout[i] = new MoonPlacement { Center = center, Radius = radius, ParentIndex = parentIndex,
                                Depth = parentIndex < 0 ? 1 : layout[parentIndex].Depth + 1 };
                        }
                        if (++candidates >= 24 || i == 0) break;
                    }
                    if (candidates == 0) { complete = false; break; }
                    sum += layout[i].Center;
                }
                if (complete && BalancedAroundHome(layout, sum)) return layout;
            }
            throw new InvalidOperationException("Could not generate a balanced, connected moon field.");
        }

        static float Crowding(MoonPlacement[] layout, int count, Vector2 center)
        {
            float score = 0;
            Vector2 direction = center.normalized;
            for (int i = 0; i < count; i++)
            {
                float alignment = Mathf.Clamp(Vector2.Dot(direction, layout[i].Center.normalized), -1, 1);
                score += Mathf.Exp((alignment - 1) * 4) * 1.5f;
                score += Mathf.Exp(-(center - layout[i].Center).sqrMagnitude / (2 * 18 * 18)) * .8f;
            }
            return score;
        }

        static bool BalancedAroundHome(MoonPlacement[] layout, Vector2 sum)
        {
            if ((sum / layout.Length).magnitude > 6) return false;
            var quadrants = new int[4];
            var angles = new float[layout.Length];
            Vector2 minimum = Vector2.zero, maximum = Vector2.zero;
            float extent = 0;
            int deepest = 0;
            for (int i = 0; i < layout.Length; i++)
            {
                Vector2 center = layout[i].Center;
                minimum = Vector2.Min(minimum, center); maximum = Vector2.Max(maximum, center);
                extent = Mathf.Max(extent, center.magnitude + layout[i].Radius);
                deepest = Mathf.Max(deepest, layout[i].Depth);
                quadrants[(center.x < 0 ? 1 : 0) + (center.y < 0 ? 2 : 0)]++;
                angles[i] = Mathf.Atan2(center.y, center.x) * Mathf.Rad2Deg;
            }
            if (extent < 70 || deepest < 4 || minimum.x > -45 || minimum.y > -45 || maximum.x < 45 || maximum.y < 45)
                return false;
            // These are coverage checks, never target positions or fixed quadrant quotas.
            foreach (int count in quadrants) if (count < 4 || count > 8) return false;
            Array.Sort(angles);
            for (int i = 0; i < angles.Length; i++)
            {
                float next = i + 1 < angles.Length ? angles[i + 1] : angles[0] + 360;
                if (next - angles[i] > 35) return false;
            }
            return true;
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
