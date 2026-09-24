using PofudukFilo.Core;
using PofudukFilo.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace PofudukFilo.UI
{
    /// <summary>HUD for the signature loop: combo counter, Şeker Hücumu meter, rush glow, fleet toasts.</summary>
    public sealed partial class GameUI
    {
        [SerializeField] private Sprite edgeGlowSprite;

        private SugarRush _rush;
        private HudBar _rushBar;
        private Text _comboText;
        private Image _edgeGlow;
        private float _comboPunch;
        private float _hurtFlash;
        private Button _rushButton;
        private Image _rushButtonTimer;
        private GameObject _bossBanner;
        private Text _bossBannerName;
        private float _bossBannerUntil;

        private void BuildRushHud(Transform hud)
        {
            _rush = FindAnyObjectByType<SugarRush>();
            var fleet = FindAnyObjectByType<Fleet>();

            _rushBar = HudBar.Create(_ui, hud, barTrackSprite, barFillSprite, Palette.Pink, sugarIcon, 26);
            UIFactory.Place(_rushBar, 0.27f, 0.858f, 0.78f, 0.884f);
            _rushBar.Snap(0f);

            _comboText = _ui.Label(hud, "", 52, Palette.Cream, TextAnchor.MiddleRight);
            UIFactory.Place(_comboText, 0.45f, 0.765f, 0.95f, 0.81f); // below the boss bar
            _comboText.rectTransform.pivot = new Vector2(1f, 0.5f); // punch grows leftwards, never off-screen
            _comboText.gameObject.SetActive(false);

            var glowNode = _ui.Node("RushGlow", hud);
            glowNode.SetAsFirstSibling();
            _edgeGlow = glowNode.gameObject.AddComponent<Image>();
            _edgeGlow.sprite = edgeGlowSprite;
            _edgeGlow.type = edgeGlowSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            _edgeGlow.raycastTarget = false;
            _edgeGlow.gameObject.SetActive(false);

            // Sugar Bomb button: bottom-left corner, appears when the meter is full.
            _rushButton = _ui.Button(hud, "ŞEKER!", Palette.HotPink, () => { if (_rush != null) _rush.Activate(); }, 50);
            UIFactory.Place(_rushButton, 0.03f, 0.03f, 0.33f, 0.12f);
            _rushButtonTimer = _ui.Bar(_rushButton.transform, new Color(0f, 0f, 0f, 0f), new Color(1f, 1f, 1f, 0.35f));
            UIFactory.Place(_rushButtonTimer.transform.parent, 0.08f, 0.04f, 0.92f, 0.16f);
            _rushButton.gameObject.SetActive(false);

            BuildBossBanner(hud);

            if (_rush != null)
            {
                _rush.RushReady += () =>
                {
                    _rushButton.gameObject.SetActive(true);
                    Feel.Juice.PopIn(_rushButton.transform);
                    Toast("ŞEKER HAZIR! Dokun!", 1.3f);
                    if (Audio.AudioManager.Instance != null) Audio.AudioManager.Instance.Play(Audio.SfxId.LevelUp, 0.02f);
                };
                _rush.MeterChanged += v => _rushBar.Set(v);
                _rush.ComboChanged += OnComboChanged;
                _rush.RushStarted += () =>
                {
                    _rushButton.gameObject.SetActive(false);
                    Toast("ŞEKER HÜCUMU!", 1.4f);
                    _edgeGlow.gameObject.SetActive(true);
                    if (Audio.AudioManager.Instance != null) Audio.AudioManager.Instance.Play(Audio.SfxId.Evolution);
                    if (Feel.Juice.Instance != null) Feel.Juice.Instance.Shake(0.4f, 0.25f);
                };
                _rush.RushEnded += () => _edgeGlow.gameObject.SetActive(false);
            }
            if (player != null) player.Damaged += _ => _hurtFlash = 1f;
            if (fleet != null)
                fleet.WingmanJoined += n => Toast(n >= Fleet.MaxWingmen ? "Filo güçlendi!" : $"Filoya katıldı! ({n}/{Fleet.MaxWingmen})");
        }

        private void BuildBossBanner(Transform hud)
        {
            Image stripe = _ui.Panel(hud, new Color(0.55f, 0.08f, 0.2f, 0.88f), "BossBanner");
            UIFactory.Place(stripe, -0.05f, 0.56f, 1.05f, 0.7f);
            stripe.raycastTarget = false;
            Text warn = _ui.Label(stripe.transform, "UYARI!", 44, Palette.Cream);
            UIFactory.Place(warn, 0f, 0.62f, 1f, 0.98f);
            _bossBannerName = _ui.Label(stripe.transform, "", 64, Palette.White);
            UIFactory.Place(_bossBannerName, 0.02f, 0.05f, 0.98f, 0.65f);
            _bossBanner = stripe.gameObject;
            _bossBanner.SetActive(false);
        }

        /// <summary>Boss entrance: red warning stripe with the boss's name, siren, shake.</summary>
        private void ShowBossBanner(string label)
        {
            _bossBannerName.text = Loc.T(label);
            _bossBanner.SetActive(true);
            Feel.Juice.PopIn(_bossBanner.transform);
            _bossBannerUntil = Time.unscaledTime + 2.4f;
            _hurtFlash = 0.8f; // red edge pulse
            if (Audio.AudioManager.Instance != null) Audio.AudioManager.Instance.Play(Audio.SfxId.BossWarning);
            if (Feel.Juice.Instance != null) Feel.Juice.Instance.Shake(0.6f, 0.5f);
        }

        private void OnComboChanged(int combo)
        {
            bool show = combo >= 5;
            _comboText.gameObject.SetActive(show);
            if (!show) return;
            _comboText.text = Loc.T($"{combo} KOMBO");
            _comboPunch = 1f;
        }

        private void UpdateRushHud()
        {
            if (_rush == null || _rushBar == null) return;
            float dt = Time.unscaledDeltaTime;

            if (_bossBanner != null && _bossBanner.activeSelf && Time.unscaledTime > _bossBannerUntil) _bossBanner.SetActive(false);

            if (_rushButton != null && _rushButton.gameObject.activeSelf)
            {
                float pulse = 1f + Mathf.Sin(Time.unscaledTime * 9f) * 0.06f;
                _rushButton.transform.localScale = new Vector3(pulse, pulse, 1f);
                _rushButtonTimer.fillAmount = _rush.ReadyTimeLeft01;
                _rushButton.transform.Find("Face").GetComponent<Image>().color =
                    Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * 0.5f, 1f), 0.45f, 1f);
            }

            _hurtFlash = Mathf.MoveTowards(_hurtFlash, 0f, dt * 2.8f);
            if (_hurtFlash > 0f && !_rush.Active)
            {
                _edgeGlow.gameObject.SetActive(true);
                _edgeGlow.color = new Color(1f, 0.15f, 0.2f, 0.75f * _hurtFlash);
                if (_hurtFlash <= 0.01f) _edgeGlow.gameObject.SetActive(false);
            }

            if (_rush.Active)
            {
                // Rainbow bar and a breathing edge glow while the rush lasts.
                Color c = Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * 0.6f, 1f), 0.55f, 1f);
                _rushBar.Color = c;
                c.a = 0.35f + 0.2f * Mathf.Sin(Time.unscaledTime * 8f);
                _edgeGlow.color = c;
                _rushBar.Set(_rushBar.Value, Loc.T("HÜCUM!"));
            }
            else
            {
                _rushBar.Color = _rushBar.Value > 0.85f ? Color.Lerp(Palette.Pink, Palette.White, Mathf.PingPong(Time.unscaledTime * 3f, 1f)) : Palette.Pink;
                _rushBar.Set(_rushBar.Value, Loc.T(_rush.Ready ? "ŞEKER HAZIR!" : "ŞEKER"));
            }

            if (_comboText.gameObject.activeSelf)
            {
                _comboPunch = Mathf.MoveTowards(_comboPunch, 0f, dt * 5f);
                float s = 1f + _comboPunch * 0.35f;
                _comboText.transform.localScale = new Vector3(s, s, 1f);
                Color cc = Color.Lerp(Palette.Cream, Palette.HotPink, Mathf.Clamp01(_rush.Combo / 60f));
                cc.a = 0.5f + 0.5f * _rush.ComboTimeLeft01;
                _comboText.color = cc;
            }
        }
    }
}
