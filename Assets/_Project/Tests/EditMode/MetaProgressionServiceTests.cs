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
    }
}
