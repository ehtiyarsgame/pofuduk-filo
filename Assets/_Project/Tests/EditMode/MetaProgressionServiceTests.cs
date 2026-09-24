using NUnit.Framework;
using PofudukFilo.Meta;
using PofudukFilo.Weapons;
using UnityEngine;

namespace PofudukFilo.Tests
{
    public sealed class MetaProgressionServiceTests
    {
        private MetaUpgradeDefinition _damage;

        [SetUp]
        public void SetUp()
        {
            _damage = ScriptableObject.CreateInstance<MetaUpgradeDefinition>();
            _damage.id = "damage";
            _damage.stat = StatType.Damage;
            _damage.effectPerLevel = 0.05f;
            _damage.maxLevel = 10;
            _damage.baseCost = 100;
            _damage.costGrowth = 1.35f;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_damage);

        [Test]
        public void FirstRunGold_BuysAtLeastTwoUpgrades()
        {
            // meta-economy.md §3.3: a ~250 gold first run affords Health Lv1 (80) + Damage Lv1 (100).
            var service = new MetaProgressionService(new SaveData(), _ => { });
            service.GrantRunRewards(250, 0);

            Assert.That(service.TryPurchase(_damage), Is.True);
            Assert.That(service.Gold, Is.EqualTo(150));
            Assert.That(service.GetLevel(_damage), Is.EqualTo(1));
        }

        [Test]
        public void TryPurchase_FailsWhenBrokeOrMaxed()
        {
            var data = new SaveData { gold = 50 };
            var service = new MetaProgressionService(data, _ => { });
            Assert.That(service.TryPurchase(_damage), Is.False);

            data.gold = 1_000_000;
            for (int i = 0; i < 10; i++) Assert.That(service.TryPurchase(_damage), Is.True);
            Assert.That(service.TryPurchase(_damage), Is.False);
            Assert.That(service.IsMaxed(_damage), Is.True);
        }

        [Test]
        public void ApplyTo_WritesBonusIntoPlayerStats()
        {
            var service = new MetaProgressionService(new SaveData { gold = 10_000 }, _ => { });
            service.TryPurchase(_damage);
            service.TryPurchase(_damage);

            var stats = new PlayerStats();
            service.ApplyTo(stats, new[] { _damage });

            Assert.That(stats.DamageMultiplier, Is.EqualTo(1.10f).Within(1e-5f));
        }

        // ---------------------------------------------------------------- Rewarded ads (ad-rewards.md)

        private static readonly long Noon = new System.DateTime(2026, 9, 24, 12, 0, 0, System.DateTimeKind.Utc).Ticks;

        private static CharacterDefinition AdPilot(int ads)
        {
            var c = ScriptableObject.CreateInstance<CharacterDefinition>();
            c.id = "pengu";
            c.goldCost = 4000;
            c.adsToUnlock = ads;
            return c;
        }

        [Test]
        public void test_ad_unlock_completes_after_the_required_views()
        {
            CharacterDefinition pilot = AdPilot(3);
            var service = new MetaProgressionService(new SaveData(), _ => { });

            Assert.That(service.RecordAdView(pilot, Noon), Is.False);
            Assert.That(service.RecordAdView(pilot, Noon), Is.False);
            Assert.That(service.IsUnlocked(pilot), Is.False);
            Assert.That(service.RecordAdView(pilot, Noon), Is.True);
            Assert.That(service.IsUnlocked(pilot), Is.True);
            Assert.That(service.CanWatchForUnlock(pilot, Noon), Is.False);
            Object.DestroyImmediate(pilot);
        }

        [Test]
        public void test_ad_views_capped_per_day_and_reset_next_day()
        {
            CharacterDefinition pilot = AdPilot(99);
            var service = new MetaProgressionService(new SaveData(), _ => { });
            for (int i = 0; i < AdRules.UnlockViewsPerDay; i++) service.RecordAdView(pilot, Noon);

            Assert.That(service.AdViewsLeftToday(Noon), Is.EqualTo(0));
            Assert.That(service.CanWatchForUnlock(pilot, Noon), Is.False);
            long tomorrow = Noon + System.TimeSpan.TicksPerDay;
            Assert.That(service.AdViewsLeftToday(tomorrow), Is.EqualTo(AdRules.UnlockViewsPerDay));
            Assert.That(service.AdProgress(pilot.id), Is.EqualTo(AdRules.UnlockViewsPerDay));
            Object.DestroyImmediate(pilot);
        }

        [Test]
        public void test_gift_pays_then_waits_for_cooldown_and_daily_limit()
        {
            var service = new MetaProgressionService(new SaveData(), _ => { });
            long t = Noon;

            Assert.That(service.ClaimGift(t), Is.EqualTo(100));
            Assert.That(service.Stardust, Is.EqualTo(AdRules.GiftStardust));
            Assert.That(service.ClaimGift(t + 1), Is.EqualTo(0));
            for (int i = 1; i < AdRules.GiftsPerDay; i++)
            {
                t += AdRules.GiftCooldown.Ticks;
                Assert.That(service.ClaimGift(t), Is.GreaterThan(0));
            }
            Assert.That(service.ClaimGift(t + AdRules.GiftCooldown.Ticks), Is.EqualTo(0));
            Assert.That(service.GiftsLeftToday(t), Is.EqualTo(0));
        }
    }
}
