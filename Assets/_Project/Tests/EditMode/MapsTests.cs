using NUnit.Framework;
using PofudukFilo.Meta;

namespace PofudukFilo.Tests
{
    public sealed class MapsTests
    {
        [TearDown]
        public void ResetMap() => Maps.Select(0);

        [Test]
        public void test_maps_first_is_open_and_next_needs_fifteen_minutes()
        {
            // Arrange: 14:59 on map 1, nothing on map 2.
            float[] best = { 899f, 0f, 0f };

            // Act / Assert
            Assert.That(Maps.IsUnlocked(0, i => best[i]), Is.True);
            Assert.That(Maps.IsUnlocked(1, i => best[i]), Is.False);

            best[0] = 900f;
            Assert.That(Maps.IsUnlocked(1, i => best[i]), Is.True);
            Assert.That(Maps.IsUnlocked(2, i => best[i]), Is.False);
        }

        [Test]
        public void test_maps_get_harder_and_pay_more()
        {
            for (int i = 1; i < Maps.Count; i++)
            {
                Assert.That(Maps.All[i].HpMultiplier, Is.GreaterThan(Maps.All[i - 1].HpMultiplier));
                Assert.That(Maps.All[i].GoldMultiplier, Is.GreaterThan(Maps.All[i - 1].GoldMultiplier));
                Assert.That(Maps.All[i].ThreatOffsetMinutes, Is.GreaterThan(Maps.All[i - 1].ThreatOffsetMinutes));
            }
        }

        [Test]
        public void test_maps_threat_clock_adds_the_selected_offset()
        {
            Maps.Select(2);
            Assert.That(Maps.ThreatMinutes(1f), Is.EqualTo(1f + Maps.All[2].ThreatOffsetMinutes).Within(1e-5f));
            Maps.Select(99);
            Assert.That(Maps.CurrentIndex, Is.EqualTo(Maps.Count - 1));
        }
    }
}
