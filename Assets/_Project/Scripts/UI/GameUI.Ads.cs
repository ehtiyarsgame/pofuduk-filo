using PofudukFilo.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PofudukFilo.UI
{
    /// <summary>
    /// Ad placements (meta-economy.md §3.5): opt-in rewarded ads only where they help the player
    /// (continue after death, double the run's gold), and paced interstitials between runs.
    /// Never during play, never a banner.
    /// </summary>
    public sealed partial class GameUI
    {
        private Button _doubleGoldButton;
        private int _lastRunGold;
        private bool _rewardedThisRun;
        private float _lastInterstitialAt = -9999f;
        private float _runStartedAt;
        private float _lastRunSeconds;

        private const string RunsFinishedKey = "pf.runsFinished";

        private void BuildAdPlacements(Transform runEndScreen)
        {
            _doubleGoldButton = _ui.Button(runEndScreen, "Altını 2 Katla (Reklam)", Palette.Mint, WatchForDoubleGold, 38);
            UIFactory.Place(_doubleGoldButton, 0.2f, 0.415f, 0.8f, 0.47f);
            _doubleGoldButton.gameObject.SetActive(false);

            run.StateChanged += s =>
            {
                if (s == GameState.Playing && _prevState is GameState.MainMenu or GameState.RunEnd)
                {
                    _rewardedThisRun = false;
                    _runStartedAt = Time.realtimeSinceStartup;
                }
                if (s == GameState.RunEnd) _lastRunSeconds = Time.realtimeSinceStartup - _runStartedAt;
                _prevState = s;
            };
        }

        private GameState _prevState = GameState.MainMenu;

        private void OnRunEndedAds(RunSummary s)
        {
            _lastRunGold = s.Gold;
            PlayerPrefs.SetInt(RunsFinishedKey, PlayerPrefs.GetInt(RunsFinishedKey, 0) + 1);
            _doubleGoldButton.gameObject.SetActive(s.Gold > 0 && RewardedAds.IsReady);
            _doubleGoldButton.interactable = true;
        }

        private void WatchForDoubleGold()
        {
            _doubleGoldButton.interactable = false;
            RewardedAds.Show(earned =>
            {
                _rewardedThisRun = true;
                if (!earned)
                {
                    _doubleGoldButton.interactable = true;
                    return;
                }
                run.Meta.GrantRunRewards(_lastRunGold, 0);
                _goldTarget += _lastRunGold;
                _doubleGoldButton.gameObject.SetActive(false);
                Toast("Altın 2 katına çıktı!");
            });
        }

        /// <summary>Death-screen continue: marks the run so no interstitial follows it.</summary>
        private void WatchForRevive() => RewardedAds.Show(earned =>
        {
            _rewardedThisRun = true;
            if (earned) run.Revive(free: true);
        });

        /// <summary>Runs <paramref name="next"/>, showing an interstitial first when pacing allows.</summary>
        private void AfterRunAd(System.Action next)
        {
            int runs = PlayerPrefs.GetInt(RunsFinishedKey, 0);
            float since = Time.realtimeSinceStartup - _lastInterstitialAt;
            if (!AdPacing.ShouldShowInterstitial(runs, since, _lastRunSeconds, _rewardedThisRun))
            {
                next();
                return;
            }
            _lastInterstitialAt = Time.realtimeSinceStartup;
            Interstitials.ShowIfReady(next);
        }
    }
}
