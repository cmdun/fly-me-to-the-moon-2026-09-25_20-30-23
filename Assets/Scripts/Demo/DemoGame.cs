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
    public enum DemoState { Title, Explore, Dialogue, Rhythm, Paused, Result, Win }

    [DefaultExecutionOrder(100)]
    public sealed class DemoGame : MonoBehaviour
    {
        public static readonly string[] Instruments = { "Flute", "Piano" };
        public static readonly Color Accent = new Color(0.4f, 0.95f, 0.8f);
        public TMP_FontAsset Font;
        public Material Material;
        public int WorldSeed;
        public int Seed { get; private set; }
        public bool MelodyRepaired { get; private set; }
        public DemoQuest Melody => Quests.Count == 0 ? null : Quests[0];
        public DemoTarget NearbyTarget { get; private set; }
        public string DialogueTitle { get; private set; }
        public string DialogueText { get; private set; }
        public string DialogueChoice { get; private set; }
        private System.Action dialogueAction;
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
            Seed = WorldSeed == 0 ? System.Guid.NewGuid().GetHashCode() : WorldSeed;
            var bodies = DemoGalaxy.Generate(this, Seed);
            Quests.Add(DemoWorld.CreateJourney(this, bodies, Seed));
            Planets.lostDistance = 18; Planets.maxAirborneSeconds = 25;
            ApplyInstrument();
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
                else if (State == DemoState.Dialogue) CloseDialogue();
                else if (State == DemoState.Explore || State == DemoState.Rhythm) Pause();
                return;
            }
            if (State == DemoState.Title)
            { if (keys != null && keys.enterKey.wasPressedThisFrame) StartGame(); return; }
            if (State == DemoState.Paused) return;
            if (State == DemoState.Dialogue)
            {
                if (keys != null && (keys.eKey.wasPressedThisFrame || keys.enterKey.wasPressedThisFrame)) AdvanceDialogue();
                return;
            }
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
                if (keys.digit5Key.wasPressedThisFrame) FreeNote(4);
                if (keys.digit6Key.wasPressedThisFrame) FreeNote(5);
                if (keys.digit7Key.wasPressedThisFrame) FreeNote(6);
            }
            if (State != DemoState.Explore) return;
            if (Mouse.current != null && (Player.IsGrounded ? Mouse.current.leftButton.isPressed : Mouse.current.leftButton.wasPressedThisFrame) && Time.time >= nextShot
                && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
                ShootToward(Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()));
            NearbyTarget = FindNearbyTarget();
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

        }
        private void SetState(DemoState state)
        {
            State = state;
            if (state == DemoState.Explore) nextShot = Time.time + .15f;
            bool exploring = state == DemoState.Explore;
            Player.enabled = exploring;
            Planets.enabled = exploring;
            Player.Body.simulated = exploring;
            Time.timeScale = state == DemoState.Paused || state == DemoState.Dialogue ? 0 : 1;
            if (!exploring) foreach (var shot in FindObjectsByType<DemoProjectile>(FindObjectsSortMode.None)) Destroy(shot.gameObject);
            Hud.RefreshPanels();
        }
        public void Tell(string message)
        { Message = message; messageUntil = Time.unscaledTime + 6f; }
        public void ApplyInstrument()
        {
            Player.jumpSpeed = Equipped == 0 ? 5f : 6f;
            Player.fluteBoost = Equipped == 0 ? 3f : 5f;
            Player.maxFlightSpeed = Equipped == 0 ? 9f : 13f;
            Player.airAcceleration = Equipped == 0 ? 4f : 6f;
        }
        public void CycleInstrument()
        {
            if (State != DemoState.Explore || !Player.IsGrounded) return;
            Equipped = (Equipped + 1) % (Completed + 1); ApplyInstrument(); Audio.Note(Equipped, 0);
        }
        public void ToggleMap() { MapVisible = !MapVisible; Hud.RefreshPanels(); }
        public void ToggleAudio() { Audio.SetMuted(!Audio.Muted); Hud.RefreshPanels(); }
        public void FreeNote(int lane)
        {
            if (State != DemoState.Explore || lane < 0 || lane > 6) return;
            Audio.Note(Equipped, lane);

        }
        public void ShootToward(Vector2 position)
        {
            if (State != DemoState.Explore || Time.time < nextShot) return;
            Vector2 direction = position - Player.Body.position;
            if (direction.sqrMagnitude < 0.01f) direction = Player.Up;
            direction.Normalize();
            if (!Player.IsGrounded && !Player.TryDirectionalBoost(-direction)) return;
            nextShot = Time.time + 0.22f;
            Audio.Note(Equipped, Shots % 7); Shots++;
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
            if (q != ActiveQuest) {  return; }
            if (target.Kind == DemoTargetKind.ShotFragment) Collect(q, 1);
            if (target.Kind != DemoTargetKind.Resonator || q.PuzzleSolved || q.PlayingSequence) return;
            Audio.Note(q.Index + 1, target.Index);
            if (target.Index == sequence[q.SequenceStep]) q.SequenceStep++;
            else { q.SequenceStep = 0;  }
            if (q.SequenceStep == sequence.Length)
            {
                q.PuzzleSolved = true; q.Reward.gameObject.SetActive(true);
                foreach (var resonator in q.Resonators) resonator.Tint(Accent);
                q.PedestalTarget.gameObject.SetActive(false);
            }
        }
        public bool Collect(DemoQuest q, int index)
        {
            if (State != DemoState.Explore || q != ActiveQuest || index < 0 || index > 2 || q.Fragments[index]) return false;
            if (index == 2 && !q.PuzzleSolved) return false;
            q.Fragments[index] = true;
            (index == 0 ? q.Walk : index == 1 ? q.Shot : q.Reward).gameObject.SetActive(false);
            Audio.Note(q.Index + 1, index);
            Tell("Score " + q.Count + "/3");
            return true;
        }
        public DemoTarget FindNearbyTarget()
        {
            DemoTarget nearest = null; float distance = 2.1f;
            foreach (var target in Targets)
            {
                if (!target.gameObject.activeInHierarchy) continue;
                if (target.Body != null && target.Body != Player.gravityManager.CurrentBody) continue;
                float d = Vector2.Distance(Player.Body.position, target.transform.position);
                if (d < distance) { distance = d; nearest = target; }
            }
            return nearest;
        }
        public void Interact()
        {
            if (State != DemoState.Explore || MapVisible) return;
            NearbyTarget = FindNearbyTarget();
            if (NearbyTarget == null) return;
            var target = NearbyTarget; var q = Melody;
            if (target.Kind == DemoTargetKind.Altar)
            {
                if (q.Unlocked) OpenDialogue("Home altar", "The melody is whole. Your piano is ready to play.", "Leave");
                else if (q.Count < 3) OpenDialogue("The missing melody", "Find the ruins page on " + q.FragmentBodies[0].planetId + ", the floating page on " + q.FragmentBodies[1].planetId + ", and the echo page on " + q.FragmentBodies[2].planetId + ". Bring all three back here.", "Explore");
                else if (!MelodyRepaired) OpenDialogue("Repair the melody", "All three pages are here. Piece them together at this altar, then perform the restored melody.", "Repair score", () => {
                    MelodyRepaired = true;
                    OpenDialogue("Score restored", "The missing melody is ready. Complete the performance to awaken your piano.", "Perform", () => BeginRhythm(q));
                });
                else OpenDialogue("Home altar", "The repaired score is ready to perform.", "Perform", () => BeginRhythm(q));
            }
            else if (target.Kind == DemoTargetKind.Resonator || target.Kind == DemoTargetKind.EchoPedestal)
                OpenDialogue("Echo stones", q.PuzzleSolved ? "The echo page is yours. Return to the home altar." : "Listen to the three stones. Shoot them in the same order: middle, first, last. A wrong note restarts the sequence.", q.PuzzleSolved ? "Leave" : "Listen", () => { if(!q.PuzzleSolved && !q.PlayingSequence) StartCoroutine(Demonstrate(q)); });
            else if (target.Kind == DemoTargetKind.ShotFragment)
                OpenDialogue("Floating page", "A page hangs beyond reach. Aim at it and shoot a musical note from the ground.", "Try it");
            else OpenDialogue("Lost page", "Walk into the page to collect it. The home altar holds the rest of the melody.", "Collect");
        }
        public void OpenDialogue(string title, string text, string choice, System.Action action = null)
        {
            DialogueTitle = title; DialogueText = text; DialogueChoice = choice; dialogueAction = action;
            MapVisible = false; SetState(DemoState.Dialogue); Hud.BeginDialogue();
        }
        public void AdvanceDialogue()
        {
            if (State != DemoState.Dialogue) return;
            if (!Hud.DialogueComplete) { Hud.RevealDialogue(); return; }
            var action = dialogueAction; CloseDialogue(); action?.Invoke();
        }
        public void CloseDialogue()
        { dialogueAction = null; SetState(DemoState.Explore); }
        private IEnumerator Demonstrate(DemoQuest q)
        {
            q.PlayingSequence = true; q.SequenceStep = 0;

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
            if (q == null || q != ActiveQuest || q.Count != 3 || !MelodyRepaired || Vector2.Distance(Player.Body.position,q.Altar) > 2.1f) return false;
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
                Performing.Unlocked = true; Completed = 1; Equipped = 1; ApplyInstrument();
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
