using PofudukFilo.Core;
using PofudukFilo.Meta;
using PofudukFilo.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace PofudukFilo.UI
{
    /// <summary>
    /// Main menu (design/ux/main-menu.md). Device feedback 2026-09-24 called the flat version
    /// "çok basit, profesyonel değil", so the menu is dressed like a store-quality mobile title:
    /// - a candy logo (gradient letters, thick outline, bobbing wave, pink ribbon, twinkling stars);
    /// - the hero ship floating over a glowing pedestal in front of slowly turning light rays;
    /// - currency capsules and a gear button at the top;
    /// - a glossy PLAY button with a play icon and a shine sweep;
    /// - a docked bottom tab bar (Ar-Ge / Silahlar / Pilotlar) with drawn icons and "!" badges.
    /// </summary>
    public sealed partial class GameUI
    {
        [Header("Menu art")]
        [SerializeField] private Sprite researchIcon;
        [SerializeField] private Sprite weaponsIcon;
        [SerializeField] private Sprite gearIcon;
        [SerializeField] private Sprite playIcon;
        [SerializeField] private Sprite pauseIcon;
        [SerializeField] private Sprite trophyIcon;
        [SerializeField] private Sprite pedestalSprite;
        [SerializeField] private Sprite raysSprite;
        [SerializeField] private Sprite capsuleSprite;
        [SerializeField] private Sprite navBarSprite;
        [SerializeField] private Sprite ribbonSprite;
        [SerializeField] private Sprite shineSprite;
        [SerializeField] private Sprite vignetteSprite;

        private sealed class Tile
        {
            public Button Button;
            public Image Icon;
            public GameObject Badge;
        }

        private Image _heroShip;
        private Text _titleTop;
        private Text _titleBottom;
        private RectTransform _rays;
        private RectTransform _shine;
        private Image[] _twinkles;
        private Text _recordText;
        private Image _recordIcon;
        private GameObject _recordCapsule;
        private Button _restartButton;
        private Text _endDust;
        private GameObject _endDustIcon;
        private GameObject _endDustGap;
        private Tile _researchTile;
        private Tile _armoryTile;
        private Tile _pilotsTile;

        private void BuildMenu(Transform root)
        {
            _menu = MakeScreen(root, GameState.MainMenu, dim: false);
            Transform m = _menu.transform;

            // Edge vignettes frame the UI against the busy starfield.
            Vignette(m, 0.8f, 1f, false);
            Vignette(m, 0f, 0.3f, true);

            // --- Top: currency capsules + gear.
            _wallet = new WalletView
            {
                Gold = CurrencyCapsule(m, coinIcon, 0.03f, 0.36f, Palette.Honey),
                Dust = CurrencyCapsule(m, stardustIcon, 0.39f, 0.66f, Palette.Hex(0xDCCFFF))
            };
            Button gear = IconButton(m, gearIcon, Palette.Lavender, OpenSettings);
            UIFactory.Place(gear, 0.84f, 0.925f, 0.97f, 0.985f);

            // --- Logo: ribbon behind line 2, gradient + wave letters, twinkles.
            Image ribbon = Img(m, ribbonSprite, "Ribbon");
            UIFactory.Place(ribbon, 0.08f, 0.745f, 0.92f, 0.815f);
            _titleTop = LogoLine(m, "POFUDUK", 140, Palette.Hex(0xFFD1E6), Palette.HotPink, 0.815f, 0.9f);
            _titleBottom = LogoLine(m, "FİLO", 124, Palette.Hex(0xFFF6C8), Palette.Honey, 0.742f, 0.822f);
            _twinkles = new Image[5];
            Vector2[] spots = { new(0.1f, 0.9f), new(0.9f, 0.88f), new(0.16f, 0.76f), new(0.86f, 0.74f), new(0.5f, 0.915f) };
            for (int i = 0; i < _twinkles.Length; i++)
            {
                _twinkles[i] = Img(m, stardustIcon, "Twinkle");
                Vector2 c = spots[i];
                UIFactory.Place(_twinkles[i], c.x - 0.035f, c.y - 0.017f, c.x + 0.035f, c.y + 0.017f);
            }

            // --- Hero: rays, pedestal, ship, pilot name capsule.
            Image rays = Img(m, raysSprite, "Rays");
            UIFactory.Place(rays, 0.02f, 0.44f, 0.98f, 0.74f);
            rays.preserveAspect = true;
            _rays = rays.rectTransform;
            Image pedestal = Img(m, pedestalSprite, "Pedestal");
            UIFactory.Place(pedestal, 0.18f, 0.475f, 0.82f, 0.54f);
            _heroShip = Img(m, null, "HeroShip");
            UIFactory.Place(_heroShip, 0.28f, 0.52f, 0.72f, 0.72f);
            Image pilotCap = Capsule(m, 0.3f, 0.47f, 0.7f, 0.5f);
            _pilotText = _ui.Label(pilotCap.transform, "", 36, Palette.Pink);
            UIFactory.Place(_pilotText, 0.05f, 0f, 0.95f, 1f);

            // --- Record / saved-run capsule with a trophy.
            Image recordCap = Capsule(m, 0.22f, 0.412f, 0.78f, 0.45f);
            _recordCapsule = recordCap.gameObject;
            RectTransform recordRow = CurrencyRow(recordCap.transform, TextAnchor.MiddleCenter);
            UIFactory.Place(recordRow, 0.04f, 0.08f, 0.96f, 0.92f);
            _recordIcon = _ui.Node("Trophy", recordRow).gameObject.AddComponent<Image>();
            _recordIcon.sprite = trophyIcon;
            _recordIcon.preserveAspect = true;
            var le = _recordIcon.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.preferredHeight = 48f;
            _recordText = _ui.Label(recordRow, "", 38, Palette.Honey);
            _recordText.horizontalOverflow = HorizontalWrapMode.Overflow;

            // --- PLAY: glossy, play icon, shine sweep, gradient label.
            _playButton = _ui.Button(m, "OYNA", Palette.HotPink, () =>
            {
                if (run.HasSavedRun) run.ResumeRun();
                else run.StartEndless();
            }, 112);
            UIFactory.Place(_playButton, 0.1f, 0.29f, 0.9f, 0.4f);
            Transform playFace = _playButton.transform.Find("Face");
            playFace.gameObject.AddComponent<RectMask2D>();
            Image play = Img(playFace, playIcon, "PlayIcon");
            UIFactory.Place(play, 0.07f, 0.2f, 0.22f, 0.8f);
            Text playText = _playButton.GetComponentInChildren<Text>();
            UIFactory.Place(playText, 0.2f, 0f, 0.98f, 1f);
            Image shine = Img(playFace, shineSprite, "Shine");
            _shine = shine.rectTransform;
            _shine.anchorMin = new Vector2(0f, -0.2f);
            _shine.anchorMax = new Vector2(0f, 1.2f);
            _shine.sizeDelta = new Vector2(120f, 0f);

            _restartButton = _ui.Button(m, "Yeni Oyun", Palette.Lavender, run.StartEndless, 32);
            UIFactory.Place(_restartButton, 0.32f, 0.232f, 0.68f, 0.278f);

            // --- Bottom tab bar.
            Image nav = Img(m, navBarSprite, "NavBar");
            nav.type = navBarSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            nav.raycastTarget = true;
            UIFactory.Place(nav, -0.01f, -0.01f, 1.01f, 0.175f);
            _researchTile = NavTab(nav.transform, "AR-GE", researchIcon, 0f, OpenResearch);
            _armoryTile = NavTab(nav.transform, "SİLAHLAR", weaponsIcon, 1f / 3f, OpenLab);
            _pilotsTile = NavTab(nav.transform, "PİLOTLAR", null, 2f / 3f, OpenHangar);

            Text credit = _ui.Label(m, $"© {StudioIntro.StudioName}", 22, new Color(0.78f, 0.71f, 1f, 0.55f));
            UIFactory.Place(credit, 0.1f, 0.004f, 0.9f, 0.022f);
        }

        // ---------------------------------------------------------------- Pieces

        private Image Img(Transform parent, Sprite sprite, string name)
        {
            Image img = _ui.Node(name, parent).gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            if (sprite == null && name != "HeroShip") img.enabled = false;
            return img;
        }

        private void Vignette(Transform parent, float y0, float y1, bool flip)
        {
            Image v = Img(parent, vignetteSprite, "Vignette");
            v.preserveAspect = false;
            UIFactory.Place(v, 0f, y0, 1f, y1);
            if (flip) v.rectTransform.localScale = new Vector3(1f, -1f, 1f);
        }

        private Image Capsule(Transform parent, float x0, float y0, float x1, float y1)
        {
            Image cap = Img(parent, capsuleSprite, "Capsule");
            cap.enabled = true;
            cap.preserveAspect = false;
            cap.type = capsuleSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            if (capsuleSprite == null) cap.color = new Color(0.15f, 0.1f, 0.22f, 0.8f);
            UIFactory.Place(cap, x0, y0, x1, y1);
            return cap;
        }

        /// <summary>A capsule with a big currency icon overlapping its left end and the amount right of it.</summary>
        private Text CurrencyCapsule(Transform parent, Sprite icon, float x0, float x1, Color color)
        {
            Image cap = Capsule(parent, x0, 0.935f, x1, 0.975f);
            Text amount = _ui.Label(cap.transform, "0", 42, color);
            UIFactory.Place(amount, 0.3f, 0f, 0.95f, 1f);
            Image i = Img(cap.transform, icon, "Icon");
            RectTransform rt = i.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(96f, 96f);
            rt.anchoredPosition = new Vector2(30f, 0f);
            return amount;
        }

        private Button IconButton(Transform parent, Sprite icon, Color face, System.Action onClick)
        {
            Button b = _ui.Button(parent, "", face, onClick, 30);
            Image i = Img(b.transform.Find("Face"), icon, "Icon");
            UIFactory.Place(i, 0.14f, 0.14f, 0.86f, 0.86f);
            return b;
        }

        private Text LogoLine(Transform parent, string text, int size, Color top, Color bottom, float y0, float y1)
        {
            Text t = _ui.Label(parent, text, size, Color.white);
            UIFactory.Place(t, 0.02f, y0, 0.98f, y1);
            // Effect order matters: wave and gradient first, then the outlines copy the result.
            Destroy(t.GetComponent<Outline>()); // removed at frame end; the new effects keep their order
            t.gameObject.AddComponent<UIWave>().amplitude = size * 0.05f;
            UIGradient g = t.gameObject.AddComponent<UIGradient>();
            g.top = top;
            g.bottom = bottom;
            Outline thick = t.gameObject.AddComponent<Outline>();
            thick.effectColor = Palette.Outline;
            thick.effectDistance = new Vector2(7f, -7f);
            Outline thin = t.gameObject.AddComponent<Outline>();
            thin.effectColor = Palette.Outline;
            thin.effectDistance = new Vector2(-5f, 5f);
            Shadow drop = t.gameObject.AddComponent<Shadow>();
            drop.effectColor = new Color(0.08f, 0.03f, 0.16f, 0.7f);
            drop.effectDistance = new Vector2(0f, -16f);
            return t;
        }

        /// <summary>A tab in the bottom bar: big icon, label, "!" badge.</summary>
        private Tile NavTab(Transform bar, string label, Sprite icon, float x0, System.Action open)
        {
            var tile = new Tile();
            RectTransform node = _ui.Node(label, bar);
            UIFactory.Place(node, x0 + 0.01f, 0.08f, x0 + 1f / 3f - 0.01f, 0.95f);
            Image hit = node.gameObject.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
            tile.Button = node.gameObject.AddComponent<Button>();
            tile.Button.onClick.AddListener(() =>
            {
                Feel.Juice.PunchUI(node);
                if (Audio.AudioManager.Instance != null) Audio.AudioManager.Instance.Play(Audio.SfxId.Click, 0.03f);
                open();
            });

            tile.Icon = Img(node, icon, "Icon");
            tile.Icon.enabled = true;
            UIFactory.Place(tile.Icon, 0.2f, 0.3f, 0.8f, 1f);
            Text text = _ui.Label(node, label, 34, Palette.White);
            UIFactory.Place(text, 0f, 0f, 1f, 0.3f);

            Image badge = Img(node, null, "Badge");
            badge.enabled = true;
            badge.sprite = roundedSprite;
            badge.type = Image.Type.Sliced;
            badge.color = Palette.Coral;
            UIFactory.Place(badge, 0.68f, 0.72f, 0.86f, 0.97f);
            Text bang = _ui.Label(badge.transform, "!", 34, Color.white);
            bang.GetComponent<Outline>().effectColor = Palette.Outline;
            tile.Badge = badge.gameObject;
            return tile;
        }

        // ---------------------------------------------------------------- Refresh & animation

        private void RefreshForge()
        {
            if (_researchTile == null) return;
            MetaProgressionService meta = run.Meta;
            _researchTile.Badge.SetActive(ResearchAffordable());
            _armoryTile.Badge.SetActive(ArmoryAffordable());
            _pilotsTile.Badge.SetActive(false);

            float best = meta.BestEndlessSeconds;
            bool saved = run.HasSavedRun;
            string line = "";
            if (saved)
            {
                (int stage, float minutes) = run.SavedRunInfo();
                line = Loc.T($"Kayıt: Bölüm {stage} · {Mathf.FloorToInt(minutes)}:{Mathf.FloorToInt(minutes * 60f % 60f):00}");
            }
            else if (best > 0f)
                line = Loc.T($"Rekor: {Mathf.FloorToInt(best / 60f)}:{Mathf.FloorToInt(best % 60f):00}");
            _recordText.text = line;
            _recordIcon.gameObject.SetActive(!saved);
            _recordCapsule.SetActive(line.Length > 0);
            UIFactory.SetText(_playButton, saved ? "DEVAM ET" : "OYNA");
            _restartButton.gameObject.SetActive(saved);
        }

        private bool ArmoryAffordable()
        {
            MetaProgressionService meta = run.Meta;
            foreach (WeaponDefinition w in run.LabWeapons)
                if (w != null && (meta.IsUnlocked(w) ? meta.CanUpgradeMastery(w) : meta.Gold >= w.labCost)) return true;
            return false;
        }

        private void UpdateMenuAnim()
        {
            if (_menu == null || !_menu.activeSelf || _heroShip == null) return;
            float t = Time.unscaledTime;
            _heroShip.rectTransform.anchoredPosition = new Vector2(0f, 10f + Mathf.Sin(t * 1.8f) * 16f);
            _heroShip.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.1f) * 3f);
            if (_rays != null) _rays.localRotation = Quaternion.Euler(0f, 0f, t * 8f);
            _playButton.transform.localScale = Vector3.one * (1f + Mathf.Max(0f, Mathf.Sin(t * 3f)) * 0.035f);

            if (_shine != null)
            {
                // A sweep every 2.6 s: crosses the button in the first 0.7 s, then waits off-screen.
                float cycle = Mathf.Repeat(t, 2.6f) / 0.7f;
                float x = Mathf.Lerp(-0.15f, 1.15f, Mathf.Clamp01(cycle));
                _shine.anchorMin = new Vector2(x, -0.2f);
                _shine.anchorMax = new Vector2(x, 1.2f);
            }

            if (_twinkles != null)
                for (int i = 0; i < _twinkles.Length; i++)
                {
                    float k = 0.5f + 0.5f * Mathf.Sin(t * 2.2f + i * 1.7f);
                    _twinkles[i].rectTransform.localScale = Vector3.one * (0.6f + 0.5f * k);
                    _twinkles[i].color = new Color(1f, 1f, 1f, 0.35f + 0.65f * k);
                }
        }
    }
}
