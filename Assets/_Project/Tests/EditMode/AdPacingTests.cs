using NUnit.Framework;
using PofudukFilo.Core;

namespace PofudukFilo.Tests
{
    public sealed class AdPacingTests
    {
        [Test]
        public void test_first_runs_are_ad_free()
        {
            Assert.That(AdPacing.ShouldShowInterstitial(1, 9999f, 300f, false), Is.False);
            Assert.That(AdPacing.ShouldShowInterstitial(2, 9999f, 300f, false), Is.False);
        }

        [Test]
        public void test_every_second_run_after_grace()
        {
            Assert.That(AdPacing.ShouldShowInterstitial(3, 9999f, 300f, false), Is.False);
            Assert.That(AdPacing.ShouldShowInterstitial(4, 9999f, 300f, false), Is.True);
        }

        [Test]
        public void test_cooldown_between_ads()
        {
            Assert.That(AdPacing.ShouldShowInterstitial(4, 60f, 300f, false), Is.False);
        }

        [Test]
        public void test_short_run_or_rewarded_watch_skips()
        {
            Assert.That(AdPacing.ShouldShowInterstitial(4, 9999f, 20f, false), Is.False);
            Assert.That(AdPacing.ShouldShowInterstitial(4, 9999f, 300f, true), Is.False);
        }
    }
}
