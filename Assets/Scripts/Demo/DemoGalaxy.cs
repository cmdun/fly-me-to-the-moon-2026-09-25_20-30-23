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
        public const int Rings = 3, MoonsPerRing = 6;
        // Bounded angular jitter keeps a chain of short hops through every ring.
        public static MoonPlacement[] Layout(int seed)
        {
            var random = new System.Random(seed);
            var layout = new MoonPlacement[Rings * MoonsPerRing];
            float rotation = (float)random.NextDouble() * 360;
            for (int ring = 0; ring < Rings; ring++)
                for (int slot = 0; slot < MoonsPerRing; slot++)
                {
                    float angle = (rotation + slot * 60 + ((float)random.NextDouble() - .5f) * 8) * Mathf.Deg2Rad;
                    layout[ring * MoonsPerRing + slot] = new MoonPlacement {
                        Center = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * (16.5f + ring * 5.5f),
                        Radius = 1.6f + (float)random.NextDouble() * .35f, Ring = ring
                    };
                }
            return layout;
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
                    Vector2.one * moon.Radius * 2, Color.Lerp(new Color(.4f,.48f,.65f), new Color(.55f,.42f,.7f), moon.Ring / 2f), game.Material, true);
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
