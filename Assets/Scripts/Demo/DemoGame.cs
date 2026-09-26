using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

namespace FlyMeToTheMoon.Demo
{
    public enum DemoState { Title, Explore, Rhythm, Paused, Result, Win }

    [DefaultExecutionOrder(100)]
    public sealed class DemoGame : MonoBehaviour
    {
        public static readonly string[] Instruments = { "Flute", "Piano", "Bells", "Drums", "Harp", "Synth" };
        public static readonly Color Accent = new Color(0.4f, 0.95f, 0.8f);
        public TMP_FontAsset Font;
        public Material Material;
        public PlayerController Player { get; private set; }
        public PlanetManager Planets { get; private set; }
        public DemoAudio Audio { get; private set; }
        public DemoHud Hud { get; private set; }
        public Transform WorldRoot { get; private set; }
        public readonly List<DemoQuest> Quests = new List<DemoQuest>();
        public readonly List<DemoTarget> Targets = new List<DemoTarget>();
        public readonly DemoRhythm Rhythm = new DemoRhythm();
        public DemoQuest Performing { get; private set; }
        public DemoState State { get; private set; }
        public int Completed { get; private set; }
        public int Equipped { get; private set; }
        public int Shots { get; private set; }
        public string Feedback { get; private set; } = "";
        public string Message { get; private set; } = "";
        public bool LastPassed { get; private set; }
        public bool MapVisible { get; private set; }
        public DemoQuest ActiveQuest => Completed < Quests.Count ? Quests[Completed] : null;
        public double SongTime => AudioSettings.dspTime - Rhythm.StartDsp;
        private DemoState beforePause;
        private float nextShot, messageUntil;
        private int seenBoost;
        private readonly int[] sequence = { 1, 0, 2 };

        private void Start()
        {
            Time.timeScale = 1;
            Player = FindFirstObjectByType<PlayerController>();
            Planets = FindFirstObjectByType<PlanetManager>();
            if (Font == null || Material == null || Player == null || Planets == null)
            { Debug.LogError("Full demo is missing a font, material, or prototype systems."); enabled = false; return; }
            if (Camera.main.GetComponent<AudioListener>() == null) Camera.main.gameObject.AddComponent<AudioListener>();
            Audio = gameObject.AddComponent<DemoAudio>();
            WorldRoot = new GameObject("Demo quest objects").transform;
            foreach (var body in Player.gravityManager.bodies)
                if (body != Planets.respawnPlanet) Quests.Add(DemoWorld.CreateQuest(this, body, Quests.Count));
            var practicePosition = Planets.respawnPlanet.Center + Vector2.up * (Planets.respawnPlanet.radius + 1.4f) + Vector2.right * 2;
            DemoWorld.Target(this, null, DemoTargetKind.Practice, 0, practicePosition, Accent, "PRACTICE\nshoot me");
            Hud = gameObject.AddComponent<DemoHud>();
            Hud.Build(this);
            Camera.main.gameObject.AddComponent<DemoZoom>().Game = this;
            SetState(DemoState.Title);
        }

        private void Update()
        {
            if (Hud == null) return;
            var keys = Keyboard.current;
            if (keys != null && keys.escapeKey.wasPressedThisFrame)
            {
                if (State == DemoState.Paused) Resume();
                else if (State == DemoState.Explore || State == DemoState.Rhythm) Pause();
                return;
            }
            if (State == DemoState.Title)
            { if (keys != null && keys.enterKey.wasPressedThisFrame) StartGame(); return; }
            if (State == DemoState.Paused) return;
            if (State == DemoState.Result || State == DemoState.Win)
            {
                if (keys != null && keys.enterKey.wasPressedThisFrame) Continue();
                if (keys != null && keys.rKey.wasPressedThisFrame && !LastPassed) BeginRhythm(Performing);
                return;
            }
            if (State == DemoState.Rhythm)
            {
                if (keys != null)
                {
                    if (keys.aKey.wasPressedThisFrame) PlayLane(0);
                    if (keys.sKey.wasPressedThisFrame) PlayLane(1);
                    if (keys.dKey.wasPressedThisFrame) PlayLane(2);
                    if (keys.fKey.wasPressedThisFrame) PlayLane(3);
                }
                if (Rhythm.Tick(SongTime)) EndRhythm();
                return;
            }
            if (keys != null)
            {
                if (keys.eKey.wasPressedThisFrame) Interact();
                if (keys.tabKey.wasPressedThisFrame) CycleInstrument();
                if (keys.mKey.wasPressedThisFrame) ToggleMap();
                if (keys.digit1Key.wasPressedThisFrame) FreeNote(0);
                if (keys.digit2Key.wasPressedThisFrame) FreeNote(1);
                if (keys.digit3Key.wasPressedThisFrame) FreeNote(2);
                if (keys.digit4Key.wasPressedThisFrame) FreeNote(3);
            }
            if (State != DemoState.Explore) return;
            if (Mouse.current != null && Mouse.current.leftButton.isPressed && Time.time >= nextShot
                && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
                ShootToward(Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()));
            foreach (var q in Quests)
            {
                bool active = q.Index <= Completed;
                q.Walk.gameObject.SetActive(active && !q.Fragments[0] && Vector2.Distance(Player.Body.position, q.Walk.transform.position) < 3f);
                if (q == ActiveQuest)
                {
                    if (Vector2.Distance(Player.Body.position, q.Walk.transform.position) < 0.7f) Collect(q, 0);
                    if (q.PuzzleSolved && Vector2.Distance(Player.Body.position, q.Reward.transform.position) < 0.7f) Collect(q, 2);
                }
            }
            if (Player.FluteJumpCount != seenBoost)
            { seenBoost = Player.FluteJumpCount; Audio.Note(Equipped, 3); }
            if (Time.unscaledTime > messageUntil) Message = "";
        }

        public void StartGame()
        {
            SetState(DemoState.Explore);
            Tell("A/D walk  |  Space jump, Space again to fly  |  Mouse shoots notes. Try the practice target!");
        }
        private void SetState(DemoState state)
        {
            State = state;
            bool exploring = state == DemoState.Explore;
            Player.enabled = exploring;
            Planets.enabled = exploring;
            Player.Body.simulated = exploring;
            Time.timeScale = state == DemoState.Paused ? 0 : 1;
            if (!exploring) foreach (var shot in FindObjectsByType<DemoProjectile>(FindObjectsSortMode.None)) Destroy(shot.gameObject);
            Hud.RefreshPanels();
        }
        public void Tell(string message)
        { Message = message; messageUntil = Time.unscaledTime + 6f; }
        public void CycleInstrument()
        { Equipped = (Equipped + 1) % (Completed + 1); Audio.Note(Equipped, 0); Tell("Equipped " + Instruments[Equipped] + ". Play notes with 1 / 2 / 3 / 4."); }
        public void ToggleMap() { MapVisible = !MapVisible; Hud.RefreshPanels(); }
        public void ToggleAudio() { Audio.SetMuted(!Audio.Muted); Hud.RefreshPanels(); }
        public void FreeNote(int lane)
        {
            if (State != DemoState.Explore) return;
            Audio.Note(Equipped, lane);
            Emit(Player.Up);
        }
        public void ShootToward(Vector2 position)
        {
            if (State != DemoState.Explore || Time.time < nextShot) return;
            nextShot = Time.time + 0.22f;
            Vector2 direction = position - Player.Body.position;
            if (direction.sqrMagnitude < 0.01f) direction = Player.Up;
            Audio.Note(Equipped, Shots % 4); Shots++;
            Emit(direction.normalized);
        }
        private void Emit(Vector2 direction)
        {
            Vector2 position = Player.Body.position + direction * 0.42f;
            var shape = DemoWorld.Shape(WorldRoot, "Musical note", position, new Vector2(0.2f, 0.15f), Accent, Material, true);
            var stem = DemoWorld.Shape(shape.transform, "Stem", position + Vector2.up * 0.12f + Vector2.right * 0.06f,
                new Vector2(0.045f, 0.25f), Accent, Material);
            var projectile = shape.gameObject.AddComponent<DemoProjectile>();
            projectile.Game = this; projectile.Direction = direction;
        }
        public void HitTarget(DemoTarget target)
        {
            if (State != DemoState.Explore) return;
            if (target.Kind == DemoTargetKind.Practice)
            { Tell("Nice! Notes can collect floating pages and activate numbered resonators. Double jump upward to the Piano moon."); Audio.Note(0, 3); return; }
            var q = target.Quest;
            if (q != ActiveQuest) { Tell(q.Unlocked ? "This moon already sings." : "Restore the earlier moon first. Press M for the route."); return; }
            if (target.Kind == DemoTargetKind.ShotFragment) Collect(q, 1);
            if (target.Kind != DemoTargetKind.Resonator || q.PuzzleSolved || q.PlayingSequence) return;
            Audio.Note(q.Index + 1, target.Index);
            if (target.Index == sequence[q.SequenceStep]) q.SequenceStep++;
            else { q.SequenceStep = 0; Tell("Sequence reset. Press E beside the resonators to hear 2 - 1 - 3 again."); }
            if (q.SequenceStep == sequence.Length)
            {
                q.PuzzleSolved = true; q.Reward.gameObject.SetActive(true);
                foreach (var resonator in q.Resonators) resonator.Tint(Accent);
                Tell("Sequence solved! Walk into PAGE 3 beneath the resonators.");
            }
        }
        public bool Collect(DemoQuest q, int index)
        {
            if (State != DemoState.Explore || q != ActiveQuest || index < 0 || index > 2 || q.Fragments[index]) return false;
            if (index == 2 && !q.PuzzleSolved) return false;
            q.Fragments[index] = true;
            (index == 0 ? q.Walk : index == 1 ? q.Shot : q.Reward).gameObject.SetActive(false);
            Audio.Note(q.Index + 1, index);
            Tell(q.Count == 3 ? "Score complete! Return to the " + q.Instrument + " altar and press E." : "Music page recovered: " + q.Count + "/3.");
            return true;
        }
        public void Interact()
        {
            if (State != DemoState.Explore) return;
            foreach (var q in Quests)
            {
                if (Vector2.Distance(Player.Body.position, q.Altar) < 1.5f)
                {
                    if (q.Unlocked) Tell(q.Instrument + " unlocked. Press Tab to equip it, then 1-4 or click to play.");
                    else if (q != ActiveQuest) Tell("Restore " + ActiveQuest.Instrument + " first. Press M for the route.");
                    else if (q.Count < 3) Tell("Find " + (3 - q.Count) + " more pages: ruins, floating note target, and resonator puzzle.");
                    else BeginRhythm(q);
                    return;
                }
                if (Vector2.Distance(Player.Body.position, q.Pedestal) < 1.8f && q == ActiveQuest)
                {
                    if (!q.PlayingSequence && !q.PuzzleSolved) StartCoroutine(Demonstrate(q));
                    else Tell(q.PuzzleSolved ? "The sequence is complete. Collect PAGE 3." : "Listen and watch...");
                    return;
                }
            }
            Tell("Walk beside an instrument altar or the numbered resonators, then press E. A/D follows the surface; double Space launches into space.");
        }
        private IEnumerator Demonstrate(DemoQuest q)
        {
            q.PlayingSequence = true; q.SequenceStep = 0;
            Tell("Watch, then shoot: 2 - 1 - 3. No timing limit.");
            foreach (int i in sequence)
            {
                q.Resonators[i].Tint(Color.white); Audio.Note(q.Index + 1, i);
                yield return new WaitForSeconds(0.45f);
                q.Resonators[i].Tint(q.Resonators[i].BaseColor);
                yield return new WaitForSeconds(0.15f);
            }
            q.PlayingSequence = false;
        }
        public bool BeginRhythm(DemoQuest q)
        {
            if (q == null || q != ActiveQuest || q.Count != 3) return false;
            Performing = q; Feedback = "Get ready"; LastPassed = false; MapVisible = false;
            Player.Body.linearVelocity = Vector2.zero;
            Rhythm.Begin(0, q.Index);
            Rhythm.Begin(Audio.Song(q.Index + 1, Rhythm), q.Index);
            SetState(DemoState.Rhythm);
            return true;
        }
        public void PlayLane(int lane)
        {
            if (State != DemoState.Rhythm) return;
            Feedback = Rhythm.Hit(lane, SongTime);
            Audio.Note(Performing.Index + 1, lane, 0.3f);
        }
        private void EndRhythm()
        {
            LastPassed = Rhythm.Passed;
            if (LastPassed && !Performing.Unlocked)
            {
                Performing.Unlocked = true; Completed++; Equipped = Completed;
                Audio.Note(Equipped, 3);
                foreach (var shape in Performing.Body.GetComponentsInChildren<PrototypeShape>())
                {
                    shape.color = Color.Lerp(shape.color, Accent, 0.35f); shape.Rebuild();
                }
            }
            SetState(Completed == Quests.Count ? DemoState.Win : DemoState.Result);
        }
        public void Pause()
        {
            if (State != DemoState.Explore && State != DemoState.Rhythm) return;
            beforePause = State; Audio.StopSong(); SetState(DemoState.Paused);
        }
        public void Resume()
        {
            if (State != DemoState.Paused) return;
            if (beforePause == DemoState.Rhythm) { Time.timeScale = 1; BeginRhythm(Performing); }
            else SetState(DemoState.Explore);
        }
        public void Continue()
        {
            Audio.StopSong(); SetState(DemoState.Explore);
            Tell(Completed == Quests.Count ? "The galaxy sings again! Explore freely and play all six instruments." : "Next: " + ActiveQuest.Instrument + ". Press M for the route. Travel via 612-B if needed.");
        }
        public void Restart()
        { Time.timeScale = 1; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
        public void Quit() { Time.timeScale = 1; Application.Quit(); }
        private void OnDestroy() { Time.timeScale = 1; }
    }

    public sealed class DemoZoom : MonoBehaviour
    {
        public DemoGame Game;
        private float ppu = 40f, speed;
        private void LateUpdate()
        {
            if (Game == null || Game.Player == null) return;
            var pixel = GetComponent<PixelPerfectCamera>();
            if (pixel == null) return;
            float target = Game.Player.FluteUsed ? 10f : Game.Player.IsGrounded ? 40f : 24f;
            ppu = Mathf.SmoothDamp(ppu, target, ref speed, 0.25f, Mathf.Infinity, Time.unscaledDeltaTime);
            pixel.assetsPPU = Mathf.RoundToInt(ppu);
        }
    }
}
