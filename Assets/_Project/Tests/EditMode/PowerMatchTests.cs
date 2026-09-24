using NUnit.Framework;
using PofudukFilo.Enemies;

namespace PofudukFilo.Tests
{
    public sealed class PowerMatchTests
    {
        private static PowerMatch Calibrated(float baselineTtk)
        {
            var pm = new PowerMatch { CalibrationSeconds = 10f };
            for (int i = 0; i < 50; i++) pm.RecordKill(baselineTtk);
            pm.Tick(10f); // calibration ends; baseline = average
            return pm;
        }

        [Test]
        public void test_power_match_holds_scale_during_calibration()
        {
            var pm = new PowerMatch { CalibrationSeconds = 10f };
            pm.RecordKill(0.2f);
            pm.Tick(5f);
            Assert.That(pm.Scale, Is.EqualTo(1f));
            Assert.That(pm.BaselineTtk, Is.LessThan(0f));
        }

        [Test]
        public void test_power_match_scale_rises_when_kills_get_faster()
        {
            PowerMatch pm = Calibrated(2f);
            for (int i = 0; i < 200; i++) pm.RecordKill(0.4f); // late run: kills 5x faster than the baseline
            for (int i = 0; i < 30; i++) pm.Tick(1f);
            Assert.That(pm.Scale, Is.GreaterThan(2f));
            Assert.That(pm.Scale, Is.LessThanOrEqualTo(pm.MaxScale));
        }

        [Test]
        public void test_power_match_scale_stays_at_one_when_player_is_slower()
        {
            PowerMatch pm = Calibrated(1f);
            for (int i = 0; i < 200; i++) pm.RecordKill(3f);
            for (int i = 0; i < 30; i++) pm.Tick(1f);
            Assert.That(pm.Scale, Is.EqualTo(1f));
        }

        [Test]
        public void test_power_match_reset_forgets_calibration()
        {
            PowerMatch pm = Calibrated(1f);
            for (int i = 0; i < 200; i++) pm.RecordKill(0.2f);
            for (int i = 0; i < 30; i++) pm.Tick(1f);
            pm.Reset();
            Assert.That(pm.Scale, Is.EqualTo(1f));
            Assert.That(pm.AverageTtk, Is.LessThan(0f));
        }
    }
}
