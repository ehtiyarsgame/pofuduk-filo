using System;
using NUnit.Framework;
using PofudukFilo.Audio;

namespace PofudukFilo.Tests
{
    public sealed class SfxSynthTests
    {
        [Test]
        public void EverySfx_IsShort_NonSilent_AndNeverClips()
        {
            foreach (SfxId id in (SfxId[])Enum.GetValues(typeof(SfxId)))
            {
                float[] samples = SfxSynth.Build(id);
                float peak = 0f;
                foreach (float s in samples) peak = Math.Max(peak, Math.Abs(s));

                Assert.That(samples.Length, Is.GreaterThan(0).And.LessThan(SfxSynth.SampleRate * 2), id.ToString());
                Assert.That(peak, Is.GreaterThan(0.01f), $"{id} is silent");
                Assert.That(peak, Is.LessThanOrEqualTo(0.9f + 1e-4f), $"{id} clips");
            }
        }

        [Test]
        public void MusicLoop_IsQuietAndEndsAtZero()
        {
            float[] loop = SfxSynth.BuildMusicLoop();
            float peak = 0f;
            foreach (float s in loop) peak = Math.Max(peak, Math.Abs(s));

            Assert.That(peak, Is.LessThanOrEqualTo(0.6f + 1e-4f));
            Assert.That(Math.Abs(loop[loop.Length - 1]), Is.LessThan(1e-3f), "tail must fade to avoid a click at the loop point");
        }
    }
}
