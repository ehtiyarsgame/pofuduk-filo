using System;

namespace PofudukFilo.Core
{
    /// <summary>
    /// Pure balance formulas from the GDDs. No UnityEngine dependency so they can be
    /// unit-tested in EditMode and tuned without touching gameplay code.
    /// See design/gdd/game-concept.md §5 and design/gdd/meta-economy.md §4.
    /// </summary>
    public static class Formulas
    {
        // XP(n) = floor(5 + 6n + 0.9 n^1.7)
        public const float XpBase = 5f;
        public const float XpLinear = 6f;
        public const float XpCoefficient = 0.9f;
        public const float XpExponent = 1.7f;

        /// <summary>XP required to go from <paramref name="level"/> to level + 1 (level starts at 1).</summary>
        public static int XpToNextLevel(int level)
        {
            if (level < 1) throw new ArgumentOutOfRangeException(nameof(level));
            return (int)Math.Floor(XpBase + XpLinear * level + XpCoefficient * Math.Pow(level, XpExponent));
        }

        /// <summary>HP(t, c) = HP_base · (1 + 0.18t + 0.012t²) · 1.22^c</summary>
        public static float EnemyHp(float baseHp, float minutes, int chapterIndex)
        {
            float timeScale = 1f + 0.18f * minutes + 0.012f * minutes * minutes;
            return baseHp * timeScale * (float)Math.Pow(1.22, chapterIndex);
        }

        /// <summary>Threat points per second the spawner may spend. Budget(t) = 1.2 + 0.75t + 0.08t², scaled by DDA (lowered from 2 + 0.9t after QA run 12: 43 enemies alive at 10 s).</summary>
        public static float SpawnBudget(float minutes, float ddaMultiplier)
        {
            float clamped = Math.Clamp(ddaMultiplier, 0.75f, 1.15f);
            return (1.2f + 0.75f * minutes + 0.08f * minutes * minutes) * clamped;
        }

        /// <summary>Cost(L) = round10(base · growth^(L−1)) for a meta upgrade going to level L (L ≥ 1).</summary>
        public static int MetaUpgradeCost(int baseCost, float growth, int targetLevel)
        {
            if (targetLevel < 1) throw new ArgumentOutOfRangeException(nameof(targetLevel));
            double raw = baseCost * Math.Pow(growth, targetLevel - 1);
            return (int)(Math.Round(raw / 10.0, MidpointRounding.AwayFromZero) * 10);
        }

        /// <summary>Gold per cleared run for chapter c: ≈ 250 · 1.35^c.</summary>
        public static int ExpectedRunGold(int chapterIndex) =>
            (int)Math.Round(250 * Math.Pow(1.35, chapterIndex));

        /// <summary>
        /// Final cooldown = base · Π(1 − r_i), floored at 35 % of base (weapon-system.md §4).
        /// </summary>
        public static float FinalCooldown(float baseCooldown, ReadOnlySpan<float> reductions)
        {
            float multiplier = 1f;
            for (int i = 0; i < reductions.Length; i++)
                multiplier *= 1f - reductions[i];
            return baseCooldown * Math.Max(multiplier, 0.35f);
        }
    }
}
