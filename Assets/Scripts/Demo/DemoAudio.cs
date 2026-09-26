using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlyMeToTheMoon.Demo
{
    public sealed class DemoAudio : MonoBehaviour
    {
        private const int Rate = 22050;
        private readonly Dictionary<int, AudioClip> tones = new Dictionary<int, AudioClip>();
        private readonly Dictionary<int, AudioClip> songs = new Dictionary<int, AudioClip>();
        private AudioSource effects;
        private AudioSource music;
        public bool Muted { get; private set; }
        private void Awake()
        {
            effects = gameObject.AddComponent<AudioSource>();
            music = gameObject.AddComponent<AudioSource>();
            effects.playOnAwake = music.playOnAwake = false;
            effects.spatialBlend = music.spatialBlend = 0;
            music.volume = 0.6f;
        }
        public void SetMuted(bool value)
        { Muted = value; effects.mute = music.mute = value; }
        private float Wave(double time, float frequency, int instrument)
        {
            double phase = 2 * Math.PI * frequency * time;
            switch (instrument)
            {
                case 1: return (float)(Math.Sin(phase) + 0.4 * Math.Sin(2 * phase) + 0.15 * Math.Sin(3 * phase)) / 1.55f;
                case 2: return (float)(Math.Sin(phase) + 0.5 * Math.Sin(2.76 * phase)) / 1.5f;
                case 3: return (float)(Math.Sin(phase * Math.Exp(-time * 6)) + 0.3 * Math.Sin(time * 7311)) / 1.3f;
                case 4: return (float)(Math.Sin(phase) + 0.25 * Math.Sin(3 * phase)) / 1.25f;
                case 5: return (float)(Math.Sin(phase) + 0.25 * Math.Sin(2 * phase) + 0.12 * Math.Sin(5 * phase)) / 1.37f;
                default: return (float)(Math.Sin(phase) + 0.12 * Math.Sin(2 * phase)) / 1.12f;
            }
        }
        private void AddTone(float[] data, double onset, int instrument, int lane, float volume)
        {
            int[] scale = { 0, 2, 4, 7 };
            float frequency = 261.6256f * Mathf.Pow(2, scale[lane % 4] / 12f);
            int first = (int)(onset * Rate), count = (int)(0.65 * Rate);
            for (int j = 0; j < count && first + j < data.Length; j++)
            {
                if (first + j < 0) continue;
                double t = j / (double)Rate;
                float attack = Mathf.Min(1, j / (Rate * 0.012f));
                float envelope = attack * Mathf.Pow(1 - j / (float)count, instrument == 0 ? 1.5f : 3f);
                data[first + j] += Wave(t, frequency, instrument) * envelope * volume;
            }
        }
        public void Note(int instrument, int lane, float volume = 0.45f)
        {
            int key = instrument * 4 + lane % 4;
            if (!tones.TryGetValue(key, out var clip))
            {
                float[] data = new float[(int)(0.7 * Rate)];
                AddTone(data, 0, instrument, lane, 0.6f);
                clip = AudioClip.Create("Demo instrument " + key, data.Length, 1, Rate, false);
                clip.SetData(data, 0); tones.Add(key, clip);
            }
            effects.PlayOneShot(clip, volume);
        }
        public double Song(int instrument, DemoRhythm chart)
        {
            music.Stop();
            if (!songs.TryGetValue(instrument, out var clip))
            {
                float[] data = new float[(int)((chart.Duration + 0.5) * Rate)];
                AddTone(data, 0.25, 3, 0, 0.12f); AddTone(data, 1.1, 3, 0, 0.12f);
                for (int i = 0; i < DemoRhythm.NoteCount; i++)
                    AddTone(data, chart.TimeOf(i), instrument, chart.Lane(i), 0.38f);
                clip = AudioClip.Create("Original moon melody " + instrument, data.Length, 1, Rate, false);
                clip.SetData(data, 0); songs.Add(instrument, clip);
            }
            music.clip = clip;
            double start = AudioSettings.dspTime + 0.2;
            music.PlayScheduled(start);
            return start;
        }
        public void StopSong() => music.Stop();
        private void OnDestroy()
        {
            foreach (var clip in tones.Values) Destroy(clip);
            foreach (var clip in songs.Values) Destroy(clip);
        }
    }
}
