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
        public static float FrequencyForNote(int note)
        {
            int[] scale = { 0, 2, 4, 5, 7, 9, 11 };
            return 261.6256f * Mathf.Pow(2, scale[Mathf.Clamp(note, 0, 6)] / 12f);
        }
        private void AddTone(float[] data, double onset, int instrument, int lane, float volume)
        {
            float frequency = FrequencyForNote(lane);
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
            int key = instrument * 7 + lane % 7;
            if (!tones.TryGetValue(key, out var clip))
            {
                float[] data = new float[(int)(0.7 * Rate)];
                AddTone(data, 0, instrument, lane, 0.6f);
                clip = AudioClip.Create("Demo instrument " + key, data.Length, 1, Rate, false);
                clip.SetData(data, 0); tones.Add(key, clip);
            }
            effects.PlayOneShot(clip, volume);
        }
        void AddVoice(float[] data,double onset,int midi,double duration,int instrument,float volume)
        {
            float frequency=440*Mathf.Pow(2,(midi-69)/12f);int first=(int)(onset*Rate),count=(int)(duration*Rate);
            for(int j=0;j<count && first+j<data.Length;j++)
            {
                double t=j/(double)Rate;
                float envelope=Mathf.Min(1,j/(Rate*.014f))*Mathf.Pow(1-j/(float)count,instrument==0?1.3f:2.8f);
                double phaseTime=instrument==0?t+.00008*Math.Sin(34*t):t;
                data[first+j]+=Wave(phaseTime,frequency,instrument)*envelope*volume;
            }
        }
        void Brush(float[] data,double onset,float volume)
        {
            int first=(int)(onset*Rate),count=(int)(Rate*.095);uint noise=73421;
            for(int j=0;j<count && first+j<data.Length;j++)
            {noise=noise*1664525+1013904223;float sample=((noise>>16)/32767.5f)-1;data[first+j]+=sample*volume*Mathf.Pow(1-j/(float)count,4);}
        }
        public double Song(int instrument, DemoRhythm chart)
        {
            music.Stop();
            if (!songs.TryGetValue(instrument, out var clip))
            {
                float[] data = new float[(int)((chart.Duration + 0.5) * Rate)];
                // Original chamber-jazz arrangement: swung melody, walking bass, soft chords and brushes.
                for(int count=0;count<3;count++) AddTone(data,.25+count*.7,3,0,.07f);
                int[] roots={48,45,50,43,52,45,50,43};
                int[][] chords={new[]{60,64,67,71},new[]{60,64,67,69},new[]{60,65,69,74},new[]{59,62,65,69},
                    new[]{59,62,67,71},new[]{61,64,67,69},new[]{60,65,69,74},new[]{59,62,65,67}};
                for(int bar=0;bar<8;bar++)
                {
                    double begin=DemoRhythm.CountIn+bar*4*DemoRhythm.Beat;
                    for(int beat=0;beat<4;beat++)
                    {
                        double onset=begin+beat*DemoRhythm.Beat;
                        AddVoice(data,onset,roots[bar]-12+(beat%2==1?7:0),.6,1,.18f);
                        Brush(data,onset,.045f);Brush(data,onset+DemoRhythm.Beat*2/3,.023f);
                        if(beat==1 || beat==3)foreach(int pitch in chords[bar])AddVoice(data,onset+.025,pitch,.65,1,.035f);
                    }
                }
                for(int i=0;i<DemoRhythm.NoteCount;i++) AddVoice(data,chart.TimeOf(i),chart.Pitch(i),.57,0,.25f);
                foreach(int pitch in new[]{48,60,64,67,71})AddVoice(data,chart.TimeOf(23),pitch,.95,1,.045f);
                for(int i=0;i<data.Length;i++)data[i]=Mathf.Clamp(data[i],-.9f,.9f);
                clip = AudioClip.Create(DemoRhythm.SongTitle+" — original swing", data.Length, 1, Rate, false);
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
