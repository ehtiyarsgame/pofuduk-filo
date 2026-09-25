using System;

namespace PofudukFilo.Meta
{
    /// <summary>
    /// Pacing for opt-in rewarded ads (design/gdd/ad-rewards.md). Pure rules: callers pass the UTC time.
    /// Ads are never forced — each one is a button the player chose to press, and each pays something.
    /// </summary>
    public static class AdRules
    {
        /// <summary>Unlock/trial views per UTC day: a new pilot takes a few days of ads, not one sitting.</summary>
        public const int UnlockViewsPerDay = 8;
        /// <summary>
        /// Ads for gold and Stardust — the main ad placement (device feedback 2026-09-25: "para için sürekli
        /// izlesinler"): up to 10 a day, 5 minutes apart, so a player can top up between runs.
        /// </summary>
        public const int GiftsPerDay = 10;
        public static readonly TimeSpan GiftCooldown = TimeSpan.FromMinutes(5);
        public const int GiftStardust = 2;

        public static long DayOf(long utcTicks) => utcTicks / TimeSpan.TicksPerDay;

        /// <summary>Time until the next gift may be claimed; zero if it is ready now.</summary>
        public static TimeSpan GiftWait(long lastGiftTicks, long nowTicks)
        {
            if (lastGiftTicks <= 0 || nowTicks < lastGiftTicks) return TimeSpan.Zero; // clock set back: don't lock out
            TimeSpan left = GiftCooldown - TimeSpan.FromTicks(nowTicks - lastGiftTicks);
            return left > TimeSpan.Zero ? left : TimeSpan.Zero;
        }
    }
}
