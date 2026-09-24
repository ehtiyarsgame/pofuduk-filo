using System;
using PofudukFilo.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PofudukFilo.UI
{
    /// <summary>
    /// Currency shown as icons, never words (device feedback 2026-09-24: "altın / Yıldız Tozu yazısı hoş
    /// durmuyor"): a coin for gold and a lilac star for Stardust, in the wallet, on every price button and
    /// on the run-end reward line.
    /// </summary>
    public sealed partial class GameUI
    {
        [SerializeField] private Sprite stardustIcon;

        private sealed class WalletView
        {
            public Text Gold;
            public Text Dust;
        }

        /// <summary>A "[coin] 2370   [star] 3" row inside the given anchor box.</summary>
        private WalletView MakeWallet(Transform parent, float x0, float y0, float x1, float y1, int size,
            TextAnchor align = TextAnchor.MiddleCenter)
        {
            RectTransform row = CurrencyRow(parent, align);
            UIFactory.Place(row, x0, y0, x1, y1);
            var w = new WalletView
            {
                Gold = AddAmount(row, coinIcon, "0", size, Palette.Honey)
            };
            RectTransform gap = _ui.Node("Gap", row);
            gap.gameObject.AddComponent<LayoutElement>().preferredWidth = size * 0.8f;
            w.Dust = AddAmount(row, stardustIcon, "0", size, Palette.Hex(0xC8B6FF));
            return w;
        }

        private void SetWallet(WalletView w)
        {
            if (w == null) return;
            w.Gold.text = run.Meta.Gold.ToString();
            w.Dust.text = run.Meta.Stardust.ToString();
        }

        private RectTransform CurrencyRow(Transform parent, TextAnchor align)
        {
            RectTransform row = _ui.Node("Currency", parent);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = align;
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return row;
        }

        /// <summary>Icon + number, laid out left to right in a <see cref="CurrencyRow"/>.</summary>
        private Text AddAmount(Transform row, Sprite icon, string amount, int size, Color color)
        {
            if (icon != null)
            {
                Image img = _ui.Node("Icon", row).gameObject.AddComponent<Image>();
                img.sprite = icon;
                img.preserveAspect = true;
                img.raycastTarget = false;
                var le = img.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = le.preferredHeight = size * 1.15f;
            }
            Text t = _ui.Label(row, amount, size, color);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>
        /// A button that shows its price as icons: an optional word on top ("Geliştir"), and "[coin] 230"
        /// (and/or "[star] 5") below. Zero amounts are left out.
        /// </summary>
        private Button PriceButton(Transform parent, string label, int gold, int dust, Color face, Action onClick, int fontSize)
        {
            Button b = _ui.Button(parent, label, face, onClick, fontSize);
            if (gold <= 0 && dust <= 0) return b; // nothing to price (MAKS / free): keep the label centred
            Transform faceNode = b.transform.Find("Face");
            Text labelText = b.GetComponentInChildren<Text>();
            bool hasLabel = !string.IsNullOrEmpty(label);
            if (hasLabel) UIFactory.Place(labelText, 0.02f, 0.5f, 0.98f, 0.98f);

            RectTransform row = CurrencyRow(faceNode, TextAnchor.MiddleCenter);
            UIFactory.Place(row, 0.04f, hasLabel ? 0.06f : 0.1f, 0.96f, hasLabel ? 0.52f : 0.9f);
            if (gold > 0) AddAmount(row, coinIcon, gold.ToString(), fontSize, Palette.White);
            if (gold > 0 && dust > 0)
            {
                RectTransform gap = _ui.Node("Gap", row);
                gap.gameObject.AddComponent<LayoutElement>().preferredWidth = fontSize * 0.4f;
            }
            if (dust > 0) AddAmount(row, stardustIcon, dust.ToString(), fontSize, Palette.White);
            return b;
        }
    }
}
