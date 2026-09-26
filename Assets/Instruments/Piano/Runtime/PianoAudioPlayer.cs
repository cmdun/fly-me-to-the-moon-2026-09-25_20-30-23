using UnityEngine;

namespace FlyMeToTheMoon.Instruments
{
    // One player-owned source keeps pickup sounds alive when the world pickup disappears.
    [DisallowMultipleComponent, RequireComponent(typeof(AudioSource))]
    public sealed class PianoAudioPlayer : MonoBehaviour
    {
        private AudioSource source;
        public AudioClip LastPlayedClip { get; private set; }

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
        }

        public void Play(AudioClip clip)
        {
            if (clip == null) return;
            LastPlayedClip = clip;
            source.PlayOneShot(clip);
        }
    }
}
