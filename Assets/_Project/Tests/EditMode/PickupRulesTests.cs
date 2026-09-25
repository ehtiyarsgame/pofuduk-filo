using NUnit.Framework;
using PofudukFilo.Progression;

namespace PofudukFilo.Tests
{
    public sealed class PickupRulesTests
    {
        [TestCase(1, PickupKind.XpSmall)]
        [TestCase(4, PickupKind.XpSmall)]
        [TestCase(5, PickupKind.XpMedium)]
        [TestCase(24, PickupKind.XpMedium)]
        [TestCase(25, PickupKind.XpLarge)]
        [TestCase(400, PickupKind.XpLarge)]
        public void XpKindFor_MapsValueToGemColour(int value, PickupKind expected)
        {
            Assert.That(PickupRules.XpKindFor(value), Is.EqualTo(expected));
        }

        [Test]
        public void IsXp_OnlyForGems()
        {
            Assert.That(PickupRules.IsXp(PickupKind.XpLarge), Is.True);
            Assert.That(PickupRules.IsXp(PickupKind.Gold), Is.False);
            Assert.That(PickupRules.IsXp(PickupKind.Bomb), Is.False);
        }

        [TestCase(1, PickupKind.Gold)]
        [TestCase(14, PickupKind.Gold)]
        [TestCase(15, PickupKind.GoldBig)]
        [TestCase(59, PickupKind.GoldBig)]
        [TestCase(60, PickupKind.GoldBar)]
        public void test_gold_kind_by_value(int value, PickupKind expected)
        {
            Assert.That(PickupRules.GoldKindFor(value), Is.EqualTo(expected));
            Assert.That(PickupRules.IsGold(expected), Is.True);
        }
    }
}
