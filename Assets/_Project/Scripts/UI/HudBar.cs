using UnityEngine;
using UnityEngine.UI;

namespace PofudukFilo.UI
{
    /// <summary>
    /// HUD bar (design/ux/hud.md): a sliced pill track, a glossy fill that eases to its target,
    /// a pale "ghost" that lingers where the value was before a drop (so a hit reads at a glance),
    /// an optional round icon badge on the left end and centred value text.
    /// </summary>
    public sealed class HudBar : MonoBehaviour
    {
        private RectTransform _fill;
        private RectTransform _ghost;
        private Image _fillImage;
        private Text _value;
        private Image _icon;
        private float _target = 1f;
        private float _shown = 1f;
        private float _ghostShown = 1f;
        private float _ghostHold;

        public Image Fill => _fillImage;
        public Image Icon => _icon;

        public Color Color
        {
            get => _fillImage.color;
            set => _fillImage.color = value;
        }

        public static HudBar Create(UIFactory ui, Transform parent, Sprite track, Sprite fill, Color color,
            Sprite icon = null, int fontSize = 30)
        {
            RectTransform root = ui.Node("HudBar", parent);
            var bar = root.gameObject.AddComponent<HudBar>();

            Image trackImage = root.gameObject.AddComponent<Image>();
            trackImage.sprite = track;
            trackImage.type = track != null ? Image.Type.Sliced : Image.Type.Simple;
            trackImage.color = track != null ? Color.white : new Color(0.15f, 0.1f, 0.22f, 0.9f);
            trackImage.raycastTarget = false;

            RectTransform inner = ui.Node("Inner", root);
            UIFactory.Place(inner, 0f, 0f, 1f, 1f, 7f);

            bar._ghost = NewFill(ui, inner, fill, new Color(1f, 1f, 1f, 0.55f), "Ghost").rectTransform;
            bar._fillImage = NewFill(ui, inner, fill, color, "Fill");
            bar._fill = bar._fillImage.rectTransform;

            bar._value = ui.Label(root, "", fontSize, Color.white);
            UIFactory.Place(bar._value, 0.1f, 0f, 0.95f, 1f);
            bar._value.gameObject.SetActive(false);

            if (icon != null)
            {
                bar._icon = ui.Node("Icon", root).gameObject.AddComponent<Image>();
                bar._icon.sprite = icon;
                bar._icon.preserveAspect = true;
                bar._icon.raycastTarget = false;
                RectTransform rt = bar._icon.rectTransform;
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(4f, 0f);
                rt.sizeDelta = new Vector2(78f, 78f);
            }
            bar.Apply();
            return bar;
        }

        private static Image NewFill(UIFactory ui, Transform parent, Sprite sprite, Color color, string name)
        {
            Image img = ui.Node(name, parent).gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            img.color = color;
            img.raycastTarget = false;
            RectTransform rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return img;
        }

        /// <summary>Sets the target fraction (eases there) and, if given, the centred value text.</summary>
        public void Set(float value01, string text = null)
        {
            value01 = Mathf.Clamp01(value01);
            if (value01 < _target) _ghostHold = 0.35f; // a drop: keep the ghost briefly, then chase
            _target = value01;
            if (text != null)
            {
                _value.gameObject.SetActive(true);
                _value.text = text;
            }
        }

        /// <summary>Jumps straight to <paramref name="value01"/> (new run, bar just shown).</summary>
        public void Snap(float value01)
        {
            _target = _shown = _ghostShown = Mathf.Clamp01(value01);
            Apply();
        }

        public float Value => _target;

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _shown = Mathf.MoveTowards(_shown, _target, dt * Mathf.Max(0.8f, Mathf.Abs(_target - _shown) * 6f));
            if (_ghostShown < _shown) _ghostShown = _shown;
            else if ((_ghostHold -= dt) <= 0f) _ghostShown = Mathf.MoveTowards(_ghostShown, _shown, dt * 0.9f);
            Apply();
        }

        private void Apply()
        {
            SetWidth(_fill, _shown);
            SetWidth(_ghost, _ghostShown);
        }

        private static void SetWidth(RectTransform rt, float v)
        {
            // Hide below a sliver: the sliced pill cannot draw narrower than its rounded ends.
            rt.gameObject.SetActive(v > 0.02f);
            rt.anchorMax = new Vector2(v, 1f);
        }
    }
}
