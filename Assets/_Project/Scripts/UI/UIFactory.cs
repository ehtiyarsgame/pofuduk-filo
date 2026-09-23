using System;
using PofudukFilo.Feel;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace PofudukFilo.UI
{
    /// <summary>Palette from design/art-bible.md §2.1.</summary>
    public static class Palette
    {
        public static readonly Color Outline = Hex(0x3B2A4F);
        public static readonly Color Lavender = Hex(0x6B5B95);
        public static readonly Color Pink = Hex(0xF7A6C1);
        public static readonly Color HotPink = Hex(0xFF4F9A);
        public static readonly Color Mint = Hex(0x7FE0C4);
        public static readonly Color Cream = Hex(0xFFE8A3);
        public static readonly Color Honey = Hex(0xFFC84A);
        public static readonly Color Coral = Hex(0xFF6B5E);
        public static readonly Color Sky = Hex(0x6EC6FF);
        public static readonly Color White = Color.white;
        public static readonly Color Dim = new(0.23f, 0.16f, 0.31f, 0.72f);

        /// <summary>Card frame by rarity: Common white, Rare blue, Epic purple, Legendary gold.</summary>
        public static readonly Color[] Rarity = { Hex(0xFFFFFF), Hex(0x6EC6FF), Hex(0xB58CFF), Hex(0xFFC84A) };

        public static Color Hex(int rgb) =>
            new(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
    }

    /// <summary>
    /// Builds "jelly UI" (art-bible §4) from code: rounded panels, thick-bottomed buttons that punch
    /// on press, rounded bold text with an outline. No prefabs needed.
    /// </summary>
    public sealed class UIFactory
    {
        private readonly Font _font;
        private readonly Sprite _rounded;

        public UIFactory(Font font, Sprite rounded)
        {
            _font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _rounded = rounded;
        }

        public static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            UnityEngine.Object.DontDestroyOnLoad(go);
        }

        public Canvas Canvas(string name, int sortOrder)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 2400f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        /// <summary>Full-stretch child that respects the device safe area (notches, home bar).</summary>
        public RectTransform SafeArea(Transform parent)
        {
            RectTransform rt = Node("SafeArea", parent);
            Rect safe = Screen.safeArea;
            rt.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            rt.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        public RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            Stretch(rt);
            return rt;
        }

        public Image Panel(Transform parent, Color color, string name = "Panel")
        {
            RectTransform rt = Node(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = _rounded;
            img.type = _rounded != null ? Image.Type.Sliced : Image.Type.Simple;
            img.color = color;
            return img;
        }

        /// <summary>Full-screen dimmer that also blocks touches to the game underneath.</summary>
        public Image Dimmer(Transform parent)
        {
            RectTransform rt = Node("Dimmer", parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = Palette.Dim;
            return img;
        }

        public Text Label(Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter)
        {
            RectTransform rt = Node("Label", parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = _font;
            t.text = text;
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;

            var outline = rt.gameObject.AddComponent<Outline>();
            outline.effectColor = Palette.Outline;
            outline.effectDistance = new Vector2(3f, -3f);
            return t;
        }

        /// <summary>
        /// Candy-tablet button: a darker "thickness" under the face; pressing punches the scale.
        /// Min touch target 48 dp is guaranteed by the caller's layout (art-bible §4).
        /// </summary>
        public Button Button(Transform parent, string text, Color face, Action onClick, int fontSize = 64)
        {
            RectTransform root = Node("Button", parent);

            Image shadow = Panel(root, Color.Lerp(face, Palette.Outline, 0.45f), "Thickness");
            shadow.rectTransform.offsetMin = new Vector2(0f, -12f);
            shadow.rectTransform.offsetMax = new Vector2(0f, -12f);
            shadow.raycastTarget = false;

            Image faceImg = Panel(root, face, "Face");
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = faceImg;
            ColorBlock colors = button.colors;
            colors.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.65f, 0.7f);
            button.colors = colors;

            Label(faceImg.transform, text, fontSize, Palette.White);
            button.onClick.AddListener(() =>
            {
                Juice.PunchUI(root);
                onClick?.Invoke();
            });
            return button;
        }

        public static void SetText(Button button, string text)
        {
            Text t = button.GetComponentInChildren<Text>();
            if (t != null) t.text = text;
        }

        // ---------------------------------------------------------------- Layout helpers

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        /// <summary>Anchor box in normalised parent space (0..1), with optional pixel padding.</summary>
        public static RectTransform Place(Component c, float xMin, float yMin, float xMax, float yMax, float pad = 0f)
        {
            var rt = (RectTransform)c.transform;
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = new Vector2(pad, pad);
            rt.offsetMax = new Vector2(-pad, -pad);
            return rt;
        }

        /// <summary>Horizontal fill bar (XP, boss HP). Returns the fill image; set fillAmount 0..1.</summary>
        public Image Bar(Transform parent, Color back, Color fill)
        {
            Image bg = Panel(parent, back, "Bar");
            Image fg = Panel(bg.transform, fill, "Fill");
            fg.type = Image.Type.Filled;
            fg.fillMethod = Image.FillMethod.Horizontal;
            fg.fillAmount = 0f;
            Place(fg, 0f, 0f, 1f, 1f, 4f);
            return fg;
        }
    }
}
