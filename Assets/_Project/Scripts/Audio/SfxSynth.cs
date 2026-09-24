using System;

namespace PofudukFilo.Audio
{
    public enum Wave
    {
        Sine,
        Triangle,
        SoftSquare,
        Noise
    }

    public enum SfxId
    {
        Pop,
        BigPop,
        Gem,
        Coin,
        LevelUp,
        Evolution,
        Hurt,
        Graze,
        BossWarning,
        Click,
        Heal
    }

    /// <summary>
    /// Pure procedural synth for the "cute" sound set (art-bible §5.1: every death is a soft
    /// "pop"). No engine dependency — samples are plain floats, unit-tested for length and range.
    /// </summary>
    public static class SfxSynth
    {
        public const int SampleRate = 44100;

        /// <summary>One note with a frequency glide and an attack/exponential-decay envelope.</summary>
        public static void AddTone(float[] buffer, float startSeconds, float duration, float freqStart, float freqEnd,
            Wave wave, float volume, float attack = 0.005f, int seed = 1)
        {
            int start = (int)(startSeconds * SampleRate);
            int length = (int)(duration * SampleRate);
            double phase = 0;
            var rng = new Random(seed);

            for (int i = 0; i < length && start + i < buffer.Length; i++)
            {
                float t = i / (float)SampleRate;
                float k = length > 1 ? i / (float)(length - 1) : 1f;
                float freq = freqStart + (freqEnd - freqStart) * k;
                phase += freq / SampleRate;

                float env = t < attack ? t / attack : (float)Math.Exp(-5.0 * (t - attack) / Math.Max(1e-3, duration));
                buffer[start + i] += Sample(wave, phase, rng) * env * volume;
            }
        }

        private static float Sample(Wave wave, double phase, Random rng)
        {
            double p = phase - Math.Floor(phase);
            switch (wave)
            {
                case Wave.Sine: return (float)Math.Sin(p * Math.PI * 2);
                case Wave.Triangle: return (float)(4 * Math.Abs(p - 0.5) - 1);
                case Wave.SoftSquare: return (float)Math.Tanh(Math.Sin(p * Math.PI * 2) * 3) * 0.7f;
                default: return (float)(rng.NextDouble() * 2 - 1);
            }
        }

        public static float[] Build(SfxId id)
        {
            switch (id)
            {
                case SfxId.Pop:
                {
                    float[] b = New(0.12f);
                    AddTone(b, 0, 0.1f, 900, 300, Wave.Sine, 0.5f);
                    AddTone(b, 0, 0.03f, 0, 0, Wave.Noise, 0.12f);
                    return Finish(b);
                }
                case SfxId.BigPop:
                {
                    float[] b = New(0.45f);
                    AddTone(b, 0, 0.35f, 500, 90, Wave.Sine, 0.6f);
                    AddTone(b, 0, 0.12f, 0, 0, Wave.Noise, 0.2f);
                    AddTone(b, 0.05f, 0.3f, 1320, 1760, Wave.Triangle, 0.15f);
                    return Finish(b);
                }
                case SfxId.Gem:
                {
                    float[] b = New(0.12f);
                    AddTone(b, 0, 0.1f, 1760, 2093, Wave.Sine, 0.25f);
                    return Finish(b);
                }
                case SfxId.Coin:
                {
                    float[] b = New(0.22f);
                    AddTone(b, 0, 0.08f, 988, 988, Wave.SoftSquare, 0.22f);
                    AddTone(b, 0.07f, 0.14f, 1319, 1319, Wave.SoftSquare, 0.22f);
                    return Finish(b);
                }
                case SfxId.LevelUp:
                {
                    float[] b = New(0.6f);
                    float[] notes = { 523, 659, 784, 1047 }; // C major arpeggio
                    for (int i = 0; i < notes.Length; i++) AddTone(b, i * 0.08f, 0.3f, notes[i], notes[i], Wave.Triangle, 0.3f);
                    return Finish(b);
                }
                case SfxId.Evolution:
                {
                    float[] b = New(1.1f);
                    AddTone(b, 0, 0.9f, 300, 1200, Wave.Triangle, 0.3f, 0.05f);
                    float[] sparkle = { 1568, 1760, 2093, 2349, 2637 };
                    for (int i = 0; i < sparkle.Length; i++) AddTone(b, 0.3f + i * 0.1f, 0.35f, sparkle[i], sparkle[i], Wave.Sine, 0.18f);
                    return Finish(b);
                }
                case SfxId.Hurt:
                {
                    float[] b = New(0.25f);
                    AddTone(b, 0, 0.2f, 220, 110, Wave.SoftSquare, 0.4f);
                    return Finish(b);
                }
                case SfxId.Graze:
                {
                    float[] b = New(0.1f);
                    AddTone(b, 0, 0.08f, 0, 0, Wave.Noise, 0.08f, 0.02f, 7);
                    AddTone(b, 0, 0.08f, 2400, 3000, Wave.Sine, 0.08f);
                    return Finish(b);
                }
                case SfxId.BossWarning:
                {
                    float[] b = New(0.9f);
                    AddTone(b, 0, 0.35f, 392, 392, Wave.SoftSquare, 0.3f);
                    AddTone(b, 0.4f, 0.45f, 311, 311, Wave.SoftSquare, 0.3f);
                    return Finish(b);
                }
                case SfxId.Heal:
                {
                    float[] b = New(0.4f);
                    AddTone(b, 0, 0.3f, 660, 990, Wave.Sine, 0.25f, 0.02f);
                    return Finish(b);
                }
                default: // Click
                {
                    float[] b = New(0.06f);
                    AddTone(b, 0, 0.05f, 1200, 800, Wave.Sine, 0.25f);
                    return Finish(b);
                }
            }
        }

        /// <summary>
        /// A gentle 8-bar loop: pentatonic triangle arpeggio over a soft bass, ~110 BPM.
        /// Quiet by design — the music should never compete with the "pop"s.
        /// </summary>
        public static float[] BuildMusicLoop()
        {
            const float beat = 60f / 110f;
            const float eighth = beat / 2f;
            float[] b = New(beat * 4 * 8);

            // I – vi – IV – V in C, each held two bars.
            float[][] chords =
            {
                new[] { 261.6f, 329.6f, 392.0f, 523.3f },
                new[] { 220.0f, 261.6f, 329.6f, 440.0f },
                new[] { 174.6f, 220.0f, 261.6f, 349.2f },
                new[] { 196.0f, 246.9f, 293.7f, 392.0f }
            };
            int[] pattern = { 0, 1, 2, 3, 2, 1, 2, 3 };

            for (int bar = 0; bar < 8; bar++)
            {
                float[] chord = chords[bar / 2];
                float barStart = bar * beat * 4;
                AddTone(b, barStart, beat * 3.5f, chord[0] / 2f, chord[0] / 2f, Wave.Sine, 0.18f, 0.02f);
                for (int i = 0; i < 8; i++)
                {
                    float f = chord[pattern[i]] * 2f;
                    AddTone(b, barStart + i * eighth, eighth * 1.6f, f, f, Wave.Triangle, 0.07f, 0.01f);
                }
            }
            return Finish(b, 0.6f);
        }

        private static float[] New(float seconds) => new float[(int)(seconds * SampleRate)];

        /// <summary>Normalises to <paramref name="peak"/> and fades the tail to avoid clicks.</summary>
        private static float[] Finish(float[] b, float peak = 0.9f)
        {
            float max = 0f;
            for (int i = 0; i < b.Length; i++) max = Math.Max(max, Math.Abs(b[i]));
            float gain = max > peak ? peak / max : 1f;

            int fade = Math.Min(b.Length, SampleRate / 200); // 5 ms
            for (int i = 0; i < b.Length; i++)
            {
                float tail = i >= b.Length - fade ? (b.Length - i) / (float)fade : 1f;
                b[i] *= gain * tail;
            }
            return b;
        }
    }
}
