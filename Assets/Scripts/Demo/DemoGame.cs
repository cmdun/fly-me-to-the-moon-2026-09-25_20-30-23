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
    public enum DemoState { Title, Explore, Dialogue, Challenge, Rhythm, Paused, Result, Win }

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
        public readonly DemoChallenge Challenge = new DemoChallenge();
        private int heardPreview = -1;

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
                else if (State == DemoState.Explore || State == DemoState.Rhythm || State == DemoState.Challenge) Pause();
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
            if (State == DemoState.Challenge)
            {
                UpdateChallenge(keys);
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
            if (!MapVisible && Mouse.current != null && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            {
                Vector2 aim = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                if (Mouse.current.leftButton.isPressed) ShootToward(aim);
                if (Mouse.current.rightButton.wasPressedThisFrame) RecoilToward(aim);
            }
            NearbyTarget = FindNearbyTarget();
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
            nextShot = Time.time + 0.22f;
            Audio.Note(Equipped, Shots % 7); Shots++;
            Emit(direction.normalized);
        }
        public bool RecoilToward(Vector2 position)
        {
            if (State != DemoState.Explore || MapVisible || Player.IsGrounded) return false;
            Vector2 direction = position - Player.Body.position;
            if (direction.sqrMagnitude < .01f) return false;
            if (!Player.TryDirectionalBoost(-direction.normalized)) return false;
            Emit(direction.normalized);
            return true;
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
        public bool Collect(DemoQuest q, int index)
        {
            if (State != DemoState.Challenge || q != ActiveQuest || index < 0 || index >= DemoQuest.FragmentCount || q.Fragments[index]) return false;
            if (!Challenge.Finished || !Challenge.Success || Challenge.Index != index) return false;
            q.Fragments[index] = true; q.Stations[index].gameObject.SetActive(false);
            Audio.Note(1, index); Tell("Score " + q.Count + "/5");
            return true;
        }
        public bool BeginChallenge(int index)
        {
            if (State != DemoState.Explore || index < 0 || index >= DemoQuest.FragmentCount || Melody.Fragments[index]) return false;
            var station = Melody.Stations[index];
            if (Player.gravityManager.CurrentBody != station.Body || Vector2.Distance(Player.Body.position, station.transform.position) > 2.1f) return false;
            Challenge.Begin(index, Seed); heardPreview = -1; MapVisible = false;
            SetState(DemoState.Challenge); return true;
        }
        private void UpdateChallenge(Keyboard keys)
        {
            if (Challenge.Finished)
            {
                if (keys != null && keys.enterKey.wasPressedThisFrame) FinishChallenge();
                return;
            }
            Challenge.Tick(Time.deltaTime);
            if (Challenge.PreviewNote != heardPreview)
            {
                heardPreview = Challenge.PreviewNote;
                if (heardPreview >= 0) Audio.Note(0, heardPreview);
            }
            if (keys == null) return;
            if (Challenge.Kind == FragmentChallenge.Beat)
            {
                var lanes = new[] { Key.A, Key.S, Key.D, Key.F };
                for (int i = 0; i < lanes.Length; i++) if (keys[lanes[i]].wasPressedThisFrame) ChallengeNote(i);
            }
            else if (Challenge.Kind == FragmentChallenge.Echo || Challenge.Kind == FragmentChallenge.Code)
            {
                var notes = new[] { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7 };
                for (int i = 0; i < notes.Length; i++) if (keys[notes[i]].wasPressedThisFrame) ChallengeNote(i);
            }
            else if (Challenge.Kind == FragmentChallenge.Maze)
                Challenge.Move(new Vector2((keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0),
                    (keys.wKey.isPressed ? 1 : 0) - (keys.sKey.isPressed ? 1 : 0)), Time.deltaTime);
        }
        public void ChallengeNote(int note)
        {
            if (State != DemoState.Challenge || Challenge.Finished) return;
            if (Challenge.Note(note)) Audio.Note(0, note);
        }
        public void FinishChallenge()
        {
            if (State != DemoState.Challenge || !Challenge.Finished) return;
            bool passed = Challenge.Success;
            if (passed) Collect(Melody, Challenge.Index);
            SetState(DemoState.Explore);
            if (!passed) Tell("Try the beacon again");
        }
        public void LeaveChallenge()
        {
            if (State != DemoState.Challenge) return;
            if (Challenge.Finished) FinishChallenge();
            else SetState(DemoState.Explore);
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
                else if (q.Count < DemoQuest.FragmentCount) OpenDialogue("The missing melody", "Five golden score pages wait on five moons. Each beacon holds a different challenge. Find them on the map and bring all five pages home.", "Explore");
                else if (!MelodyRepaired) OpenDialogue("Repair the melody", "All five pages are here. Piece them together at this altar, then perform the restored melody.", "Repair score", () => {
                    MelodyRepaired = true;
                    OpenDialogue("Score restored", "The missing melody is ready. Complete the performance to awaken your piano.", "Perform", () => BeginRhythm(q));
                });
                else OpenDialogue("Home altar", "The repaired score is ready to perform.", "Perform", () => BeginRhythm(q));
            }
            else if (target.Kind == DemoTargetKind.FragmentStation)
                OpenDialogue(DemoChallenge.Names[target.Index], DemoChallenge.Instructions[target.Index], "Begin challenge", () => BeginChallenge(target.Index));
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
        public bool BeginRhythm(DemoQuest q)
        {
            if (q == null || q != ActiveQuest || q.Count != DemoQuest.FragmentCount || !MelodyRepaired || Vector2.Distance(Player.Body.position,q.Altar) > 2.1f) return false;
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
            if (State != DemoState.Explore && State != DemoState.Rhythm && State != DemoState.Challenge) return;
            beforePause = State; Audio.StopSong(); SetState(DemoState.Paused);
        }
        public void Resume()
        {
            if (State != DemoState.Paused) return;
            if (beforePause == DemoState.Rhythm) { Time.timeScale = 1; BeginRhythm(Performing); }
            else SetState(beforePause == DemoState.Challenge ? DemoState.Challenge : DemoState.Explore);
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
