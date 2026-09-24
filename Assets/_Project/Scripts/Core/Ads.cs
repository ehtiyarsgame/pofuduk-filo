using System;

namespace PofudukFilo.Core
{
    /// <summary>
    /// Ad unit configuration. These are Google's public TEST ids — they show "Test Ad" creatives
    /// and pay nothing. Replace with the AdMob console ids (Apps ▸ App settings / Ad units)
    /// before a store release; shipping test ids earns nothing, shipping real ids in debug risks
    /// an account ban for invalid traffic.
    /// </summary>
    public static class AdsConfig
    {
        public const string AndroidAppId = "ca-app-pub-3940256099942544~3347511713";
        public const string RewardedUnitId = "ca-app-pub-3940256099942544/5224354917";
        public const string InterstitialUnitId = "ca-app-pub-3940256099942544/1033173712";
    }

    public interface IInterstitialAdProvider
    {
        bool IsReady { get; }
        /// <summary>Shows the ad; <paramref name="onClosed"/> runs when it closes or fails.</summary>
        void Show(Action onClosed);
    }

    /// <summary>Interstitials between runs, gated by <see cref="AdPacing"/>. No provider = no ads.</summary>
    public static class Interstitials
    {
        public static IInterstitialAdProvider Provider { get; set; }

        public static void ShowIfReady(Action onClosed)
        {
            if (Provider == null || !Provider.IsReady)
            {
                onClosed?.Invoke();
                return;
            }
            Provider.Show(onClosed);
        }
    }

    /// <summary>
    /// When an interstitial may interrupt (design/gdd/meta-economy.md §3.5): never before the
    /// player has finished <see cref="GraceRuns"/> runs, only every <see cref="EveryNthRun"/>
    /// run, never twice within <see cref="MinSecondsBetween"/>, never after a very short run
    /// (a quick restart must stay quick), and never after the player watched a rewarded ad this
    /// run — they already gave us their attention.
    /// </summary>
    public static class AdPacing
    {
        public const int GraceRuns = 2;
        public const int EveryNthRun = 2;
        public const float MinSecondsBetween = 180f;
        public const float MinRunSeconds = 45f;

        public static bool ShouldShowInterstitial(int runsFinished, float secondsSinceLastAd, float runSeconds,
            bool watchedRewardedThisRun)
        {
            if (watchedRewardedThisRun) return false;
            if (runsFinished <= GraceRuns) return false;
            if (runsFinished % EveryNthRun != 0) return false;
            if (secondsSinceLastAd < MinSecondsBetween) return false;
            return runSeconds >= MinRunSeconds;
        }
    }
}
