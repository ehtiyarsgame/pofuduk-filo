using NUnit.Framework;
using PofudukFilo.Core;

namespace PofudukFilo.Tests
{
    public sealed class FormulasTests
    {
        // Expected values are the table in design/gdd/game-concept.md §5.
        [TestCase(1, 11)]
        [TestCase(2, 19)]
        [TestCase(3, 28)]
        [TestCase(5, 48)]
        [TestCase(10, 110)]
        [TestCase(25, 369)]
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
            Assert.That(Formulas.SpawnBudget(0f, 10f), Is.EqualTo(1.5f * 1.15f).Within(1e-5f));
            Assert.That(Formulas.SpawnBudget(0f, 0f), Is.EqualTo(1.5f * 0.75f).Within(1e-5f));
        }
    }
}
