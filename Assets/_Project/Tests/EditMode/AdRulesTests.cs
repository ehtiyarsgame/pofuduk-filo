using System;
using NUnit.Framework;
using PofudukFilo.Meta;

namespace PofudukFilo.Tests
{
    public sealed class AdRulesTests
    {
        private static readonly long Noon = new DateTime(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc).Ticks;

        [Test]
        public void test_ad_rules_gift_ready_when_never_claimed()
        {
            Assert.That(AdRules.GiftWait(0, Noon), Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void test_ad_rules_gift_waits_out_the_cooldown()
        {
            long claimed = Noon;
            Assert.That(AdRules.GiftWait(claimed, claimed + TimeSpan.FromMinutes(5).Ticks),
                Is.EqualTo(AdRules.GiftCooldown - TimeSpan.FromMinutes(5)));
            Assert.That(AdRules.GiftWait(claimed, claimed + AdRules.GiftCooldown.Ticks), Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void test_ad_rules_clock_set_back_does_not_lock_the_gift()
        {
            Assert.That(AdRules.GiftWait(Noon, Noon - TimeSpan.FromHours(3).Ticks), Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void test_ad_rules_day_changes_at_utc_midnight()
        {
            long lateNight = new DateTime(2026, 9, 24, 23, 59, 0, DateTimeKind.Utc).Ticks;
            long nextDay = new DateTime(2026, 9, 25, 0, 1, 0, DateTimeKind.Utc).Ticks;
            Assert.That(AdRules.DayOf(Noon), Is.EqualTo(AdRules.DayOf(lateNight)));
            Assert.That(AdRules.DayOf(nextDay), Is.EqualTo(AdRules.DayOf(Noon) + 1));
        }
    }
}
