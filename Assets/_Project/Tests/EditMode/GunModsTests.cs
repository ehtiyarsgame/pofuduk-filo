using System.Collections.Generic;
using NUnit.Framework;
using PofudukFilo.Meta;

namespace PofudukFilo.Tests
{
    public sealed class GunModsTests
    {
        [Test]
        public void test_gun_mods_cost_grows_per_level()
        {
            Assert.That(GunMods.Cost(0), Is.EqualTo(250));
            Assert.That(GunMods.Cost(1), Is.EqualTo(400));
            Assert.That(GunMods.Cost(4), Is.EqualTo(1640));
        }

        [Test]
        public void test_gun_mods_every_hero_gun_has_three_tracks()
        {
            foreach (string gun in new[] { "feather_blaster", "chick_cannon", "spark_pistol", "ice_gun", "bubble_rifle", "star_bow", "yarn_launcher" })
                Assert.That(GunMods.For(gun).Count, Is.EqualTo(3), gun);
            Assert.That(GunMods.For("egg_mortar").Count, Is.EqualTo(0));
        }

        [Test]
        public void test_gun_mods_count_tracks_step_at_whole_levels()
        {
            // Arrange: Mırnav's chain track at level 3, range at 5, shock at 0.
            var levels = new Dictionary<string, int>
            {
                [GunMods.SaveKey("spark_pistol", "zincir")] = 3,
                [GunMods.SaveKey("spark_pistol", "menzil")] = 5
            };

            // Act
            GunMods.Bonus b = GunMods.Evaluate("spark_pistol", k => levels.TryGetValue(k, out int l) ? l : 0);

            // Assert: 0.5 × 3 = 1.5 → 1 extra jump; range +40 %; no stun bonus.
            Assert.That(b.Count, Is.EqualTo(1));
            Assert.That(b.Area, Is.EqualTo(0.4f).Within(1e-5f));
            Assert.That(b.Duration, Is.EqualTo(0f));
        }

        [Test]
        public void test_gun_mods_levels_above_max_are_clamped()
        {
            GunMods.Bonus b = GunMods.Evaluate("feather_blaster", _ => 99);
            Assert.That(b.Count, Is.EqualTo(2)); // 0.4 × 5
            Assert.That(b.FireRate, Is.EqualTo(0.2f).Within(1e-5f));
            Assert.That(b.Damage, Is.EqualTo(0.25f).Within(1e-5f));
        }
    }
}
