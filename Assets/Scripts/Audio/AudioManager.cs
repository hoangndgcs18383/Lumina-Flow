using System.Collections.Generic;
using UnityEngine;

namespace LuminaFlow
{
    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }
        private AudioSource effects, music;
        private readonly List<AudioClip> generated = new List<AudioClip>();
        private readonly AudioClip[] notes = new AudioClip[5];
        private AudioClip tap, win;
        private void Awake()
        {
            Instance = this;
            effects = gameObject.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            effects.volume = .25f;
            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.volume = .055f;
            int[] steps = { 0, 2, 4, 7, 9 };
            for (int i = 0; i < notes.Length; i++) notes[i] = Tone("Mirror note", 440 * Mathf.Pow(2, steps[i] / 12f), .22f);
            tap = Tone("Soft tap", 330, .075f);
            win = Chord();
            music.clip = Ambient();
        }
        private void Start() => RefreshSettings();
        public void RefreshSettings()
        {
            if (ProgressStore.MusicEnabled) { if (!music.isPlaying) music.Play(); }
            else music.Stop();
        }
        public void PlayTurn(int count) { if (ProgressStore.SoundEnabled) effects.PlayOneShot(notes[(count - 1) % notes.Length]); }
        public void PlayTap() { if (ProgressStore.SoundEnabled) effects.PlayOneShot(tap); }
        public void PlayWin() { if (ProgressStore.SoundEnabled) effects.PlayOneShot(win, .8f); }
        private AudioClip Tone(string name, float frequency, float duration)
        {
            const int rate = 22050;
            var samples = new float[(int)(rate * duration)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                float envelope = Mathf.Min(t / .008f, 1) * Mathf.Pow(1 - t / duration, 2);
                samples[i] = (.7f * Mathf.Sin(t * frequency * Mathf.PI * 2) + .15f * Mathf.Sin(t * frequency * Mathf.PI * 4)) * envelope;
            }
            return Clip(name, samples, rate);
        }
        private AudioClip Chord()
        {
            const int rate = 22050;
            var samples = new float[rate];
            float[] frequencies = { 440f, 554.365f, 659.255f, 880f };
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                foreach (float frequency in frequencies)
                    samples[i] += Mathf.Sin(t * frequency * Mathf.PI * 2) * .22f * Mathf.Min(t / .015f, 1) * Mathf.Exp(-t * 4);
            }
            return Clip("Core awakened", samples, rate);
        }
        private AudioClip Ambient()
        {
            const int rate = 22050;
            var samples = new float[rate * 8];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                // Integer cycle counts make the loop seamless.
                samples[i] = (Mathf.Sin(2 * Mathf.PI * 110 * t) + .4f * Mathf.Sin(2 * Mathf.PI * 165 * t) + .25f * Mathf.Sin(2 * Mathf.PI * 220 * t)) * .3f * (.75f + .25f * Mathf.Sin(t * Mathf.PI * .25f));
            }
            return Clip("Neon drift", samples, rate);
        }
        private AudioClip Clip(string name, float[] samples, int rate)
        {
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            generated.Add(clip);
            return clip;
        }
        private void OnDestroy()
        {
            foreach (var clip in generated) Destroy(clip);
            if (Instance == this) Instance = null;
        }
    }
}
