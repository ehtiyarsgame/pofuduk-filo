using System;
using PofudukFilo.Core;
using PofudukFilo.Meta;
using PofudukFilo.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace PofudukFilo.UI
{
    /// <summary>
    /// Opt-in rewarded ads outside the run (design/gdd/ad-rewards.md): unlock pilots and weapons with a few
    /// ads instead of gold, try one for a run ("Dene"), and a daily gift. Every placement is a button the
    /// player chooses to press, shows what it pays before the ad, and pays right after it.
    /// </summary>
    public sealed partial class GameUI
    {
        [SerializeField] private Sprite adIcon;
        [SerializeField] private Sprite giftIcon;

        private Button _giftButton;
        private Text _powerText;
        private readonly System.Collections.Generic.List<Button> _walletAdButtons = new();
        private Text _giftText;
        private Text _menuToast;
        private float _menuToastUntil;
        private float _nextGiftRefresh;

        private static long Now => DateTime.UtcNow.Ticks;

        // ---------------------------------------------------------------- Menu: daily gift

        private void BuildRewards(Transform menu, Transform safeRoot)
        {
            // The gift button itself is a side icon of the lobby (BuildMenu); this adds the toast it reports into.
            _menuToast = _ui.Label(safeRoot, "", 56, Palette.Cream);
            UIFactory.Place(_menuToast, 0.05f, 0.62f, 0.95f, 0.7f);
            _menuToast.gameObject.SetActive(false);
        }

        private void RefreshGift()
        {
            if (_giftButton == null) return;
            MetaProgressionService meta = run.Meta;
            long now = Now;
            bool ready = meta.CanClaimGift(now);
            string label;
            if (ready) label = Loc.T("HAZIR");
            else if (meta.GiftsLeftToday(now) == 0) label = Loc.T("Yarın");
            else
            {
                TimeSpan wait = meta.GiftWait(now);
                label = $"{(int)wait.TotalMinutes}:{wait.Seconds:00}";
            }
            _giftText.text = label;
            if (_powerText != null) _powerText.text = Loc.T($"GÜÇ ×{meta.PowerRating(meta.SelectedCharacterId):0.00}");
            _giftText.color = ready ? Palette.Hex(0xFFE45C) : Palette.White;
            // The lobby tile stays pressable (a tap while waiting says when); the red dot marks a ready gift.
            if (_giftDot != null) _giftDot.SetActive(ready);
            foreach (Button b in _walletAdButtons) b.interactable = ready;
        }

        private void ClaimGift()
        {
            if (!run.Meta.CanClaimGift(Now))
            {
                MenuToast(run.Meta.GiftsLeftToday(Now) == 0 ? Loc.T("Bugünkü hediyeler bitti, yarın gel!")
                    : Loc.T($"Sonraki hediye: {_giftText.text}"));
                return;
            }
            WatchAd(() =>
            {
                int gold = run.Meta.ClaimGift(Now);
                if (gold <= 0) return;
                MenuToast(Loc.T($"Hediye: +{gold} altın, +{AdRules.GiftStardust} yıldız tozu!"));
                RefreshMenu();
            });
        }

        private void UpdateRewards()
        {
            if (_menuToast != null && _menuToast.gameObject.activeSelf && Time.unscaledTime > _menuToastUntil)
                _menuToast.gameObject.SetActive(false);
            if (_giftButton == null || _menu == null) return;
            float t = Time.unscaledTime;
            if (!_menu.activeSelf)
            {
                // Meta screens: keep their wallet ad buttons in step with the cooldown.
                if (t >= _nextGiftRefresh && run.State == GameState.MainMenu)
                {
                    _nextGiftRefresh = t + 1f;
                    RefreshGift();
                }
                return;
            }

            bool ready = _giftButton.interactable;
            // A ready gift wiggles now and then — a nudge, not a nag.
            float wiggle = ready ? Mathf.Sin(t * 14f) * 8f * Mathf.Clamp01(Mathf.Sin(t * 1.3f) * 4f - 3f) : 0f;
            _giftButton.transform.localRotation = Quaternion.Euler(0f, 0f, wiggle);
            if (t >= _nextGiftRefresh)
            {
                _nextGiftRefresh = t + 1f;
                RefreshGift();
            }
        }

        private void MenuToast(string text, float seconds = 2.2f)
        {
            if (_menuToast == null) return;
            _menuToast.text = Loc.T(text);
            _menuToast.gameObject.SetActive(true);
            _menuToast.transform.SetAsLastSibling();
            _menuToastUntil = Time.unscaledTime + seconds;
            Feel.Juice.PopIn(_menuToast.transform);
        }

        // ---------------------------------------------------------------- Watching

        /// <summary>Shows a rewarded ad and runs <paramref name="onEarned"/> only if it was watched to the end.</summary>
        private void WatchAd(Action onEarned)
        {
            if (!RewardedAds.IsReady)
            {
                MenuToast("Reklam yükleniyor, birazdan tekrar dene.");
                return;
            }
            RewardedAds.Show(earned =>
            {
                if (earned) onEarned();
                else MenuToast("Reklam yarıda kaldı, ödül verilmedi.");
            });
        }

        /// <summary>
        /// A button that shows its price in ads: an optional word on top and "[TV] 1/4" below.
        /// </summary>
        private Button AdButton(Transform parent, string label, string counter, Color face, Action onClick, int fontSize)
        {
            Button b = _ui.Button(parent, label, face, onClick, fontSize);
            Transform faceNode = b.transform.Find("Face");
            Text labelText = b.GetComponentInChildren<Text>();
            bool hasLabel = !string.IsNullOrEmpty(label);
            if (hasLabel) UIFactory.Place(labelText, 0.02f, 0.5f, 0.98f, 0.98f);
            RectTransform row = CurrencyRow(faceNode, TextAnchor.MiddleCenter);
            UIFactory.Place(row, 0.04f, hasLabel ? 0.06f : 0.1f, 0.96f, hasLabel ? 0.52f : 0.9f);
            AddAmount(row, adIcon, counter, fontSize, Palette.White);
            return b;
        }

        // ---------------------------------------------------------------- Hangar & Lab rows

        /// <summary>"Dene [TV]" under a locked item's text: one ad buys one run with it.</summary>
        private void AddTrialButton(Transform row, Action onClick)
        {
            Button trial = AdButton(row, "Dene", "", Palette.Lavender, onClick, 30);
            UIFactory.Place(trial, 0.22f, 0.04f, 0.46f, 0.3f);
        }

        /// <summary>"Dene": one ad = one run with the pilot, and the ad also counts towards unlocking it.</summary>
        private void TryPilot(CharacterDefinition c)
        {
            if (!CanStartTrial()) return;
            WatchAd(() =>
            {
                HideMetaScreens();
                run.StartTrial(c);
            });
        }

        private void TryWeapon(WeaponDefinition w)
        {
            if (!CanStartTrial()) return;
            WatchAd(() =>
            {
                HideMetaScreens();
                run.StartTrial(w);
            });
        }

        /// <summary>A trial starts a new run, so it would replace a saved one — ask the player to finish that first.</summary>
        private bool CanStartTrial()
        {
            if (!run.HasSavedRun) return true;
            MenuToast("Önce kayıtlı oyununa devam et.");
            return false;
        }
    }
}
