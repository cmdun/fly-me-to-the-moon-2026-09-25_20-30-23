using System.Collections.Generic;
using UnityEngine;

namespace FlyMeToTheMoon.Demo
{
    public sealed class DemoCreature : MonoBehaviour
    {
        public string EncounterId;
        public string Speaker;
        public string[] Lines;
        public string[] RepeatLines;
        public DemoTarget Target;
        public bool Seen;
    }

    /// <summary>Seeded, optional conversations that teach exploration while telling the story of the Hush.</summary>
    public sealed class DemoWorldEvents : MonoBehaviour
    {
        private DemoGame game;
        public readonly List<DemoCreature> Encounters = new List<DemoCreature>();
        public int SeenCount { get; private set; }

        private static readonly string[] Names =
        {
            "Pip, Starhopper", "Nim, Cartographer", "Mote, Lantern-Keeper",
            "Tink, Echo Beetle", "Vesper, Quietling", "The Bramble Choir",
            "Cadence, Pulse-Smith", "Sable, Gate-Moth"
        };

        private static readonly string[][] FirstLines =
        {
            new[] {
                "The little moons pull sideways. Walk with A and D, then press Space to jump away from the surface.",
                "Press Space once more in the air for a flute boost. You get one boost each flight, so listen before you leap.",
                "The Hush cannot catch a traveler who keeps a beat." },
            new[] {
                "I chart paths that move when nobody watches. Press M to see the whole world and its five golden page beacons.",
                "The nearby map stays centered on you. Follow nearby moons, one jump at a time; R returns you to your last safe landing.",
                "Every map has a blank place. The Hush was born in ours." },
            new[] {
                "Aim with the mouse and left-click to fire a note. Shooting plants your feet briefly, without spending your boost.",
                "Right-click in the air to recoil away from your aim. That spends the same one-flight boost as a second jump.",
                "Light and sound both travel. Only one of them remembers the way home." },
            new[] {
                "One beacon remembers your footsteps. C records a route onto its plate; E starts the echo while you cross the open gate.",
                "C lets you rerecord immediately. In the second room, your echo holds one plate while you stand on the other and shoot.",
                "The Hush repeats nothing. That is how we know an echo is still alive." },
            new[] {
                "We call it the Hush. It did not steal our music; it taught the moons to forget it.",
                "Five score pages still remember. Complete their five beacons and carry them to the altar on 612-B.",
                "Speak to us again. Small voices are how a world refuses to disappear." },
            new[] {
                "Keys 1 through 7 wake seven notes. Play them anywhere; a song does not need to be a weapon.",
                "When the score is repaired, perform it at the home altar. The piano will answer.",
                "After it wakes, land and press Tab to change instruments. We will add your note to ours." },
            new[] {
                "The pulse shrine throws waves around its moon. Jump low waves, hide under the blue shelter for high ones, and watch the warnings.",
                "Shoot the exposed switches between storms. If a wave catches you, only your current circuit resets.",
                "The Hush fears a rhythm chosen together." },
            new[] {
                "Five beacons teach five ways to remember: carry a spark, outwit a sleeping giant, repair a storm, shepherd singers, and duet with your echo.",
                "Hold Q to call singers; one needs a long note and another moves only in silence. Escape pauses any encounter.",
                "A closed gate is only a question asked in stone." }
        };

        private static readonly Color[] Colors =
        {
            new Color(.95f,.66f,.34f), new Color(.45f,.82f,1f), new Color(1f,.86f,.38f),
            new Color(.7f,.55f,1f), new Color(.65f,.72f,.85f), new Color(.96f,.5f,.78f),
            new Color(1f,.48f,.34f), new Color(.56f,1f,.7f)
        };

        public const int CreatureCount = 8;
        public static int[] Plan(int seed) => Plan(seed, DemoGalaxy.Layout(seed));
        public static int[] Plan(int seed, MoonPlacement[] layout)
        {
            var order = new List<int>();
            for (int i = 0; i < layout.Length; i++) order.Add(i);
            order.Sort((a,b) => {
                int depth = layout[a].Depth.CompareTo(layout[b].Depth);
                return depth != 0 ? depth : a.CompareTo(b);
            });
            var result = new int[CreatureCount];
            var random = new System.Random(seed ^ 0x6e624eb7);
            // Spread the existing eight roles across early and late travel, independently
            // of world coordinates: these groups do not determine moon placement.
            int groupSize = layout.Length / (CreatureCount / 2);
            for (int group = 0; group < CreatureCount / 2; group++)
            {
                int first = random.Next(groupSize), second = random.Next(groupSize - 1);
                if (second >= first) second++;
                result[group * 2] = order[group * groupSize + first];
                result[group * 2 + 1] = order[group * groupSize + second];
            }
            return result;
        }

        public void Build(DemoGame owner, GravityBody[] bodies, int seed)
        {
            game = owner;
            var home = owner.Planets.respawnPlanet;
            Vector2 guideUp = Quaternion.Euler(0, 0, 18f) * Vector2.up;
            Create("keeper", "Lyra, Keeper of 612-B", home, guideUp, new Color(.4f,.95f,.8f),
                new[] {
                    "The moons have gone quiet, traveler. The Hush tore one melody into five pages and sealed them inside golden beacons.",
                    "Press M to read the whole-world map. Complete all five beacon challenges, then bring their pages to the altar here on 612-B.",
                    "Twelve moons hold seeds, crystals and lost singers too. Press J for your field journal. Carry each collection back to the Observatory; a complete archive will awaken HOME." },
                new[] { "The moons are listening. Complete the five beacons, then return to the home altar." });

            int[] plan = Plan(seed, owner.MoonLayout);
            var random = new System.Random(seed ^ 0x21c449);
            for (int i = 0; i < plan.Length; i++)
            {
                GravityBody body = bodies[plan[i] + 1];
                Vector2 towardHome = (home.Center - body.Center).normalized;
                Vector2 up = FindClearDirection(body, towardHome, random);
                Create("moon-voice-" + i, Names[i], body, up, Colors[i], FirstLines[i],
                    new[] { RepeatLine(i) });
            }
        }

        private string RepeatLine(int role)
        {
            switch (role)
            {
                case 0: return "One boost for each flight. Land, breathe, and your flute will be ready again.";
                case 1: return "M opens the map. R finds your last safe ground.";
                case 2: return "Left click shoots. Right click recoils in the air and spends your boost.";
                case 3: return "C records your footsteps. Let the echo hold a plate while you play the other half of the duet.";
                case 4: return "The Hush grows smaller each time someone answers it.";
                case 5: return "Play seven notes with 1 through 7. We are listening.";
                case 6: return "Jump low waves. Shelter from high waves. Shoot the shrine when its switches glow.";
                default: return "Five beacons, five ways to remember. Escape pauses a challenge.";
            }
        }

        private Vector2 FindClearDirection(GravityBody body, Vector2 towardHome, System.Random random)
        {
            Vector2 fallback = towardHome;
            for (int attempt = 0; attempt < 24; attempt++)
            {
                Vector2 direction = Quaternion.Euler(0, 0, (float)random.NextDouble() * 300f - 150f) * towardHome;
                Vector2 position = body.Center + direction * (body.radius + .3f);
                bool clear = true;
                foreach (var target in game.Targets)
                    if (target.Body == body && Vector2.Distance(position, target.transform.position) < 3f) { clear = false; break; }
                if (clear) return direction;
                fallback = direction;
            }
            return fallback;
        }

        private DemoCreature Create(string id, string speaker, GravityBody body, Vector2 up, Color color,
            string[] lines, string[] repeatLines)
        {
            Vector2 position = body.Center + up.normalized * (body.radius + .3f);
            var target = DemoWorld.Target(game, null, DemoTargetKind.Creature, Encounters.Count, position, color, speaker);
            target.Body = body;
            target.Shape.size = new Vector2(.46f,.58f); target.Shape.Rebuild();
            target.transform.rotation = Quaternion.FromToRotation(Vector3.up, up);

            AddPart(target.transform, "Pale mask", new Vector2(0,.19f), new Vector2(.34f,.24f), new Color(.88f,.92f,.88f), true);
            AddPart(target.transform, "Left eye", new Vector2(-.08f,.2f), new Vector2(.045f,.075f), new Color(.08f,.1f,.15f), true);
            AddPart(target.transform, "Right eye", new Vector2(.08f,.2f), new Vector2(.045f,.075f), new Color(.08f,.1f,.15f), true);
            AddPart(target.transform, "Left feeler", new Vector2(-.14f,.39f), new Vector2(.045f,.24f), color, false, 18f);
            AddPart(target.transform, "Right feeler", new Vector2(.14f,.39f), new Vector2(.045f,.24f), color, false, -18f);

            var creature = target.gameObject.AddComponent<DemoCreature>();
            creature.EncounterId = id; creature.Speaker = speaker; creature.Lines = lines;
            creature.RepeatLines = repeatLines; creature.Target = target;
            Encounters.Add(creature);
            return creature;
        }

        private void AddPart(Transform parent, string name, Vector2 localPosition, Vector2 size, Color color, bool circle, float rotation = 0)
        {
            var part = DemoWorld.Shape(parent, name, Vector2.zero, size, color, game.Material, circle);
            part.transform.localPosition = new Vector3(localPosition.x, localPosition.y, -.04f);
            part.transform.localRotation = Quaternion.Euler(0, 0, rotation);
        }

        public void Speak(DemoCreature creature)
        {
            if (creature == null) return;
            if (creature.EncounterId == "keeper" && creature.Seen)
            {
                string progress = game.Melody.Unlocked
                    ? "The piano has answered. Play, and the moons will learn their names again."
                    : game.MelodyRepaired
                        ? "The score is whole. Perform it at the altar and let the piano answer."
                        : game.Melody.Count > 0
                            ? "You carry " + game.Melody.Count + " of the five pages. The home altar is keeping their place."
                            : creature.RepeatLines[0];
                game.OpenDialogue(creature.Speaker, progress, "Farewell");
                return;
            }
            string[] pages = creature.Seen ? creature.RepeatLines : creature.Lines;
            game.OpenDialogueSequence(creature.Speaker, pages, creature.Seen ? "Farewell" : "I will remember", () =>
            {
                if (creature.Seen) return;
                creature.Seen = true; SeenCount++;
                creature.Target.Tint(Color.Lerp(creature.Target.BaseColor, DemoGame.Accent, .45f));
                game.Audio.Note(game.Equipped, creature.Target.Index % 7);
            });
        }
    }
}
