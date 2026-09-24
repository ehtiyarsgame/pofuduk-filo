using System;

namespace PofudukFilo.Core
{
    /// <summary>An ad network adapter (Unity LevelPlay, AdMob…). Implement and assign to <see cref="RewardedAds.Provider"/>.</summary>
    public interface IRewardedAdProvider
    {
        bool IsReady { get; }
        /// <summary>Shows a rewarded ad; <paramref name="onFinished"/> receives true only if the reward was earned.</summary>
        void Show(Action<bool> onFinished);
    }

    /// <summary>
    /// Optional rewarded-ad continue (game-concept.md §3.5: never mandatory, once per run).
    /// Without a real provider the placeholder grants the reward immediately, so the flow is
    /// testable before an SDK is chosen.
    /// </summary>
    public static class RewardedAds
    {
        public static IRewardedAdProvider Provider { get; set; } = new InstantRewardProvider();

        public static bool IsReady => Provider != null && Provider.IsReady;

        public static void Show(Action<bool> onFinished)
        {
            if (!IsReady)
            {
                onFinished?.Invoke(false);
                return;
            }
            Provider.Show(onFinished);
        }

        private sealed class InstantRewardProvider : IRewardedAdProvider
        {
            public bool IsReady => true;
            public void Show(Action<bool> onFinished) => onFinished?.Invoke(true);
        }
    }
}
