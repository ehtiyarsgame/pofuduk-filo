using System;
using NUnit.Framework;
using PofudukFilo.Core;

namespace PofudukFilo.Tests
{
    public sealed class FormulasTests
    {
        // Expected values are the table in design/gdd/game-concept.md §5.
        [TestCase(1, 19)]
        [TestCase(2, 30)]
        [TestCase(3, 42)]
        [TestCase(5, 68)]
        [TestCase(10, 145)]
        [TestCase(25, 449)]
        public void XpToNextLevel_MatchesGddTable(int level, int expected)
        {
            Assert.That(Formulas.XpToNextLevel(level), Is.EqualTo(expected));
        }

        [Test]
        public void XpCurve_IsStrictlyIncreasing()
        {
            for (int n = 1; n < 60; n++)
                Assert.That(Formulas.XpToNextLevel(n + 1), Is.GreaterThan(Formulas.XpToNextLevel(n)));
        }

        // Workshop "Damage" row in design/gdd/meta-economy.md §3.3: 100 → 1,490, total 5,470.
        [Test]
        public void MetaUpgradeCost_DamageRow_MatchesGdd()
        {
            int total = 0;
            for (int level = 1; level <= 10; level++) total += Formulas.MetaUpgradeCost(100, 1.35f, level);

            Assert.That(Formulas.MetaUpgradeCost(100, 1.35f, 1), Is.EqualTo(100));
            Assert.That(Formulas.MetaUpgradeCost(100, 1.35f, 10), Is.EqualTo(1490));
            Assert.That(total, Is.EqualTo(5470));
        }

        [Test]
        public void FinalCooldown_StacksMultiplicatively_WithFloor()
        {
            Assert.That(Formulas.FinalCooldown(1f, new[] { 0.1f, 0.1f }), Is.EqualTo(0.81f).Within(1e-5f));
            Assert.That(Formulas.FinalCooldown(1f, new[] { 0.5f, 0.5f, 0.5f }), Is.EqualTo(0.35f).Within(1e-5f));
        }

        [Test]
        public void SpawnBudget_ClampsDda()
        {
            Assert.That(Formulas.SpawnBudget(0f, 10f), Is.EqualTo(2f * 1.15f).Within(1e-5f));
            Assert.That(Formulas.SpawnBudget(0f, 0f), Is.EqualTo(2f * 0.75f).Within(1e-5f));
        }

        [Test]
        public void test_mastery_cost_grows_and_rounds()
        {
            Assert.That(Formulas.MasteryCost(0), Is.EqualTo(300));
            Assert.That(Formulas.MasteryCost(1), Is.EqualTo(480));
            Assert.That(Formulas.MasteryCost(9), Is.GreaterThan(7000));
        }

        [Test]
        public void test_pilot_level_cost_and_mastery_multiplier()
        {
            Assert.That(Formulas.PilotLevelCost(1), Is.EqualTo(600));
            Assert.That(Formulas.PilotLevelCost(2), Is.EqualTo(960));
            Assert.That(Formulas.MasteryMultiplier(0), Is.EqualTo(1f));
            Assert.That(Formulas.MasteryMultiplier(10), Is.EqualTo(1.4f).Within(1e-5f));
            Assert.That(Formulas.MasteryMultiplier(99), Is.EqualTo(1.4f).Within(1e-5f));
        }

        [Test]
        public void test_rush_meter_gain_caps_combo()
        {
            Assert.That(Formulas.RushMeterGain(1f, 20, 0.03f), Is.EqualTo(1.6f).Within(1e-5f));
            Assert.That(Formulas.RushMeterGain(1f, Formulas.RushComboCap, 0.03f), Is.EqualTo(2.2f).Within(1e-5f));
            Assert.That(Formulas.RushMeterGain(1f, 173, 0.03f), Is.EqualTo(2.2f).Within(1e-5f));
        }

        [Test]
        public void test_rush_meter_max_grows_with_time()
        {
            Assert.That(Formulas.RushMeterMax(120f, 0.5f, 0f), Is.EqualTo(120f).Within(1e-4f));
            Assert.That(Formulas.RushMeterMax(120f, 0.5f, 5f), Is.EqualTo(420f).Within(1e-4f));
            Assert.That(Formulas.RushMeterMax(120f, 0.5f, -1f), Is.EqualTo(120f).Within(1e-4f));
            Assert.That(Formulas.RushMeterMax(120f, 0.35f, 1.5f, 2f), Is.EqualTo(120f).Within(1e-4f));
            Assert.That(Formulas.RushMeterMax(120f, 0.35f, 5f, 2f), Is.EqualTo(246f).Within(1e-3f));
        }

        [Test]
        public void test_power_match_rises_when_killing_too_fast()
        {
            // Killing in half the target time: ln(2) error → scale grows by e^(0.1·0.693) per second.
            float next = Formulas.PowerMatchStep(1f, 0.5f, 1f, 0.1f, 1f, 6f);
            Assert.That(next, Is.EqualTo(MathF.Exp(0.1f * MathF.Log(2f))).Within(1e-5f));
        }

        [Test]
        public void test_power_match_never_below_one_or_above_max()
        {
            Assert.That(Formulas.PowerMatchStep(1f, 3f, 1f, 0.5f, 1f, 6f), Is.EqualTo(1f));
            Assert.That(Formulas.PowerMatchStep(5.9f, 0.01f, 1f, 1f, 5f, 6f), Is.EqualTo(6f));
            Assert.That(Formulas.PowerMatchStep(2f, 0f, 1f, 1f, 1f, 6f), Is.EqualTo(2f));
        }

        [Test]
        public void test_forge_cost_and_multipliers()
        {
            Assert.That(Formulas.ForgeCost(0), Is.EqualTo(120));
            Assert.That(Formulas.ForgeCost(1), Is.EqualTo(140));
            Assert.That(Formulas.ForgeCost(10), Is.EqualTo(530));
            Assert.That(Formulas.ForgeCost(1000), Is.GreaterThan(0));
            Assert.That(Formulas.ForgePowerMultiplier(20), Is.EqualTo(1.4f).Within(1e-5f));
            Assert.That(Formulas.ForgeSpeedMultiplier(40), Is.EqualTo(1.4f).Within(1e-5f));
            Assert.That(Formulas.ForgeSpeedMultiplier(99), Is.EqualTo(1.4f).Within(1e-5f));
        }

        [Test]
        public void test_fit_camera_tall_phone_keeps_design_width()
        {
            (float size, float vw) = Formulas.FitCamera(0.42f, 10.8f);
            Assert.That(2f * size * 0.42f, Is.EqualTo(2f * 10.8f * Formulas.DesignAspect).Within(1e-3f));
            Assert.That(vw, Is.EqualTo(1f));
        }

        [Test]
        public void test_fit_camera_tablet_is_pillarboxed()
        {
            (float size, float vw) = Formulas.FitCamera(0.75f, 10.8f);
            Assert.That(size, Is.EqualTo(10.8f).Within(1e-4f));
            Assert.That(vw, Is.EqualTo(Formulas.MaxAspect / 0.75f).Within(1e-4f));
        }

        [Test]
        public void test_fit_camera_reference_phone_unchanged()
        {
            (float size, float vw) = Formulas.FitCamera(Formulas.DesignAspect, 10.8f);
            Assert.That(size, Is.EqualTo(10.8f).Within(1e-4f));
            Assert.That(vw, Is.EqualTo(1f));
        }

        [Test]
        public void test_gift_gold_tracks_forge_depth_with_a_floor()
        {
            Assert.That(Formulas.GiftGold(0, 0), Is.EqualTo(120));
            Assert.That(Formulas.GiftGold(10, 10), Is.EqualTo(Formulas.ForgeCost(10)));
            Assert.That(Formulas.GiftGold(30, 10), Is.EqualTo(Formulas.ForgeCost(20)));
        }

        [Test]
        public void test_enemy_damage_scale_grows_with_run_time()
        {
            Assert.That(Formulas.EnemyDamageScale(0f), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(Formulas.EnemyDamageScale(5f), Is.EqualTo(3.25f).Within(1e-4f));
            Assert.That(Formulas.EnemyDamageScale(-1f), Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void test_leak_damage_scales_with_size_and_hp_left()
        {
            Assert.That(Formulas.LeakDamageFraction(10f, 1f, false), Is.EqualTo(0.03f).Within(1e-5f));
            Assert.That(Formulas.LeakDamageFraction(45f, 1f, false), Is.EqualTo(0.065f).Within(1e-5f));
            Assert.That(Formulas.LeakDamageFraction(500f, 1f, false), Is.EqualTo(0.10f).Within(1e-5f));
            Assert.That(Formulas.LeakDamageFraction(500f, 1f, true), Is.EqualTo(0.20f).Within(1e-5f));
            // A nearly dead leaker still costs a fifth of a full one.
            Assert.That(Formulas.LeakDamageFraction(10f, 0.01f, false), Is.EqualTo(0.006f).Within(1e-5f));
        }

        [Test]
        public void test_enemy_fire_rate_scale_is_capped()
        {
            Assert.That(Formulas.EnemyFireRateScale(0f), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(Formulas.EnemyFireRateScale(5f), Is.EqualTo(1.5f).Within(1e-5f));
            Assert.That(Formulas.EnemyFireRateScale(60f), Is.EqualTo(2.2f).Within(1e-5f));
        }

        [Test]
        public void test_power_rating_starts_at_one_and_grows_with_upgrades()
        {
            Assert.That(Formulas.PowerRating(0, 0, 0, 0, 1), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(Formulas.PowerRating(10, 10, 10, 0, 1), Is.EqualTo(1.4f).Within(1e-4f));
            Assert.That(Formulas.PowerRating(0, 0, 0, 5, 3), Is.EqualTo(1.16f).Within(1e-4f));
        }

        [Test]
        public void test_coin_value_scales_with_power_and_run_time()
        {
            Assert.That(Formulas.CoinValue(5f, 1f, 0f), Is.EqualTo(5f).Within(1e-4f));
            Assert.That(Formulas.CoinValue(5f, 2f, 5f), Is.EqualTo(8.4853f).Within(1e-4f));
            Assert.That(Formulas.CoinValue(5f, 0.5f, 0f), Is.EqualTo(5f).Within(1e-4f)); // never below base
        }
    }
}
