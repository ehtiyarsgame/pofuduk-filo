using System.Collections;
using PofudukFilo.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PofudukFilo.UI
{
    /// <summary>
    /// "Ehtiyars Game" studio intro (design/gdd/brand.md): the emblem pops in, the studio name and "sunar"
    /// rise under it, then everything fades to reveal the menu. Plays once per app launch (a language
    /// switch reloads the scene and must not replay it); a tap skips to the fade. Unscaled time, so the
    /// menu's paused time scale does not stall it.
    /// </summary>
    public sealed class StudioIntro : MonoBehaviour
    {
        public const string StudioName = "EHTIYARS GAME";

        [SerializeField] private Sprite emblem;
        [SerializeField] private Font font;
        [SerializeField] private float holdSeconds = 1.3f;

        private static bool s_played;

        private CanvasGroup _root;
        private CanvasGroup _content;
        private RectTransform _emblem;
        private Text _title;
        private Text _subtitle;
        private bool _skip;

        /// <summary>True while the intro covers the screen (QA waits on it).</summary>
        public static bool Playing { get; private set; }

        private void Start()
        {
            if (s_played)
            {
                enabled = false;
                return;
            }
            s_played = true;
            Playing = true;
            Build();
            StartCoroutine(Play());
        }

        private void Update()
        {
            if (!Playing) return;
            Pointer pointer = Pointer.current;
            if (pointer != null && pointer.press.wasPressedThisFrame) _skip = true;
        }

        private void Build()
        {
            var canvasGo = new GameObject("StudioIntro", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // above every game canvas
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 2340f);
            scaler.matchWidthOrHeight = 0.5f;
            _root = canvasGo.GetComponent<CanvasGroup>();

            // Full-screen plum backdrop; it also swallows taps meant for the menu underneath.
            Image bg = NewImage("Backdrop", canvasGo.transform, null, new Color(0.13f, 0.09f, 0.22f, 1f));
            Stretch(bg.rectTransform);

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(CanvasGroup));
            contentGo.transform.SetParent(canvasGo.transform, false);
            Stretch((RectTransform)contentGo.transform);
            _content = contentGo.GetComponent<CanvasGroup>();

            Image e = NewImage("Emblem", contentGo.transform, emblem, Color.white);
            e.preserveAspect = true;
            _emblem = e.rectTransform;
            _emblem.anchorMin = _emblem.anchorMax = new Vector2(0.5f, 0.56f);
            _emblem.sizeDelta = new Vector2(620f, 620f);

            _title = NewText("Studio", contentGo.transform, StudioName, 96, new Color(1f, 0.78f, 0.29f));
            _title.rectTransform.anchorMin = _title.rectTransform.anchorMax = new Vector2(0.5f, 0.38f);
            _subtitle = NewText("Presents", contentGo.transform, Loc.T("sunar"), 52, new Color(0.78f, 0.71f, 1f));
            _subtitle.rectTransform.anchorMin = _subtitle.rectTransform.anchorMax = new Vector2(0.5f, 0.335f);
        }

        private IEnumerator Play()
        {
            float t = 0f;
            const float popIn = 0.5f, titleIn = 0.45f;
            bool chimed = false;
            while (t < popIn + titleIn + holdSeconds && !_skip)
            {
                t += Time.unscaledDeltaTime;
                // Emblem: overshoot pop (ease-out-back) with a fade.
                float p = Mathf.Clamp01(t / popIn);
                float back = 1f + 2.2f * Mathf.Pow(p - 1f, 3f) + 1.2f * Mathf.Pow(p - 1f, 2f);
                _emblem.localScale = Vector3.one * Mathf.LerpUnclamped(0.55f, 1f, back);
                _emblem.GetComponent<Image>().color = new Color(1f, 1f, 1f, p);
                _emblem.anchoredPosition = new Vector2(0f, Mathf.Sin(t * 2.2f) * 8f);
                if (!chimed && p > 0.6f)
                {
                    chimed = true;
                    if (Audio.AudioManager.Instance != null) Audio.AudioManager.Instance.Play(Audio.SfxId.LevelUp);
                }

                // Name, then "sunar", rise and fade in.
                float q = Mathf.Clamp01((t - popIn * 0.7f) / titleIn);
                SetRise(_title, q);
                SetRise(_subtitle, Mathf.Clamp01((t - popIn * 0.7f - 0.25f) / titleIn));
                yield return null;
            }

            SetRise(_title, 1f);
            SetRise(_subtitle, 1f);
            yield return Fade(_content, 0.35f);
            yield return Fade(_root, 0.4f);
            Playing = false;
            Destroy(_root.gameObject);
        }

        private static void SetRise(Text text, float k)
        {
            float e = 1f - (1f - k) * (1f - k);
            Color c = text.color;
            c.a = e;
            text.color = c;
            text.rectTransform.anchoredPosition = new Vector2(0f, (1f - e) * -40f);
        }

        private static IEnumerator Fade(CanvasGroup group, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                group.alpha = 1f - t / seconds;
                yield return null;
            }
            group.alpha = 0f;
        }

        private static Image NewImage(string name, Transform parent, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            return img;
        }

        private Text NewText(string name, Transform parent, string value, int size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.color = new Color(color.r, color.g, color.b, 0f);
            text.raycastTarget = false;
            text.rectTransform.sizeDelta = new Vector2(1000f, size * 1.5f);
            var outline = go.GetComponent<Outline>();
            outline.effectColor = new Color(0.1f, 0.05f, 0.18f, 0.8f);
            outline.effectDistance = new Vector2(4f, -4f);
            return text;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
