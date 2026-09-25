using PofudukFilo.Core;
using PofudukFilo.Meta;
using PofudukFilo.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace PofudukFilo.UI
{
    /// <summary>
    /// Main menu v4 — a market-style lobby (design/ux/main-menu.md §3). Device feedback 2026-09-25: "menü çok
    /// basit, oynayası gelmez". Built on the pattern the top mobile shooters and survivor-likes share:
    /// - top bar: pilot avatar with the power tag, currency capsules with an ad "+", gear;
    /// - an illustrated stage card with the hero ship and the best time;
    /// - a chest track under it — one-time rewards for record times, always showing the next goal;
    /// - side event icons (gift, stars / next pilot, login streak) with timers and badges;
    /// - a big golden pulsing PLAY;
    /// - a 5-tab bottom bar with a raised centre Home tab.
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
        [SerializeField] private Sprite pawSprite;
        [SerializeField] private Sprite shineSprite;
        [SerializeField] private Sprite vignetteSprite;
        [SerializeField] private Sprite stageCardSprite;
        [SerializeField] private Sprite chestClosedSprite;
        [SerializeField] private Sprite chestOpenSprite;
        [SerializeField] private Sprite homeBubbleSprite;
        [SerializeField] private Sprite homeIcon;

        private sealed class Tile
        {
            public Button Button;
            public Image Icon;
            public Text Label;
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
        private Tile _missionsTile;
        private Tile _homeTile;
        private Image _avatar;
        private Tile _giftTile;
        private Tile _starsTile;
        private Tile _nextPilotTile;
        private Tile _streakTile;
        private Image _chestFill;
        private Image[] _chests;
        private Text _chestHint;

        private void BuildMenu(Transform root)
        {
            _menu = MakeScreen(root, GameState.MainMenu, dim: false);
            Transform m = _menu.transform;

            // Edge vignettes frame the UI against the busy starfield.
            Vignette(m, 0.8f, 1f, false);
            Vignette(m, 0f, 0.3f, true);

            // --- Top bar: avatar (tap → Pilotlar) with the power tag, wallet with an ad "+", gear.
            Button avatar = IconButton(m, null, Palette.Hex(0x5B3C99), OpenHangar);
            UIFactory.Place(avatar, 0.02f, 0.935f, 0.15f, 0.99f);
            _avatar = avatar.transform.Find("Face/Icon").GetComponent<Image>();
            _avatar.enabled = true;
            Image powerCap = Capsule(m, 0f, 0.905f, 0.2f, 0.932f);
            _powerText = _ui.Label(powerCap.transform, "", 22, Palette.Honey);
            UIFactory.Place(_powerText, 0.04f, 0f, 0.96f, 1f);
            _wallet = new WalletView
            {
                Gold = CurrencyCapsule(m, coinIcon, 0.21f, 0.48f, Palette.Honey),
                Dust = CurrencyCapsule(m, stardustIcon, 0.52f, 0.72f, Palette.Hex(0xDCCFFF))
            };
            // Same "[TV] +" as the meta screens: an ad pays gold and Stardust (ad-rewards.md, the gift).
            Button more = AdButton(m, "", "+", Palette.Mint, ClaimGift, 30);
            UIFactory.Place(more, 0.735f, 0.938f, 0.845f, 0.972f);
            _walletAdButtons.Add(more);
            Button gear = IconButton(m, gearIcon, Palette.Lavender, OpenSettings);
            UIFactory.Place(gear, 0.86f, 0.93f, 0.98f, 0.985f);

            // --- Logo, smaller than v3: the stage card is the hero of the screen now.
            Image ribbon = Img(m, ribbonSprite, "Ribbon");
            UIFactory.Place(ribbon, 0.16f, 0.816f, 0.84f, 0.868f);
            _titleTop = LogoLine(m, "GALAXY", 100, Palette.Hex(0xE6D9FF), Palette.Hex(0x9C7BFF), 0.862f, 0.91f);
            _titleBottom = LogoLine(m, "PAWS", 90, Palette.Hex(0xFFF6C8), Palette.Honey, 0.818f, 0.864f);
            _twinkles = new Image[4];
            Vector2[] spots = { new(0.24f, 0.9f), new(0.78f, 0.89f), new(0.2f, 0.83f), new(0.8f, 0.826f) };
            for (int i = 0; i < _twinkles.Length; i++)
            {
                _twinkles[i] = Img(m, i >= 2 ? pawSprite ?? stardustIcon : stardustIcon, "Twinkle");
                Vector2 c = spots[i];
                UIFactory.Place(_twinkles[i], c.x - 0.03f, c.y - 0.014f, c.x + 0.03f, c.y + 0.014f);
            }

            // --- Stage card: rays peek around it, the ship floats on a pedestal inside, best time at its foot.
            Image rays = Img(m, raysSprite, "Rays");
            UIFactory.Place(rays, 0f, 0.5f, 1f, 0.82f);
            rays.color = new Color(1f, 1f, 1f, 0.55f);
            _rays = rays.rectTransform;
            Image card = Img(m, stageCardSprite, "StageCard");
            card.enabled = true;
            if (stageCardSprite == null) card.color = Palette.Hex(0x3E2C63);
            UIFactory.Place(card, 0.17f, 0.54f, 0.83f, 0.785f);
            Image titleCap = Capsule(m, 0.24f, 0.768f, 0.76f, 0.8f);
            titleCap.color = Palette.Hex(0xFF7FB0);
            Text title = _ui.Label(titleCap.transform, "SONSUZ GALAKSİ", 34, Palette.White);
            UIFactory.Place(title, 0.04f, 0f, 0.96f, 1f);
            Image pedestal = Img(m, pedestalSprite, "Pedestal");
            UIFactory.Place(pedestal, 0.28f, 0.585f, 0.72f, 0.625f);
            _heroShip = Img(m, null, "HeroShip");
            UIFactory.Place(_heroShip, 0.34f, 0.6f, 0.66f, 0.755f);

            Image recordCap = Capsule(m, 0.28f, 0.548f, 0.72f, 0.58f);
            _recordCapsule = recordCap.gameObject;
            RectTransform recordRow = CurrencyRow(recordCap.transform, TextAnchor.MiddleCenter);
            UIFactory.Place(recordRow, 0.04f, 0.08f, 0.96f, 0.92f);
            _recordIcon = _ui.Node("Trophy", recordRow).gameObject.AddComponent<Image>();
            _recordIcon.sprite = trophyIcon;
            _recordIcon.preserveAspect = true;
            var le = _recordIcon.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.preferredHeight = 40f;
            _recordText = _ui.Label(recordRow, "", 30, Palette.Honey);
            _recordText.horizontalOverflow = HorizontalWrapMode.Overflow;

            // --- Side event icons: left = gift + stars, right = next pilot + login streak.
            _giftTile = SideIcon(m, giftIcon, 0.02f, 0.69f, ClaimGift);
            _giftButton = _giftTile.Button;
            _giftText = _giftTile.Label;
            _starsTile = SideIcon(m, stardustIcon, 0.02f, 0.585f, OpenConstellation);
            _starsTile.Label.text = Loc.T("YILDIZLAR");
            _nextPilotTile = SideIcon(m, null, 0.85f, 0.69f, OpenHangar);
            _streakTile = SideIcon(m, pawSprite, 0.85f, 0.585f, () => OpenMeta(_missions));

            // --- Chest track: record-time rewards, the next goal always in sight.
            BuildChestTrack(m);

            // --- PLAY: golden, glossy, play icon, shine sweep.
            _playButton = _ui.Button(m, "OYNA", Palette.Honey, () =>
            {
                if (run.HasSavedRun) run.ResumeRun();
                else run.StartEndless();
            }, 112);
            UIFactory.Place(_playButton, 0.1f, 0.28f, 0.9f, 0.39f);
            Transform playFace = _playButton.transform.Find("Face");
            playFace.gameObject.AddComponent<RectMask2D>();
            Image play = Img(playFace, playIcon, "PlayIcon");
            UIFactory.Place(play, 0.14f, 0.22f, 0.28f, 0.78f);
            Text playText = _playButton.GetComponentInChildren<Text>();
            // Right of the icon, shrinking to fit: "DEVAM ET" / "CONTINUE" are wider than "OYNA".
            UIFactory.Place(playText, 0.3f, 0.08f, 0.94f, 0.92f);
            playText.resizeTextForBestFit = true;
            playText.resizeTextMinSize = 48;
            playText.resizeTextMaxSize = 112;
            playText.horizontalOverflow = HorizontalWrapMode.Wrap;
            Image shine = Img(playFace, shineSprite, "Shine");
            _shine = shine.rectTransform;
            _shine.anchorMin = new Vector2(0f, -0.2f);
            _shine.anchorMax = new Vector2(0f, 1.2f);
            _shine.sizeDelta = new Vector2(120f, 0f);

            _restartButton = _ui.Button(m, "Yeni Oyun", Palette.Lavender, run.StartEndless, 32);
            UIFactory.Place(_restartButton, 0.3f, 0.222f, 0.7f, 0.262f);

            // --- Bottom bar: five tabs, Home raised in the middle.
            Image nav = Img(m, navBarSprite, "NavBar");
            nav.type = navBarSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            nav.raycastTarget = true;
            UIFactory.Place(nav, -0.01f, -0.01f, 1.01f, 0.165f);
            _researchTile = NavTab(nav.transform, "AR-GE", researchIcon, 0f, OpenResearch, 0.2f);
            _armoryTile = NavTab(nav.transform, "SİLAHLAR", weaponsIcon, 0.2f, OpenLab, 0.2f);
            _homeTile = NavTab(nav.transform, "ANA SAYFA", homeIcon, 0.4f, () => { }, 0.2f);
            RaiseHomeTab(_homeTile);
            _pilotsTile = NavTab(nav.transform, "PİLOTLAR", null, 0.6f, OpenHangar, 0.2f);
            _missionsTile = NavTab(nav.transform, "GÖREVLER", trophyIcon, 0.8f, () => OpenMeta(_missions), 0.2f);

            BuildRewards(m, root);
            BuildMissions(m, root);
        }

        private void BuildChestTrack(Transform m)
        {
            Image start = Img(m, trophyIcon, "TrackStart");
            UIFactory.Place(start, 0.02f, 0.448f, 0.1f, 0.49f);
            Image track = Capsule(m, 0.1f, 0.461f, 0.9f, 0.477f);
            track.color = new Color(0.1f, 0.06f, 0.18f, 0.9f);
            _chestFill = _ui.Panel(track.transform, Palette.Honey, "Fill");
            _chestFill.raycastTarget = false;
            UIFactory.Place(_chestFill, 0f, 0f, 0f, 1f);

            _chests = new Image[RecordChests.Count];
            for (int i = 0; i < _chests.Length; i++)
            {
                float x = 0.1f + 0.8f * (i + 1) / _chests.Length;
                Image chest = Img(m, chestClosedSprite, "Chest");
                chest.enabled = true;
                chest.raycastTarget = true;
                UIFactory.Place(chest, x - 0.05f, 0.44f, x + 0.05f, 0.5f);
                int index = i;
                chest.gameObject.AddComponent<Button>().onClick.AddListener(() => TapChest(index));
                _chests[i] = chest;
                Text mark = _ui.Label(m, Clock(RecordChests.Seconds[i]), 24, Palette.Cream);
                UIFactory.Place(mark, x - 0.06f, 0.419f, x + 0.06f, 0.442f);
            }
            _chestHint = _ui.Label(m, "", 26, Palette.Hex(0xDCCFFF));
            UIFactory.Place(_chestHint, 0.05f, 0.5f, 0.95f, 0.528f);
        }

        private void TapChest(int index)
        {
            MetaProgressionService meta = run.Meta;
            int gold = RecordChests.Gold[index], dust = RecordChests.Dust[index];
            if (meta.ClaimRecordChest(index))
            {
                Feel.Juice.PunchUI(_chests[index].rectTransform);
                MenuToast(dust > 0 ? Loc.T($"Sandık: +{gold} altın, +{dust} yıldız tozu!") : Loc.T($"Sandık: +{gold} altın!"));
                RefreshMenu();
            }
            else if (meta.RecordChestClaimed(index)) MenuToast("Bu sandığı zaten açtın.");
            else
            {
                MenuToast(Loc.T($"{Clock(RecordChests.Seconds[index])} hayatta kal, sandığı aç!"));
            }
        }

        /// <summary>A round event icon beside the stage card: icon, a state tag under it and a "!" badge.</summary>
        private Tile SideIcon(Transform parent, Sprite icon, float x0, float y0, System.Action onClick)
        {
            var tile = new Tile();
            tile.Button = IconButton(parent, icon, Palette.Hex(0x5B3C99), onClick);
            UIFactory.Place(tile.Button, x0, y0, x0 + 0.13f, y0 + 0.06f);
            tile.Icon = tile.Button.transform.Find("Face/Icon").GetComponent<Image>();
            tile.Icon.enabled = true;
            Image cap = Capsule(parent, x0 - 0.01f, y0 - 0.026f, x0 + 0.14f, y0 - 0.002f);
            tile.Label = _ui.Label(cap.transform, "", 22, Palette.Cream);
            UIFactory.Place(tile.Label, 0.04f, 0f, 0.96f, 1f);
            tile.Badge = Badge(tile.Button.transform, 0.72f, 0.7f, 1.02f, 1.05f);
            return tile;
        }

        private GameObject Badge(Transform parent, float x0, float y0, float x1, float y1)
        {
            Image badge = Img(parent, null, "Badge");
            badge.enabled = true;
            badge.sprite = roundedSprite;
            badge.type = Image.Type.Sliced;
            badge.color = Palette.Coral;
            UIFactory.Place(badge, x0, y0, x1, y1);
            Text bang = _ui.Label(badge.transform, "!", 30, Color.white);
            bang.GetComponent<Outline>().effectColor = Palette.Outline;
            badge.gameObject.SetActive(false);
            return badge.gameObject;
        }

        /// <summary>The centre tab sits in a glossy bubble that rises above the bar — the "you are here" tab.</summary>
        private void RaiseHomeTab(Tile home)
        {
            Transform node = home.Button.transform;
            Image bubble = Img(node, homeBubbleSprite, "Bubble");
            bubble.enabled = true;
            if (homeBubbleSprite == null) bubble.color = Palette.HotPink;
            bubble.transform.SetAsFirstSibling();
            UIFactory.Place(bubble, 0.02f, 0.28f, 0.98f, 1.5f);
            UIFactory.Place(home.Icon, 0.2f, 0.5f, 0.8f, 1.3f);
            home.Label.color = Palette.Honey;
            home.Badge.SetActive(false);
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
        private Tile NavTab(Transform bar, string label, Sprite icon, float x0, System.Action open, float width = 1f / 3f)
        {
            var tile = new Tile();
            RectTransform node = _ui.Node(label, bar);
            UIFactory.Place(node, x0 + 0.01f, 0.08f, x0 + width - 0.01f, 0.95f);
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
            UIFactory.Place(tile.Icon, 0.2f, 0.32f, 0.8f, 1f);
            tile.Label = _ui.Label(node, label, 30, Palette.White);
            UIFactory.Place(tile.Label, -0.04f, 0f, 1.04f, 0.3f);
            tile.Label.resizeTextForBestFit = true;
            tile.Label.resizeTextMinSize = 18;
            tile.Label.resizeTextMaxSize = 30;
            tile.Label.horizontalOverflow = HorizontalWrapMode.Wrap;
            tile.Badge = Badge(node, 0.66f, 0.72f, 0.88f, 0.97f);
            return tile;
        }

        // ---------------------------------------------------------------- Refresh & animation

        private void RefreshForge()
        {
            if (_researchTile == null) return;
            MetaProgressionService meta = run.Meta;
            _researchTile.Badge.SetActive(ResearchAffordable());
            _armoryTile.Badge.SetActive(ArmoryAffordable());
            bool pilotReady = false;
            foreach (CharacterDefinition c in run.Characters) pilotReady |= meta.CanUnlock(c);
            _pilotsTile.Badge.SetActive(pilotReady);

            float best = meta.BestEndlessSeconds;
            bool saved = run.HasSavedRun;
            string line;
            if (saved)
            {
                (int stage, float minutes) = run.SavedRunInfo();
                line = Loc.T($"Kayıt: Bölüm {stage} · {Mathf.FloorToInt(minutes)}:{Mathf.FloorToInt(minutes * 60f % 60f):00}");
            }
            else line = Loc.T($"Rekor: {Clock(best)}");
            _recordText.text = line;
            _recordIcon.gameObject.SetActive(!saved);
            UIFactory.SetText(_playButton, saved ? "DEVAM ET" : "OYNA");
            _restartButton.gameObject.SetActive(saved);
            RefreshChests(best);
            RefreshSideIcons();
            RefreshGift();
            RefreshMissionsBadge();
        }

        private static string Clock(float seconds) =>
            $"{Mathf.FloorToInt(seconds / 60f)}:{Mathf.FloorToInt(seconds % 60f):00}";

        private void RefreshChests(float best)
        {
            if (_chests == null) return;
            MetaProgressionService meta = run.Meta;
            _chestFill.rectTransform.anchorMax = new Vector2(RecordChests.Progress(best), 1f);
            bool any = false;
            for (int i = 0; i < _chests.Length; i++)
            {
                bool claimed = meta.RecordChestClaimed(i);
                bool ready = meta.CanClaimRecordChest(i);
                any |= ready;
                _chests[i].sprite = claimed ? chestOpenSprite : chestClosedSprite;
                _chests[i].color = claimed ? new Color(1f, 1f, 1f, 0.55f)
                    : ready ? Color.white
                    : new Color(0.6f, 0.55f, 0.7f, 1f);
            }
            int next = RecordChests.NextGoal(best);
            _chestHint.text = any ? Loc.T("Sandık hazır — dokun, aç!")
                : next >= 0 ? Loc.T($"Sonraki sandık: {Clock(RecordChests.Seconds[next])} hayatta kal")
                : Loc.T("Bütün sandıklar açıldı!");
            _chestHint.color = any ? Palette.Honey : Palette.Hex(0xDCCFFF);
        }

        private void RefreshSideIcons()
        {
            if (_nextPilotTile == null) return;
            MetaProgressionService meta = run.Meta;
            CharacterDefinition next = NextPilot();
            if (next != null)
            {
                _nextPilotTile.Icon.sprite = next.sprite;
                int pct = next.goldCost > 0 ? Mathf.Clamp(Mathf.FloorToInt(100f * meta.Gold / next.goldCost), 0, 100) : 100;
                _nextPilotTile.Label.text = $"%{pct}";
                _nextPilotTile.Badge.SetActive(meta.CanUnlock(next));
            }
            _nextPilotTile.Button.gameObject.SetActive(next != null);
            _nextPilotTile.Label.transform.parent.gameObject.SetActive(next != null);

            long now = System.DateTime.UtcNow.Ticks;
            meta.CheckIn(now);
            int day = (meta.Streak - 1) % 7 + 1;
            _streakTile.Label.text = Loc.T($"GÜN {day}/7");
            _streakTile.Badge.SetActive(meta.CanClaimStreak(now));
        }

        /// <summary>The cheapest locked pilot that gold alone opens — the menu's and the run-end's next goal.</summary>
        private CharacterDefinition NextPilot()
        {
            CharacterDefinition next = null;
            foreach (CharacterDefinition c in run.Characters)
                if (!run.Meta.IsUnlocked(c) && c.requiresChapterCleared < 0 && (next == null || c.goldCost < next.goldCost)) next = c;
            return next;
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

            if (_chests != null)
                for (int i = 0; i < _chests.Length; i++)
                {
                    bool ready = _chests[i].color == Color.white && _chests[i].sprite == chestClosedSprite;
                    float w = ready ? Mathf.Sin(t * 16f + i) * 10f * Mathf.Clamp01(Mathf.Sin(t * 2f) * 3f - 1.5f) : 0f;
                    _chests[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, w);
                    _chests[i].rectTransform.localScale = Vector3.one * (ready ? 1.1f + 0.05f * Mathf.Sin(t * 5f) : 1f);
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
