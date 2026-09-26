using System;

namespace FlyMeToTheMoon.Demo
{
    public sealed class DemoRhythm
    {
        public const int NoteCount = 24;
        public const double Step = 0.85;
        public const double CountIn = 2.0;
        public const double GoodWindow = 0.20;
        public const double PerfectWindow = 0.10;
        private static readonly int[] Phrase = { 0, 1, 2, 1, 3, 2, 1, 0, 0, 2, 3, 2, 1, 0, 1, 3, 2, 1, 0, 1, 2, 3, 1, 0 };
        public readonly int[] Results = new int[NoteCount]; // 0 pending, 1 miss, 2 good, 3 perfect
        public int Hits { get; private set; }
        public int Perfect { get; private set; }
        public int Misses { get; private set; }
        public int Variant { get; private set; }
        public double StartDsp { get; private set; }
        public bool Running { get; private set; }
        public double Duration => TimeOf(NoteCount - 1) + 1.0;
        public bool Passed => Hits / (float)NoteCount >= 0.70f;
        public int Lane(int index) => (Phrase[index] + Variant) % 4;
        public double TimeOf(int index) => CountIn + index * Step;
        public void Begin(double dsp, int variant)
        {
            Array.Clear(Results, 0, Results.Length);
            Hits = Perfect = Misses = 0;
            StartDsp = dsp;
            Variant = variant % 4;
            Running = true;
        }
        public string Hit(int lane, double elapsed)
        {
            if (!Running) return "";
            int best = -1;
            double closest = GoodWindow + 0.000001;
            for (int i = 0; i < NoteCount; i++)
            {
                double error = Math.Abs(elapsed - TimeOf(i));
                if (Results[i] == 0 && Lane(i) == lane && error < closest)
                { best = i; closest = error; }
            }
            if (best < 0) return "Keep the beat";
            bool perfect = closest <= PerfectWindow + 0.000001;
            Results[best] = perfect ? 3 : 2;
            Hits++;
            if (perfect) Perfect++;
            return perfect ? "PERFECT" : "GOOD";
        }
        public bool Tick(double elapsed)
        {
            if (!Running) return false;
            for (int i = 0; i < NoteCount; i++)
                if (Results[i] == 0 && elapsed > TimeOf(i) + GoodWindow)
                { Results[i] = 1; Misses++; }
            if (elapsed < Duration) return false;
            Running = false;
            return true;
        }
        public void Stop() => Running = false;
    }
}
