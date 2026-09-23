using NUnit.Framework;
using PofudukFilo.Enemies;

namespace PofudukFilo.Tests
{
    public sealed class DifficultyDirectorTests
    {
        [Test]
        public void TwoHitsInAMinute_EaseByTenPercent()
        {
            var dda = new DifficultyDirector();
            dda.Reset(0f);
            dda.RegisterPlayerHit(10f);
            dda.RegisterPlayerHit(30f);

            dda.Evaluate(31f);

            Assert.That(dda.Multiplier, Is.EqualTo(0.9f).Within(1e-5f));
        }

        [Test]
        public void Easing_IsRateLimited_AndFloorsAtMinus25Percent()
        {
            var dda = new DifficultyDirector();
            dda.Reset(0f);

            for (float t = 0f; t < 600f; t += 5f)
            {
                dda.RegisterPlayerHit(t);
                dda.Evaluate(t);
            }

            Assert.That(dda.Multiplier, Is.EqualTo(DifficultyDirector.Min).Within(1e-5f));

            var fresh = new DifficultyDirector();
            fresh.Reset(0f);
            fresh.RegisterPlayerHit(1f);
            fresh.RegisterPlayerHit(2f);
            fresh.Evaluate(3f);
            fresh.Evaluate(4f); // inside cooldown — no second step
            Assert.That(fresh.Multiplier, Is.EqualTo(0.9f).Within(1e-5f));
        }

        [Test]
        public void CleanPlay_HardensUpToPlus15Percent()
        {
            var dda = new DifficultyDirector();
            dda.Reset(0f);

            dda.Evaluate(89f);
            Assert.That(dda.Multiplier, Is.EqualTo(1f));

            for (float t = 90f; t < 1000f; t += 1f) dda.Evaluate(t);
            Assert.That(dda.Multiplier, Is.EqualTo(DifficultyDirector.Max).Within(1e-5f));
        }

        [Test]
        public void OldHits_FallOutOfTheWindow()
        {
            var dda = new DifficultyDirector();
            dda.Reset(0f);
            dda.RegisterPlayerHit(0f);
            dda.RegisterPlayerHit(1f);

            dda.Evaluate(62f); // both hits older than 60 s, and not yet 90 s clean

            Assert.That(dda.Multiplier, Is.EqualTo(1f));
        }
    }
}
