using System;
using UnityEngine;

namespace FlyMeToTheMoon.Demo
{
    public enum FragmentChallenge { Echo, Beat, Targets, Code, Maze }

    // Pure challenge rules: the same clocks and input paths drive the UI and tests.
    public sealed class DemoChallenge
    {
        public static readonly string[] Names = { "Echo memory", "Pulse passage", "Shooting stars", "Note cipher", "Silent maze" };
        public static readonly string[] Instructions = {
            "Watch and listen to six notes, then repeat them with keys 1–7. Two wrong notes break the echo. You have 24 seconds after the preview.",
            "Use A/S/D/F as the notes cross the white line. Hit at least 10 of 12 within the narrow timing window. Wrong keys count as mistakes.",
            "Left-click eight moving stars in 16 seconds. Aim carefully: three misses end the attempt.",
            "Keys 1–7 are C D E F G A B. Follow six number clues. Count up or down, wrapping between 7 and 1. Two wrong answers or 22 seconds ends the attempt.",
            "Guide the gold note with WASD through the three openings to the green exit. Walls return you to the start. Three collisions or 22 seconds ends the attempt."
        };
        public FragmentChallenge Kind { get; private set; }
        public int Index => (int)Kind;
        public int Step { get; private set; }
        public int Strikes { get; private set; }
        public float Elapsed { get; private set; }
        public bool Finished { get; private set; }
        public bool Success { get; private set; }
        public Vector2 MazePosition { get; private set; }
        public readonly int[] Sequence = new int[12];
        public readonly int[] Intervals = new int[6];
        public readonly bool[] Judged = new bool[12];
        public const float PreviewDuration = 5.2f;
        public const float BeatWindow = .12f;
        private float collisionUntil;
        public bool Previewing => Kind == FragmentChallenge.Echo && Elapsed < PreviewDuration;
        public int PreviewNote => Previewing && Elapsed >= .5f && Elapsed < 4.7f && (Elapsed - .5f) % .7f < .5f
            ? Sequence[Mathf.Min(5, (int)((Elapsed - .5f) / .7f))] : -1;
        public float Limit => Kind == FragmentChallenge.Echo ? PreviewDuration + 24 : Kind == FragmentChallenge.Beat ? BeatTime(11) + .5f : Kind == FragmentChallenge.Targets ? 16 : 22;
        public float Remaining => Mathf.Max(0, Limit - Elapsed);
        public static float BeatTime(int i) => 2 + i * .65f;
        public static float MazeGap(int i) => i == 1 ? -.48f : .48f;
        public Vector2 TargetPosition => new Vector2(Mathf.Sin(Elapsed * (1.7f + Step * .08f) + Step * 1.9f) * .78f,
            Mathf.Cos(Elapsed * 2.1f + Step * 2.3f) * .65f);
        public string CodeClue => Step == 0 ? "Start on key " + (Sequence[0] + 1) :
            "From your last answer, count " + (Intervals[Mathf.Min(Step, 5)] > 0 ? "UP " : "DOWN ") + Mathf.Abs(Intervals[Mathf.Min(Step, 5)]);
        public void Begin(int index, int seed)
        {
            Kind = (FragmentChallenge)index; Step = Strikes = 0; Elapsed = collisionUntil = 0;
            Finished = Success = false; MazePosition = new Vector2(-.88f, -.75f);
            Array.Clear(Judged, 0, Judged.Length);
            var random = new System.Random(seed ^ (index * 7919));
            for (int i = 0; i < Sequence.Length; i++) Sequence[i] = random.Next(index == 1 ? 4 : 7);
            for (int i = 1; i < 6; i++)
            {
                Intervals[i] = random.Next(1, 4) * (random.Next(2) == 0 ? -1 : 1);
                if (Kind == FragmentChallenge.Code) Sequence[i] = (Sequence[i - 1] + Intervals[i] + 7) % 7;
            }
        }
        public void Tick(float delta)
        {
            if (Finished) return;
            Elapsed += Mathf.Max(0, delta);
            if (Kind == FragmentChallenge.Beat)
                for (int i = 0; i < 12; i++)
                    if (!Judged[i] && Elapsed > BeatTime(i) + BeatWindow) { Judged[i] = true; Strikes++; }
            if (Kind == FragmentChallenge.Beat && Strikes > 2) Finish(false);
            if (Elapsed >= Limit) Finish(Kind == FragmentChallenge.Beat && Step >= 10 && Strikes <= 2);
        }
        public bool Note(int note)
        {
            if (Finished || Previewing) return false;
            if (Kind == FragmentChallenge.Echo || Kind == FragmentChallenge.Code)
            {
                if (note != Sequence[Step]) { if (++Strikes >= 2) Finish(false); return false; }
                if (++Step == 6) Finish(true);
                return true;
            }
            if (Kind != FragmentChallenge.Beat) return false;
            for (int i = 0; i < 12; i++)
                if (!Judged[i] && Sequence[i] == note && Mathf.Abs(Elapsed - BeatTime(i)) <= BeatWindow)
                { Judged[i] = true; Step++; return true; }
            if (++Strikes > 2) Finish(false);
            return false;
        }
        public bool Shoot(Vector2 point)
        {
            if (Finished || Kind != FragmentChallenge.Targets) return false;
            if (Vector2.Distance(point, TargetPosition) > .095f) { if (++Strikes >= 3) Finish(false); return false; }
            if (++Step == 8) Finish(true);
            return true;
        }
        public void Move(Vector2 direction, float delta)
        {
            if (Finished || Kind != FragmentChallenge.Maze || Elapsed < collisionUntil) return;
            Vector2 movement = Vector2.ClampMagnitude(direction, 1) * (.7f * Mathf.Max(0, delta));
            int steps = Mathf.Max(1, Mathf.CeilToInt(movement.magnitude / .015f));
            for (int s = 0; s < steps; s++)
            {
                Vector2 next = MazePosition + movement / steps;
                next.x = Mathf.Clamp(next.x, -.94f, .94f); next.y = Mathf.Clamp(next.y, -.88f, .88f);
                for (int i = 0; i < 3; i++)
                    if (Mathf.Abs(next.x - (i - 1) * .48f) < .07f && Mathf.Abs(next.y - MazeGap(i)) > .18f)
                    {
                        MazePosition = new Vector2(-.88f, -.75f); collisionUntil = Elapsed + .5f;
                        if (++Strikes >= 3) Finish(false);
                        return;
                    }
                MazePosition = next;
                if (Vector2.Distance(next, new Vector2(.88f, .75f)) < .11f) { Step = 1; Finish(true); return; }
            }
        }
        private void Finish(bool passed) { Finished = true; Success = passed; }
    }
}
