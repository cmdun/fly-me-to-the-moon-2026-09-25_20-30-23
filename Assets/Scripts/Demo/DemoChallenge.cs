using UnityEngine;

namespace FlyMeToTheMoon.Demo
{
    public enum FragmentChallenge { Relay, Giant, Storm, Shepherd, Echo }

    // Owns one temporary world encounter. Exploration physics and the real player remain active.
    public sealed class DemoChallenge
    {
        public static readonly string[] Names = { "Orbital Relay", "The Sleeping Giant", "Pulse Storm", "Starlight Shepherd", "Duet with Your Echo" };
        public static readonly string[] Instructions = {
            "Carry the spark through three gold gates to the receiver. Space jumps; Space again or right-click uses your one airborne boost. Land on blue shrines to save your progress and restore your normal jump opportunity. You can fly past a shrine for a shorter, riskier route. The second gate is angled; the last moves. R retries from your last shrine.",
            "Retrieve the page beyond the sleeping giant. Walking is quiet; jumps and flute shots raise awareness. Cracked red ground is noisy. Shoot a distant bell to draw its attention away, then approach the page and press E. Watch its changing sleeping position and awareness meter. If it wakes, try again from the beacon.",
            "Repair three circuits. Jump over low waves; stand inside the blue shelter for high waves. Warnings name each wave and its direction. Between bursts, shoot both exposed switches. Later circuits reverse direction and combine waves. A hit knocks you back and resets only the current circuit. R retries it.",
            "Guide three creatures into the green sanctuary. Hold Q to call. The first follows immediately; the second needs a sustained call; the third hears your destination but moves only after you release Q. The last two wake after the first arrives. Press E by the gate lever. Nearby shots frighten them; red patches scatter them. Call from nearby safe ground.",
            "Press C to record up to eight seconds of movement and shots. Walk onto the cyan plate, then wait there. E ends recording and starts playback. Your ghost repeats the route while you cross its gate and shoot the receiver. The second room also needs you on the gold plate when the receiver is hit. C rerecords immediately; the route and countdown stay visible."
        };
        public static string[] InstructionPages(int index)
        {
            var pages=new System.Collections.Generic.List<string>();string page="";
            foreach(string sentence in Instructions[index].Split(new[]{". "},System.StringSplitOptions.None))
            {
                if(page.Length+sentence.Length>230 && page.Length>0){pages.Add(page.Trim());page="";}
                page+=sentence.TrimEnd('.')+". ";
            }
            if(page.Length>0)pages.Add(page.Trim());return pages.ToArray();
        }
        public DemoGame Game { get; private set; }
        public WorldEncounter Encounter { get; private set; }
        public Transform Root { get; private set; }
        public int Index { get; private set; }
        public FragmentChallenge Kind => (FragmentChallenge)Index;
        public bool Active { get; private set; }
        public bool Finished => Encounter != null && Encounter.Complete;
        public bool Success => Finished;
        public float Elapsed { get; private set; }
        private float airborne;
        public void Begin(DemoGame game, int index)
        {
            End(); Game = game; Index = index; Elapsed = airborne = 0; Active = true;
            Root = new GameObject(Names[index]).transform; Root.SetParent(game.WorldRoot, false);
            switch (Kind)
            {
                case FragmentChallenge.Relay: Encounter = new RelayEncounter(this); break;
                case FragmentChallenge.Giant: Encounter = new GiantEncounter(this); break;
                case FragmentChallenge.Storm: Encounter = new StormEncounter(this); break;
                case FragmentChallenge.Shepherd: Encounter = new ShepherdEncounter(this); break;
                default: Encounter = new EchoEncounter(this); break;
            }
            Encounter.Reset();
        }
        public void Tick(float delta, bool calling)
        {
            if (!Active || Finished) return;
            Elapsed += delta; Encounter.Tick(delta, calling);
            airborne = Game.Player.IsGrounded ? 0 : airborne + delta;
            if (airborne > 25 || Game.Player.gravityManager.NearestSurfaceDistance(Game.Player.Body.position) > 18) Retry();
        }
        public void Retry()
        {
            if (!Active || Finished) return;
            airborne = 0; Game.ClearShots(); Encounter.Retries++; Encounter.Reset();
        }
        public void End()
        {
            Active = false;
            if (Root != null) { Root.gameObject.SetActive(false); Object.Destroy(Root.gameObject); }
            Root = null;
        }
    }
}
