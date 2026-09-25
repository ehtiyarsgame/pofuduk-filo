using System;

namespace PofudukFilo.Meta
{
    /// <summary>
    /// Record chests (menu v4, design/ux/main-menu.md §3): one-time rewards along the menu's chest track, opened when the
    /// best endless time first reaches each mark. The track shows the next goal right under PLAY — "one more minute
    /// and I open the next chest". Pure rules.
    /// </summary>
    public static class RecordChests
    {
        public static readonly int[] Seconds = { 90, 180, 300, 420, 600 };
        public static readonly int[] Gold = { 200, 400, 700, 1000, 1600 };
        public static readonly int[] Dust = { 0, 2, 4, 6, 10 };

        public static int Count => Seconds.Length;

        public static bool Reached(int index, float bestSeconds) => bestSeconds >= Seconds[index];

        public static bool IsClaimed(int claimedMask, int index) => (claimedMask & (1 << index)) != 0;

        public static bool CanClaim(int index, float bestSeconds, int claimedMask) =>
            index >= 0 && index < Count && Reached(index, bestSeconds) && !IsClaimed(claimedMask, index);

        /// <summary>Fill of the track 0..1: piecewise between the marks so each chest sits at an even step.</summary>
        public static float Progress(float bestSeconds)
        {
            if (bestSeconds <= 0f) return 0f;
            float prev = 0f;
            for (int i = 0; i < Count; i++)
            {
                if (bestSeconds < Seconds[i])
                    return (i + (bestSeconds - prev) / (Seconds[i] - prev)) / Count;
                prev = Seconds[i];
            }
            return 1f;
        }

        /// <summary>Index of the first chest not yet reached, or −1 when all are reached.</summary>
        public static int NextGoal(float bestSeconds)
        {
            for (int i = 0; i < Count; i++) if (!Reached(i, bestSeconds)) return i;
            return -1;
        }
    }
}
