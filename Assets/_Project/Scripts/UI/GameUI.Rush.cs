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
        private Image _rushFill;
        private Text _rushLabel;
        private Text _comboText;
        private Image _edgeGlow;
        private float _comboPunch;

        private void BuildRushHud(Transform hud)
        {
            _rush = FindAnyObjectByType<SugarRush>();
            var fleet = FindAnyObjectByType<Fleet>();

            _rushFill = _ui.Bar(hud, Palette.Outline, Palette.Pink);
            UIFactory.Place(_rushFill.transform.parent, 0.36f, 0.912f, 0.64f, 0.93f);
            _rushLabel = _ui.Label(hud, "ŞEKER", 30, Palette.Cream);
            UIFactory.Place(_rushLabel, 0.36f, 0.888f, 0.64f, 0.912f);

            _comboText = _ui.Label(hud, "", 56, Palette.Cream, TextAnchor.MiddleRight);
            UIFactory.Place(_comboText, 0.55f, 0.855f, 0.97f, 0.9f);
            _comboText.gameObject.SetActive(false);

            var glowNode = _ui.Node("RushGlow", hud);
            glowNode.SetAsFirstSibling();
            _edgeGlow = glowNode.gameObject.AddComponent<Image>();
            _edgeGlow.sprite = edgeGlowSprite;
            _edgeGlow.type = edgeGlowSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            _edgeGlow.raycastTarget = false;
            _edgeGlow.gameObject.SetActive(false);

            if (_rush != null)
            {
                _rush.MeterChanged += v => _rushFill.fillAmount = v;
                _rush.ComboChanged += OnComboChanged;
                _rush.RushStarted += () =>
                {
                    Toast("ŞEKER HÜCUMU!", 1.4f);
                    _edgeGlow.gameObject.SetActive(true);
                    if (Audio.AudioManager.Instance != null) Audio.AudioManager.Instance.Play(Audio.SfxId.Evolution);
                    if (Feel.Juice.Instance != null) Feel.Juice.Instance.Shake(0.4f, 0.25f);
                };
                _rush.RushEnded += () => _edgeGlow.gameObject.SetActive(false);
            }
            if (fleet != null)
                fleet.WingmanJoined += n => Toast(n >= Fleet.MaxWingmen ? "Filo güçlendi!" : $"Filoya katıldı! ({n}/{Fleet.MaxWingmen})");
        }

        private void OnComboChanged(int combo)
        {
            bool show = combo >= 5;
            _comboText.gameObject.SetActive(show);
            if (!show) return;
            _comboText.text = $"{combo} KOMBO";
            _comboPunch = 1f;
        }

        private void UpdateRushHud()
        {
            if (_rush == null || _rushFill == null) return;
            float dt = Time.unscaledDeltaTime;

            if (_rush.Active)
            {
                // Rainbow bar and a breathing edge glow while the rush lasts.
                Color c = Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * 0.6f, 1f), 0.55f, 1f);
                _rushFill.color = c;
                c.a = 0.35f + 0.2f * Mathf.Sin(Time.unscaledTime * 8f);
                _edgeGlow.color = c;
                _rushLabel.text = "HÜCUM!";
            }
            else
            {
                _rushFill.color = _rushFill.fillAmount > 0.85f ? Color.Lerp(Palette.Pink, Palette.White, Mathf.PingPong(Time.unscaledTime * 3f, 1f)) : Palette.Pink;
                _rushLabel.text = "ŞEKER";
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
