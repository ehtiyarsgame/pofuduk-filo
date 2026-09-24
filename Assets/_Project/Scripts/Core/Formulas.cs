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

        /// <summary>HP(t, c) = HP_base · (1 + 0.25t + 0.035t²) · 1.22^c</summary>
        public static float EnemyHp(float baseHp, float minutes, int chapterIndex)
        {
            float timeScale = 1f + 0.25f * minutes + 0.035f * minutes * minutes; // device feedback: late game melted (was 0.18m + 0.012m²)
            return baseHp * timeScale * (float)Math.Pow(1.22, chapterIndex);
        }

        /// <summary>Threat points per second the spawner may spend. Budget(t) = 1.5 + 0.9t + 0.08t², scaled by DDA (QA run 12 at 2 + 0.9t: dead at 12 s; run 13 at 1.2 + 0.75t: never below 70 % HP).</summary>
        public static float SpawnBudget(float minutes, float ddaMultiplier)
        {
            float clamped = Math.Clamp(ddaMultiplier, 0.75f, 1.15f);
            return (1.5f + 0.9f * minutes + 0.08f * minutes * minutes) * clamped;
        }

        /// <summary>Cost(L) = round10(base · growth^(L−1)) for a meta upgrade going to level L (L ≥ 1).</summary>
        public static int MetaUpgradeCost(int baseCost, float growth, int targetLevel)
        {
            if (targetLevel < 1) throw new ArgumentOutOfRangeException(nameof(targetLevel));
            double raw = baseCost * Math.Pow(growth, targetLevel - 1);
            return (int)(Math.Round(raw / 10.0, MidpointRounding.AwayFromZero) * 10);
        }

        /// <summary>Gold per cleared run for chapter c: ≈ 250 · 1.35^c.</summary>
        // ---- Permanent progression (meta-economy.md §3.6)

        public const int MaxWeaponMastery = 10;
        public const float MasteryDamagePerLevel = 0.08f;
        public const int MaxPilotLevel = 10;
        public const float PilotBonusPerLevel = 0.03f;

        /// <summary>Gold to raise a weapon's mastery from <paramref name="level"/> to level+1: 150·1.55^L, rounded to 10.</summary>
        public static int MasteryCost(int level)
        {
            if (level < 0 || level >= MaxWeaponMastery) throw new ArgumentOutOfRangeException(nameof(level));
            return (int)(Math.Round(150 * Math.Pow(1.55, level) / 10.0, MidpointRounding.AwayFromZero) * 10);
        }

        /// <summary>Gold to raise a pilot from <paramref name="level"/> (1-based) to level+1: 300·1.6^(L−1), rounded to 10.</summary>
        public static int PilotLevelCost(int level)
        {
            if (level < 1 || level >= MaxPilotLevel) throw new ArgumentOutOfRangeException(nameof(level));
            return (int)(Math.Round(300 * Math.Pow(1.6, level - 1) / 10.0, MidpointRounding.AwayFromZero) * 10);
        }

        public static float MasteryMultiplier(int level) => 1f + MasteryDamagePerLevel * Math.Clamp(level, 0, MaxWeaponMastery);

        public static int ExpectedRunGold(int chapterIndex) =>
            (int)Math.Round(250 * Math.Pow(1.35, chapterIndex));

        // ---- Sugar Rush meter (sugar-rush.md §4)

        /// <summary>Combo above this adds no more meter per kill — keeps late-run combos in the hundreds from chaining rushes.</summary>
        public const int RushComboCap = 40;

        /// <summary>Meter gained per kill: value · (1 + min(combo, cap) · bonus).</summary>
        public static float RushMeterGain(float killValue, int combo, float comboBonus) =>
            killValue * (1f + Math.Clamp(combo, 0, RushComboCap) * comboBonus);

        /// <summary>
        /// Meter needed for a rush at run minute t: base · (1 + growth · max(0, t − start)). Kill rate climbs all run,
        /// so the bar must too — but not before <paramref name="startMinutes"/>, so a new player still meets the first rush early.
        /// </summary>
        public static float RushMeterMax(float baseMax, float growthPerMinute, float minutes, float startMinutes = 0f) =>
            baseMax * (1f + growthPerMinute * Math.Max(0f, minutes - startMinutes));

        // ---- Power Match & Forge (power-match.md §4)

        /// <summary>
        /// One Power Match step: the enemy HP scale moves toward the value that brings the average time-to-kill
        /// back to <paramref name="targetTtk"/>, by at most e^(rate·dt) per step, clamped to [1, maxScale].
        /// Killing faster than the target raises it; slower lowers it, never below the authored curve.
        /// </summary>
        public static float PowerMatchStep(float scale, float avgTtk, float targetTtk, float rate, float dt, float maxScale)
        {
            if (avgTtk <= 0f || targetTtk <= 0f) return scale;
            float error = Math.Clamp(MathF.Log(targetTtk / avgTtk), -1f, 1f);
            return Math.Clamp(scale * MathF.Exp(rate * error * dt), 1f, maxScale);
        }

        public const float ForgePowerPerLevel = 0.05f;
        public const float ForgeSpeedPerLevel = 0.025f;
        public const int MaxForgeSpeedLevel = 40;

        /// <summary>Gold for the next Forge level (either track, unlimited): 40·1.14^L, rounded to 5.</summary>
        public static int ForgeCost(int level)
        {
            if (level < 0) throw new ArgumentOutOfRangeException(nameof(level));
            double raw = 40 * Math.Pow(1.14, Math.Min(level, 150));
            return (int)Math.Min(int.MaxValue / 2, Math.Round(raw / 5.0, MidpointRounding.AwayFromZero) * 5);
        }

        /// <summary>Ateş Gücü: damage × (1 + 0.05·L), no cap.</summary>
        public static float ForgePowerMultiplier(int level) => 1f + ForgePowerPerLevel * Math.Max(0, level);

        /// <summary>Ateş Hızı: fire rate × (1 + 0.025·L), capped at level 40 (×2) so bullet density stays readable.</summary>
        public static float ForgeSpeedMultiplier(int level) => 1f + ForgeSpeedPerLevel * Math.Clamp(level, 0, MaxForgeSpeedLevel);

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
