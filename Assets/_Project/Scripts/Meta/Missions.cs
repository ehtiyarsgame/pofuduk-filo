using System;

namespace PofudukFilo.Meta
{
    public enum MissionKind
    {
        Kills,     // enemies defeated (total today)
        Minutes,   // survive this long in a single run (best today)
        Gold,      // gold earned from runs (total today)
        Bosses,    // bosses defeated (total today)
        Rushes,    // Sugar Rushes triggered (total today)
        Runs       // runs played (total today)
    }

    public readonly struct MissionDef
    {
        public readonly MissionKind Kind;
        public readonly int Target;
        public readonly int RewardGold;
        public readonly int RewardDust;

        public MissionDef(MissionKind kind, int target, int rewardGold, int rewardDust)
        {
            Kind = kind;
            Target = target;
            RewardGold = rewardGold;
            RewardDust = rewardDust;
        }

        /// <summary>True when progress for this kind is the best single run rather than a daily total.</summary>
        public bool IsBestOfRun => Kind == MissionKind.Minutes;
    }

    /// <summary>
    /// Daily missions and the login streak (retention.md): a reason to come back every day. Pure rules — the day is
    /// passed in (UTC day number), the three missions are chosen deterministically from it so every device agrees.
    /// </summary>
    public static class Missions
    {
        public const int PerDay = 3;

        private static readonly (MissionKind kind, int[] targets, int gold, int dust)[] Pool =
        {
            (MissionKind.Kills, new[] { 300, 500, 800 }, 250, 0),
            (MissionKind.Minutes, new[] { 3, 4, 5 }, 300, 1),
            (MissionKind.Gold, new[] { 600, 1000, 1500 }, 250, 0),
            (MissionKind.Bosses, new[] { 1, 2 }, 200, 3),
            (MissionKind.Rushes, new[] { 2, 4, 6 }, 200, 1),
            (MissionKind.Runs, new[] { 3, 5 }, 150, 1)
        };

        /// <summary>Three missions of different kinds for the given UTC day.</summary>
        public static MissionDef[] ForDay(long day)
        {
            var rng = new Random(unchecked((int)(day * 7919 + 17)));
            var kinds = new int[Pool.Length];
            for (int i = 0; i < kinds.Length; i++) kinds[i] = i;
            for (int i = kinds.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (kinds[i], kinds[j]) = (kinds[j], kinds[i]);
            }
            var result = new MissionDef[PerDay];
            for (int i = 0; i < PerDay; i++)
            {
                var p = Pool[kinds[i]];
                int tier = rng.Next(p.targets.Length);
                // Harder tiers pay more: +50 % per tier.
                result[i] = new MissionDef(p.kind, p.targets[tier], p.gold + p.gold * tier / 2, p.dust + tier);
            }
            return result;
        }

        /// <summary>Login streak after checking in on <paramref name="today"/>: +1 for consecutive days, back to 1 after a gap.</summary>
        public static int NextStreak(int streak, long lastDay, long today)
        {
            if (lastDay == today) return Math.Max(1, streak);
            return lastDay == today - 1 ? streak + 1 : 1;
        }

        /// <summary>Streak reward: day 1 → 100 gold … day 7 → 1000 gold + 5 Stardust, then the week repeats.</summary>
        public static (int gold, int dust) StreakReward(int streak)
        {
            int d = (Math.Max(1, streak) - 1) % 7 + 1;
            int[] gold = { 100, 150, 200, 300, 400, 600, 1000 };
            return (gold[d - 1], d == 7 ? 5 : d >= 4 ? 1 : 0);
        }
    }
}
