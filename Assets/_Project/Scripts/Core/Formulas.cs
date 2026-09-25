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
        // XP(n) = floor(10 + 9n + 0.9 n^1.7) — raised from 5 + 6n (device feedback 2026-09-24: level-ups came
        // almost every few seconds at the start and the build got strong too early).
        public const float XpBase = 10f;
        public const float XpLinear = 9f;
        public const float XpCoefficient = 0.9f;
        public const float XpExponent = 1.7f;
        /// <summary>Horde density (threat.md §3.6) brings about 3.25× the kills, so every level needs 3.25× the XP.</summary>
        public const double XpScale = 3.25;

        /// <summary>XP required to go from <paramref name="level"/> to level + 1 (level starts at 1).</summary>
        public static int XpToNextLevel(int level)
        {
            if (level < 1) throw new ArgumentOutOfRangeException(nameof(level));
            return (int)Math.Floor(XpScale * (XpBase + XpLinear * level + XpCoefficient * Math.Pow(level, XpExponent)));
        }

        /// <summary>HP(t, c) = HP_base · (1 + 0.35t + 0.06t²) · 1.22^c — a DPS race (threat.md §3.1): without upgrades the
        /// swarm outgrows the player's damage (×4.25 at 5 min, ×10.5 at 10 min).</summary>
        public static float EnemyHp(float baseHp, float minutes, int chapterIndex)
        {
            // device feedback 2026-09-25: "10 dakikadan fazla oynayabiliyorum, çok geliştirme yapmadan" at 0.25m + 0.035m²
            float timeScale = 1f + 0.35f * minutes + 0.06f * minutes * minutes;
            return baseHp * timeScale * (float)Math.Pow(1.22, chapterIndex);
        }

        /// <summary>
        /// Enemy bullet damage × (1 + 0.15t + 0.025t²), t in run minutes (threat.md §3.1). The un-upgraded player
        /// must eventually fall behind — Ball Blast's wall — so Forge levels and ads decide how far a run goes.
        /// ×1 at start, ×1.7 at 3 min, ×2.4 at 5 min, ×3.3 at 7 min (QA run 45 at 0.3/0.03: bot dead at 142 s;
        /// run 51 at 0.2 with the full enemy mix: 155 s).
        /// </summary>
        public static float EnemyDamageScale(float minutes)
        {
            float m = Math.Max(0f, minutes);
            return 1f + 0.25f * m + 0.04f * m * m; // humans dodge far better than the QA bot (threat.md §3.1)
        }

        // ---------------------------------------------------------------- Leaks (threat.md §3.4)

        /// <summary>
        /// Share of the player's max HP lost when an enemy escapes off the bottom:
        /// clamp(2 % + 0.1 %·baseHp, 3 %, 10 %) (×2 for elites) × max(0.2, hpLeft).
        /// Chick 3 %, Cookie Robot 4.5 %, Jelly Bear 6.5 %, Ice Cream Tower 9 %, elite ≤ 20 %.
        /// This is the DPS wall (Ball Blast): dodging alone no longer keeps a run alive — only damage does, so
        /// upgrades decide how far a run goes. A nearly dead leaker costs a fifth of a full one.
        /// </summary>
        public static float LeakDamageFraction(float enemyBaseHp, float hpLeftFraction, bool elite)
        {
            float size = Math.Clamp(0.02f + 0.001f * Math.Max(0f, enemyBaseHp), 0.03f, 0.10f) * (elite ? 2f : 1f);
            return size * Math.Max(0.2f, Math.Clamp(hpLeftFraction, 0f, 1f));
        }

        /// <summary>Enemy fire-rate multiplier: 1 + 0.1t, capped at ×2.2 (reached at 12 min).</summary>
        public static float EnemyFireRateScale(float minutes) => Math.Min(1f + 0.1f * Math.Max(0f, minutes), 2.2f);

        /// <summary>Threat points per second the spawner may spend. Budget(t) = 1.5 + 0.9t + 0.08t², scaled by DDA (QA run 12 at 2 + 0.9t: dead at 12 s; run 13 at 1.2 + 0.75t: never below 70 % HP).</summary>
        public static float SpawnBudget(float minutes, float ddaMultiplier)
        {
            float clamped = Math.Clamp(ddaMultiplier, 0.75f, 1.15f);
            // threat.md §3.6, the horde (2026-09-25: "canavarları çoklatabiliriz… hala çok basit"): 2.5× the old
            // 2 + 1.1t + 0.12t², with lighter fodder, so a strong build mows a crowd and a weak one drowns in leaks.
            // 2026-09-25 again: "düşmanları biraz daha arttıralım" — ×1.3 on top (3.25× the original growth).
            // The start is gentler (3.5, not 6.5): QA run 67 had 40 enemies on screen at 10 s and the Lv1 gun died at 28 s.
            return (3.5f + 3.6f * minutes + 0.39f * minutes * minutes) * clamped;
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
        public const float MasteryDamagePerLevel = 0.04f; // was 0.08 (economy.md §5: "yüzdeler çok yüksek")
        public const int MaxPilotLevel = 10;
        public const float PilotBonusPerLevel = 0.03f;

        /// <summary>Gold to raise a weapon's mastery from <paramref name="level"/> to level+1: 300·1.6^L, rounded to 10.</summary>
        public static int MasteryCost(int level)
        {
            if (level < 0 || level >= MaxWeaponMastery) throw new ArgumentOutOfRangeException(nameof(level));
            return (int)(Math.Round(300 * Math.Pow(1.6, level) / 10.0, MidpointRounding.AwayFromZero) * 10);
        }

        /// <summary>Gold to raise a pilot from <paramref name="level"/> (1-based) to level+1: 600·1.6^(L−1), rounded to 10.</summary>
        public static int PilotLevelCost(int level)
        {
            if (level < 1 || level >= MaxPilotLevel) throw new ArgumentOutOfRangeException(nameof(level));
            return (int)(Math.Round(600 * Math.Pow(1.6, level - 1) / 10.0, MidpointRounding.AwayFromZero) * 10);
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

        // ---- Screen fit (design/ux/screen-fit.md)

        /// <summary>Phone reference aspect (1080×2400) the playfield width is designed for.</summary>
        public const float DesignAspect = 0.45f;
        /// <summary>Widest aspect shown (9:16); wider screens (tablets) are pillarboxed to a centred column.</summary>
        public const float MaxAspect = 0.5625f;

        /// <summary>
        /// Camera fit for any portrait screen: never narrower than the designed playfield width (tall phones see more
        /// height, same width), never wider than <see cref="MaxAspect"/> (tablets get a centred 9:16 column).
        /// Returns the orthographic size and the viewport width fraction (1 = full width).
        /// </summary>
        public static (float size, float viewportWidth) FitCamera(float screenAspect, float baseSize)
        {
            if (screenAspect <= 0f) return (baseSize, 1f);
            float aspect = Math.Min(screenAspect, MaxAspect);
            float designWidth = 2f * baseSize * DesignAspect;
            float size = Math.Max(baseSize, designWidth / (2f * aspect));
            float viewport = screenAspect > MaxAspect ? MaxAspect / screenAspect : 1f;
            return (size, viewport);
        }

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

        // economy.md §5 (2026-09-25): one run's gold used to buy ×1.8 damage ("ilk oyundan sonra herşeyi baya
        // geliştirdim… yüzdeler çok yüksek, saçma ve ucuz"). Now a level is a small step and costs three times more.
        public const float ForgePowerPerLevel = 0.02f;
        public const float ForgeSpeedPerLevel = 0.01f;
        public const int MaxForgeSpeedLevel = 40;

        /// <summary>Gold for the next Forge level (either track, unlimited): 120·1.16^L, rounded to 5.</summary>
        public static int ForgeCost(int level)
        {
            if (level < 0) throw new ArgumentOutOfRangeException(nameof(level));
            double raw = 120 * Math.Pow(1.16, Math.Min(level, 150));
            return (int)Math.Min(int.MaxValue / 2, Math.Round(raw / 5.0, MidpointRounding.AwayFromZero) * 5);
        }

        /// <summary>Ateş Gücü: damage × (1 + 0.02·L), no cap.</summary>
        public static float ForgePowerMultiplier(int level) => 1f + ForgePowerPerLevel * Math.Max(0, level);

        /// <summary>Ateş Hızı: fire rate × (1 + 0.01·L), capped at level 40 (×1.4) so bullet density stays readable.</summary>
        public static float ForgeSpeedMultiplier(int level) => 1f + ForgeSpeedPerLevel * Math.Clamp(level, 0, MaxForgeSpeedLevel);

        // ---------------------------------------------------------------- Power coefficient & coins (economy.md)

        /// <summary>
        /// Güç Katsayısı: how far the player has built up outside the run.
        /// P = 1 + 0.02·ForgePower + 0.01·ForgeSpeed + 0.01·(workshop levels) + 0.02·(weapon mastery levels)
        ///       + 0.03·(pilot level − 1), tracking what the levels really add. Fresh save = ×1.00; Forge 10/10 + 10
        ///       workshop levels = ×1.40.
        /// </summary>
        public static float PowerRating(int forgePower, int forgeSpeed, int workshopLevels, int masteryLevels, int pilotLevel) =>
            1f + 0.02f * Math.Max(0, forgePower) + 0.01f * Math.Max(0, forgeSpeed) + 0.01f * Math.Max(0, workshopLevels)
               + 0.02f * Math.Max(0, masteryLevels) + 0.03f * Math.Max(0, pilotLevel - 1);

        /// <summary>Enemy HP multiplier from the player's power: √P (P = 2 → ×1.41), so upgrades always net out stronger.</summary>
        public static float EnemyHpForPower(float powerRating) => (float)Math.Sqrt(Math.Max(1f, powerRating));

        /// <summary>
        /// Coin value = base × √P × (1 + 0.04·t), t in run minutes: stronger players still earn bigger coins, but
        /// √P (was P) and 0.04 (was 0.08) stop the snowball where every upgrade paid for the next one faster
        /// (economy.md §5: "para bu kadar kolay kazanılmamalı").
        /// </summary>
        public static float CoinValue(float baseValue, float powerRating, float minutes) =>
            baseValue * (float)Math.Sqrt(Math.Max(1f, powerRating)) * (1f + 0.04f * Math.Max(0f, minutes));

        // ---------------------------------------------------------------- Rewarded ads (ad-rewards.md)

        /// <summary>
        /// Daily gift gold: worth about one Forge level at the player's current depth, so the gift keeps its
        /// value as the economy grows — max(100, ForgeCost(⌊(power+speed)/2⌋)).
        /// </summary>
        public static int GiftGold(int forgePower, int forgeSpeed) =>
            Math.Max(100, ForgeCost(Math.Max(0, forgePower + forgeSpeed) / 2));

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
