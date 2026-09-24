#if ADMOB
using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace PofudukFilo.Core
{
    /// <summary>
    /// Google AdMob adapter (compiled only when the com.google.ads.mobile package is present —
    /// the ADMOB define comes from the runtime asmdef's versionDefines). Flow: UMP consent
    /// (GDPR/EEA form when required) → MobileAds.Initialize → keep one rewarded and one
    /// interstitial loaded, reloading after each show or failure with backoff.
    /// </summary>
    public sealed class AdMobAds : IRewardedAdProvider, IInterstitialAdProvider
    {
        private static AdMobAds s_instance;

        private RewardedAd _rewarded;
        private InterstitialAd _interstitial;
        private bool _initialized;
        private int _rewardedRetry;
        private int _interstitialRetry;

        bool IRewardedAdProvider.IsReady => _rewarded != null && _rewarded.CanShowAd();
        bool IInterstitialAdProvider.IsReady => _interstitial != null && _interstitial.CanShowAd();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (s_instance != null || Array.IndexOf(Environment.GetCommandLineArgs(), "-qa-shots") >= 0) return;
            s_instance = new AdMobAds();
            s_instance.RequestConsentThenInit();
        }

        private void RequestConsentThenInit()
        {
            MobileAds.RaiseAdEventsOnUnityMainThread = true;
            var request = new ConsentRequestParameters();
            ConsentInformation.Update(request, updateError =>
            {
                if (updateError != null) Debug.LogWarning($"[Ads] Consent update: {updateError.Message}");
                ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                {
                    if (formError != null) Debug.LogWarning($"[Ads] Consent form: {formError.Message}");
                    if (ConsentInformation.CanRequestAds()) Init();
                });
            });
        }

        private void Init()
        {
            if (_initialized) return;
            _initialized = true;
            MobileAds.Initialize(_ =>
            {
                RewardedAds.Provider = this;
                Interstitials.Provider = this;
                LoadRewarded();
                LoadInterstitial();
            });
        }

        // ---------------------------------------------------------------- Rewarded

        private void LoadRewarded()
        {
            _rewarded?.Destroy();
            _rewarded = null;
            RewardedAd.Load(AdsConfig.RewardedUnitId, new AdRequest(), (ad, error) =>
            {
                if (error != null || ad == null)
                {
                    Retry(ref _rewardedRetry, LoadRewarded);
                    return;
                }
                _rewardedRetry = 0;
                _rewarded = ad;
            });
        }

        void IRewardedAdProvider.Show(Action<bool> onFinished)
        {
            RewardedAd ad = _rewarded;
            if (ad == null || !ad.CanShowAd())
            {
                onFinished?.Invoke(false);
                return;
            }

            bool earned = false;
            bool done = false;
            void Finish()
            {
                if (done) return;
                done = true;
                AudioListener.pause = false;
                onFinished?.Invoke(earned);
                LoadRewarded();
            }

            ad.OnAdFullScreenContentClosed += Finish;
            ad.OnAdFullScreenContentFailed += _ => Finish();
            AudioListener.pause = true;
            ad.Show(_ => earned = true);
        }

        // ---------------------------------------------------------------- Interstitial

        private void LoadInterstitial()
        {
            _interstitial?.Destroy();
            _interstitial = null;
            InterstitialAd.Load(AdsConfig.InterstitialUnitId, new AdRequest(), (ad, error) =>
            {
                if (error != null || ad == null)
                {
                    Retry(ref _interstitialRetry, LoadInterstitial);
                    return;
                }
                _interstitialRetry = 0;
                _interstitial = ad;
            });
        }

        void IInterstitialAdProvider.Show(Action onClosed)
        {
            InterstitialAd ad = _interstitial;
            if (ad == null || !ad.CanShowAd())
            {
                onClosed?.Invoke();
                return;
            }

            bool done = false;
            void Finish()
            {
                if (done) return;
                done = true;
                AudioListener.pause = false;
                onClosed?.Invoke();
                LoadInterstitial();
            }

            ad.OnAdFullScreenContentClosed += Finish;
            ad.OnAdFullScreenContentFailed += _ => Finish();
            AudioListener.pause = true;
            ad.Show();
        }

        /// <summary>Exponential backoff (2, 4, 8 … 64 s) so a no-fill never hammers the network.</summary>
        private static void Retry(ref int attempt, Action load)
        {
            attempt = Mathf.Min(attempt + 1, 6);
            float delay = Mathf.Pow(2f, attempt);
            AdScheduler.Run(delay, load);
        }
    }
}
#endif
