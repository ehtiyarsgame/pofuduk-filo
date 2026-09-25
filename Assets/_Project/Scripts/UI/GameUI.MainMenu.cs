using PofudukFilo.Core;
using PofudukFilo.Meta;
using PofudukFilo.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace PofudukFilo.UI
{
    /// <summary>
    /// Main menu v5, the hero lobby (design/ux/main-menu.md §4). It is built one-to-one from the approved mockup
    /// A (design/ux/mockups/menu-v5-mockup.png), and every rect is given in that mockup's 390×844 px coordinates
    /// (<see cref="At"/>). Layout, top to bottom:
    /// - top bar: pilot avatar with a level badge and bar, ink currency pills with a green "+" (ad), gear;
    /// - left column: gift, missions, stars; right column: next pilot, login streak;
    /// - the hero ship, big, floating on a glowing platform under a spotlight, with the power tag above it and
    ///   the pilot ribbon below it;
    /// - the record road (a chest track);
    /// - a huge golden bevelled OYNA button;
    /// - a 5-tab bar whose selected Home tab is raised.
    /// Everything uses the casual kit (ArtRecipes.CandyButton and friends) and ink-stroked text.
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
        [SerializeField] private Sprite chestClosedSprite;
        [SerializeField] private Sprite chestOpenSprite;
        [SerializeField] private Sprite homeIcon;

        [Header("Lobby kit (main-menu.md §4)")]
        [SerializeField] private Sprite kitYellow;
        [SerializeField] private Sprite kitPink;
        [SerializeField] private Sprite kitBlue;
        [SerializeField] private Sprite kitPurple;
        [SerializeField] private Sprite kitOrange;
        [SerializeField] private Sprite kitGreen;
        [SerializeField] private Sprite kitLavender;
        [SerializeField] private Sprite kitViolet;
        [SerializeField] private Sprite kitPill;
        [SerializeField] private Sprite kitPanel;
        [SerializeField] private Sprite kitDotRed;
        [SerializeField] private Sprite kitDotGold;
        [SerializeField] private Sprite kitBarInk;
        [SerializeField] private Sprite kitBarGold;
        [SerializeField] private Sprite kitBarGreen;
        [SerializeField] private Sprite kitPlatform;
        [SerializeField] private Sprite kitSpot;
        [SerializeField] private Sprite kitBackdrop;
        [SerializeField] private Sprite kitTabBar;
        [SerializeField] private Sprite kitPower;

        /// <summary>Ink: the outline colour of every lobby shape and text stroke.</summary>
        private static readonly Color Ink = Palette.Hex(0x1D1440);
        private static readonly Color TabIdle = Palette.Hex(0xB9B0EA);
        private const float MockW = 390f, MockH = 844f;

        private sealed class Tile
        {
            public Button Button;
            public Image Icon;
            public Text Label;
            public GameObject Badge;
            public Text BadgeText;
        }

        private Image _heroShip;
        private RectTransform _heroSpot;
        private RectTransform _platform;
        private RectTransform _shine;
        private Text _recordText;
        private Text _playSub;
        private Button _restartButton;
        private Text _endDust;
        private GameObject _endDustIcon;
        private GameObject _endDustGap;
        private Tile _researchTile;
        private Tile _armoryTile;
        private Tile _pilotsTile;
        private Tile _missionsTile;
        private Tile _giftTile;
        private Tile _sideMissions;
        private Tile _starsTile;
        private Tile _nextPilotTile;
        private Tile _streakTile;
        private Image _avatar;
        private Text _levelText;
        private Image _levelFill;
        private Text _ribbonText;
        private Image _chestFill;
        private Image[] _chests;
        private GameObject _giftDot;

        private void BuildMenu(Transform root)
        {
            _menu = MakeScreen(root, GameState.MainMenu, dim: false);
            Transform m = _menu.transform;

            // Calm radial backdrop over the busy game starfield, bleeding past the safe area.
            Image backdrop = Kit(m, kitBackdrop, "Backdrop", 1f, Image.Type.Simple);
            backdrop.preserveAspect = false;
            if (kitBackdrop == null) backdrop.color = Palette.Hex(0x2A1F6E);
            UIFactory.Place(backdrop, -0.08f, -0.08f, 1.08f, 1.08f);

            BuildTopBar(m);
            BuildHero(m);
            BuildSideColumns(m);
            BuildRecordRoad(m);
            BuildPlay(m);
            BuildTabBar(m);

            BuildRewards(m, root);
            BuildMissions(m, root);
        }

        // ---------------------------------------------------------------- Sections

        private void BuildTopBar(Transform m)
        {
            // Avatar: the selected pilot in a blue tile (tap → Pilotlar), level badge on its corner.
            Button avatar = KitButton(m, kitBlue, OpenHangar, 1.1f);
            At(avatar, 12, 14, 54, 54);
            _avatar = Kit(avatar.transform, null, "Pilot", 1f, Image.Type.Simple);
            _avatar.preserveAspect = true;
            UIFactory.Place(_avatar, -0.1f, 0.02f, 1.1f, 1.14f);
            Image badge = Kit(m, kitDotGold, "LevelBadge", 1f, Image.Type.Simple);
            At(badge, 6, 50, 26, 26);
            _levelText = InkText(badge.transform, "1", 39, Palette.White, 4f);

            _pilotText = InkText(m, "", 42, Palette.White, 4f, TextAnchor.MiddleLeft);
            At(_pilotText, 74, 21, 92, 22);
            Image track = Kit(m, kitBarInk, "LevelTrack", 0.67f);
            At(track, 74, 45, 86, 12);
            _levelFill = Kit(track.transform, kitBarGreen, "Fill", 0.8f);
            UIFactory.Place(_levelFill, 0.03f, 0.17f, 0.5f, 0.83f);

            _wallet = new WalletView
            {
                Gold = CurrencyPill(m, coinIcon, 166, 84),
                Dust = CurrencyPill(m, stardustIcon, 258, 68)
            };

            Button gear = KitButton(m, kitLavender, OpenSettings, 1.15f);
            At(gear, 334, 19, 44, 44);
            Image cog = Kit(gear.transform, gearIcon, "Icon", 1f, Image.Type.Simple);
            cog.preserveAspect = true;
            UIFactory.Place(cog, 0.17f, 0.22f, 0.83f, 0.88f);
        }

        /// <summary>An ink pill with the currency icon overlapping its left end and a green "+" (ad → gift) inside its right end.</summary>
        private Text CurrencyPill(Transform m, Sprite icon, float x, float w)
        {
            Image pill = Kit(m, kitPill, "Pill", 1.66f);
            At(pill, x, 24, w, 34);
            Text amount = InkText(pill.transform, "0", 47, Palette.White, 4f);
            UIFactory.Place(amount, 0.26f, 0f, 0.7f, 1f);
            amount.resizeTextForBestFit = true;
            amount.resizeTextMinSize = 28;
            amount.resizeTextMaxSize = 47;
            amount.horizontalOverflow = HorizontalWrapMode.Wrap;
            Image i = Kit(m, icon, "Icon", 1f, Image.Type.Simple);
            i.preserveAspect = true;
            At(i, x - 10, 19, 40, 40);
            Button plus = KitButton(m, kitGreen, ClaimGift, 1.7f);
            At(plus, x + w - 28, 26, 26, 26);
            Text sign = InkText(plus.transform, "+", 58, Palette.White, 4f);
            UIFactory.Place(sign, 0f, 0.12f, 1f, 1f);
            _walletAdButtons.Add(plus);
            return amount;
        }

        private void BuildHero(Transform m)
        {
            Image spot = Kit(m, kitSpot, "Spotlight", 1f, Image.Type.Simple);
            At(spot, 15, 130, 360, 360);
            _heroSpot = spot.rectTransform;
            Image platform = Kit(m, kitPlatform, "Platform", 1f, Image.Type.Simple);
            platform.preserveAspect = false;
            At(platform, 80, 398, 230, 62);
            _platform = platform.rectTransform;
            _heroShip = Kit(m, null, "HeroShip", 1f, Image.Type.Simple);
            _heroShip.preserveAspect = true;
            At(_heroShip, 70, 170, 250, 250);
            var drop = _heroShip.gameObject.AddComponent<Shadow>();
            drop.effectColor = new Color(0f, 0f, 0f, 0.3f);
            drop.effectDistance = new Vector2(0f, -24f);

            // Power tag above the hero (tap → Ar-Ge, where power is bought).
            Image tag = Kit(m, kitPill, "PowerTag", 1.86f);
            At(tag, 118, 152, 154, 30);
            tag.raycastTarget = true;
            tag.gameObject.AddComponent<Button>().onClick.AddListener(OpenResearch);
            Image bolt = Kit(tag.transform, kitPower, "Bolt", 1f, Image.Type.Simple);
            bolt.preserveAspect = true;
            UIFactory.Place(bolt, 0.04f, 0.14f, 0.2f, 0.86f);
            _powerText = InkText(tag.transform, "", 44, Palette.Hex(0xFFE45C), 4f);
            UIFactory.Place(_powerText, 0.22f, 0f, 0.96f, 1f);
            _powerText.resizeTextForBestFit = true;
            _powerText.resizeTextMinSize = 26;
            _powerText.resizeTextMaxSize = 44;
            _powerText.horizontalOverflow = HorizontalWrapMode.Wrap;

            // Pilot ribbon under the platform: name, then the pilot's own gun in gold.
            Image ribbon = Kit(m, kitPink, "Ribbon", 1.36f);
            At(ribbon, 62, 462, 266, 40);
            _ribbonText = InkText(ribbon.transform, "", 55, Palette.White, 5f);
            UIFactory.Place(_ribbonText, 0.04f, 0.14f, 0.96f, 1f);
            _ribbonText.supportRichText = true;
            _ribbonText.resizeTextForBestFit = true;
            _ribbonText.resizeTextMinSize = 30;
            _ribbonText.resizeTextMaxSize = 55;
        }

        private void BuildSideColumns(Transform m)
        {
            _giftTile = SideButton(m, kitPink, giftIcon, 10, 150, ClaimGift);
            _giftButton = _giftTile.Button;
            _giftText = _giftTile.Label;
            _giftDot = _giftTile.Badge;
            _giftTile.BadgeText.text = "1";
            _sideMissions = SideButton(m, kitBlue, trophyIcon, 10, 230, () => OpenMeta(_missions));
            _sideMissions.Label.text = Loc.T("GÖREV");
            _starsTile = SideButton(m, kitPurple, stardustIcon, 10, 310, OpenConstellation);
            _starsTile.Label.text = Loc.T("YILDIZ");
            _nextPilotTile = SideButton(m, kitOrange, null, 318, 150, OpenHangar);
            _streakTile = SideButton(m, kitGreen, pawSprite, 318, 230, () => OpenMeta(_missions));
        }

        /// <summary>A 62×62 candy tile with an icon, an ink label hanging under it and a red dot on its corner.</summary>
        private Tile SideButton(Transform m, Sprite face, Sprite icon, float x, float y, System.Action onClick)
        {
            var tile = new Tile { Button = KitButton(m, face, onClick, 0.86f) };
            At(tile.Button, x, y, 62, 62);
            tile.Icon = Kit(tile.Button.transform, icon, "Icon", 1f, Image.Type.Simple);
            tile.Icon.preserveAspect = true;
            UIFactory.Place(tile.Icon, 0.14f, 0.2f, 0.86f, 0.9f);
            tile.Label = InkText(m, "", 36, Palette.White, 4.5f);
            At(tile.Label, x - 12, y + 55, 86, 20);
            (tile.Badge, tile.BadgeText) = Dot(m, x + 48, y - 8);
            return tile;
        }

        private (GameObject, Text) Dot(Transform m, float x, float y)
        {
            Image dot = Kit(m, kitDotRed, "Dot", 1f, Image.Type.Simple);
            if (kitDotRed == null) dot.color = Palette.Coral;
            At(dot, x, y, 22, 22);
            Text t = InkText(dot.transform, "!", 36, Palette.White, 3.5f);
            UIFactory.Place(t, 0f, 0.06f, 1f, 1f);
            dot.gameObject.SetActive(false);
            return (dot.gameObject, t);
        }

        private void BuildRecordRoad(Transform m)
        {
            Image panel = Kit(m, kitPanel, "RecordRoad", 0.76f);
            At(panel, 16, 520, 358, 70);
            Text title = InkText(m, "REKOR YOLU", 39, Palette.White, 4f, TextAnchor.MiddleLeft);
            At(title, 30, 526, 170, 22);
            _recordText = InkText(m, "", 39, Palette.Hex(0xFFE45C), 4f, TextAnchor.MiddleRight);
            At(_recordText, 180, 526, 180, 22);

            Image track = Kit(m, kitBarInk, "Track", 0.67f);
            At(track, 34, 560, 310, 14);
            _chestFill = Kit(track.transform, kitBarGold, "Fill", 0.8f);
            UIFactory.Place(_chestFill, 0f, 0.14f, 0f, 0.86f);

            _chests = new Image[RecordChests.Count];
            for (int i = 0; i < _chests.Length; i++)
            {
                float cx = 34 + 310f * (i + 1) / _chests.Length;
                Image chest = Kit(m, chestClosedSprite, "Chest", 1f, Image.Type.Simple);
                chest.preserveAspect = true;
                chest.raycastTarget = true;
                At(chest, cx - 21, 540, 42, 42);
                int index = i;
                chest.gameObject.AddComponent<Button>().onClick.AddListener(() => TapChest(index));
                _chests[i] = chest;
            }
        }

        private void BuildPlay(Transform m)
        {
            _playButton = KitButton(m, kitYellow, () =>
            {
                if (run.HasSavedRun) run.ResumeRun();
                else run.StartEndless();
            }, 0.58f);
            At(_playButton, 34, 622, 322, 92);
            _playButton.gameObject.AddComponent<RectMask2D>();
            Text big = InkText(_playButton.transform, "OYNA", 138, Palette.White, 8f);
            UIFactory.Place(big, 0.02f, 0.36f, 0.98f, 0.97f);
            big.resizeTextForBestFit = true;
            big.resizeTextMinSize = 70;
            big.resizeTextMaxSize = 138;
            _playSub = InkText(_playButton.transform, "", 42, Palette.White, 4f);
            _playSub.GetComponent<UIStroke>().color = Palette.Hex(0x7A3A00);
            UIFactory.Place(_playSub, 0.02f, 0.12f, 0.98f, 0.4f);
            Image shine = Kit(_playButton.transform, shineSprite, "Shine", 1f, Image.Type.Simple);
            shine.color = new Color(1f, 1f, 1f, 0.5f);
            _shine = shine.rectTransform;
            _shine.anchorMin = new Vector2(0f, -0.2f);
            _shine.anchorMax = new Vector2(0f, 1.2f);
            _shine.sizeDelta = new Vector2(120f, 0f);

            // "Yeni Oyun" under PLAY, only while a run is saved.
            Image pill = Kit(m, kitPill, "NewGame", 1.9f);
            At(pill, 120, 718, 150, 28);
            pill.raycastTarget = true;
            _restartButton = pill.gameObject.AddComponent<Button>();
            _restartButton.onClick.AddListener(run.StartEndless);
            InkText(pill.transform, "Yeni Oyun", 36, Palette.White, 3.5f);
        }

        private void BuildTabBar(Transform m)
        {
            Image bar = Kit(m, kitTabBar, "TabBar", 1f);
            if (kitTabBar == null) bar.color = Palette.Hex(0x1B1447);
            bar.raycastTarget = true;
            UIFactory.Place(bar, -0.02f, -0.08f, 1.02f, 1f - 758f / MockH);

            float[] x0 = { 6f, 77.7f, 149.3f, 244.7f, 316.3f };
            float[] w = { 67.7f, 67.7f, 91.4f, 67.7f, 67.7f };
            _researchTile = Tab(m, "AR-GE", researchIcon, x0[0], w[0], OpenResearch);
            _armoryTile = Tab(m, "SİLAHLAR", weaponsIcon, x0[1], w[1], OpenLab);
            Tab(m, "ANA SAYFA", homeIcon, x0[2], w[2], () => { }, selected: true);
            _pilotsTile = Tab(m, "PİLOTLAR", null, x0[3], w[3], OpenHangar);
            _missionsTile = Tab(m, "GÖREVLER", trophyIcon, x0[4], w[4], () => OpenMeta(_missions));
        }

        /// <summary>A bottom tab: icon over a label; the selected one is a raised violet candy tile, taller and brighter.</summary>
        private Tile Tab(Transform m, string label, Sprite icon, float x, float w, System.Action open, bool selected = false)
        {
            var tile = new Tile();
            float y = selected ? 746f : 766f, h = selected ? 84f : 64f;
            if (selected)
            {
                tile.Button = KitButton(m, kitViolet, open, 0.86f);
            }
            else
            {
                Image hit = Kit(m, null, label, 1f, Image.Type.Simple);
                hit.color = new Color(1f, 1f, 1f, 0f);
                hit.raycastTarget = true;
                tile.Button = hit.gameObject.AddComponent<Button>();
                tile.Button.onClick.AddListener(() =>
                {
                    Feel.Juice.PunchUI(hit.rectTransform);
                    if (Audio.AudioManager.Instance != null) Audio.AudioManager.Instance.Play(Audio.SfxId.Click, 0.03f);
                    open();
                });
            }
            At(tile.Button, x, y, w, h);
            float iconSize = selected ? 54f : 40f;
            tile.Icon = Kit(m, icon, "Icon", 1f, Image.Type.Simple);
            tile.Icon.preserveAspect = true;
            At(tile.Icon, x + (w - iconSize) / 2f, selected ? 752f : 768f, iconSize, iconSize);
            tile.Label = InkText(m, label, selected ? 39 : 33, selected ? Palette.White : TabIdle, 4f);
            At(tile.Label, x - 4, selected ? 808f : 812f, w + 8, 18);
            tile.Label.resizeTextForBestFit = true;
            tile.Label.resizeTextMinSize = 22;
            tile.Label.resizeTextMaxSize = selected ? 39 : 33;
            tile.Label.horizontalOverflow = HorizontalWrapMode.Wrap;
            (tile.Badge, tile.BadgeText) = Dot(m, x + w - 32, y);
            return tile;
        }

        // ---------------------------------------------------------------- Kit helpers

        /// <summary>Places a rect by the mockup's 390×844 px coordinates (x, y from the top-left).</summary>
        private static void At(Component c, float x, float y, float w, float h) =>
            UIFactory.Place(c, x / MockW, 1f - (y + h) / MockH, (x + w) / MockW, 1f - y / MockH);

        /// <summary>An image from the kit; sliced sprites use <paramref name="ppu"/> to set how thick their border draws.</summary>
        private Image Kit(Transform parent, Sprite sprite, string name, float ppu = 1f, Image.Type type = Image.Type.Sliced)
        {
            Image img = _ui.Node(name, parent).gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = sprite != null ? type : Image.Type.Simple;
            img.pixelsPerUnitMultiplier = ppu;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>A candy-kit button: pressing dims and punches it, with a click.</summary>
        private Button KitButton(Transform parent, Sprite face, System.Action onClick, float ppu)
        {
            Image img = Kit(parent, face, "KitButton", ppu);
            if (face == null) img.color = Palette.Lavender;
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            ColorBlock colors = b.colors;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.65f, 0.75f);
            b.colors = colors;
            b.onClick.AddListener(() =>
            {
                Feel.Juice.PunchUI(img.rectTransform);
                if (Audio.AudioManager.Instance != null) Audio.AudioManager.Instance.Play(Audio.SfxId.Click, 0.03f);
                onClick?.Invoke();
            });
            return b;
        }

        /// <summary>Text with a thick ink stroke (four offset copies) and a drop underneath — the casual-game title look.</summary>
        private Text InkText(Transform parent, string text, int size, Color color, float stroke,
            TextAnchor align = TextAnchor.MiddleCenter)
        {
            Text t = _ui.Label(parent, text, size, color, align);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            Destroy(t.GetComponent<Outline>()); // removed at frame end; UIStroke draws the whole stroke itself
            UIStroke s = t.gameObject.AddComponent<UIStroke>();
            s.color = Ink;
            s.width = stroke;
            s.drop = stroke * 1.3f;
            return t;
        }

        // ---------------------------------------------------------------- Refresh

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
            UIFactory.SetText(_playButton, saved ? "DEVAM ET" : "OYNA");
            if (saved)
            {
                (int stage, float minutes) = run.SavedRunInfo();
                _playSub.text = Loc.T($"Kayıt: Bölüm {stage} · {Mathf.FloorToInt(minutes)}:{Mathf.FloorToInt(minutes * 60f % 60f):00}");
            }
            else _playSub.text = Loc.T("SONSUZ GALAKSİ");
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
            _chestFill.rectTransform.anchorMax = new Vector2(RecordChests.Progress(best), 0.86f);
            _chestFill.enabled = best > 0f;
            bool any = false;
            for (int i = 0; i < _chests.Length; i++)
            {
                bool claimed = meta.RecordChestClaimed(i);
                bool ready = meta.CanClaimRecordChest(i);
                any |= ready;
                _chests[i].sprite = claimed ? chestOpenSprite : chestClosedSprite;
                _chests[i].color = claimed || ready ? Color.white : new Color(0.45f, 0.42f, 0.55f, 1f);
            }
            _recordText.text = any ? Loc.T("Sandık hazır!") : Loc.T($"En iyi {Clock(best)}");
        }

        private void RefreshSideIcons()
        {
            if (_nextPilotTile == null) return;
            MetaProgressionService meta = run.Meta;
            CharacterDefinition next = NextPilot();
            _nextPilotTile.Button.gameObject.SetActive(next != null);
            _nextPilotTile.Label.gameObject.SetActive(next != null);
            if (next != null)
            {
                _nextPilotTile.Icon.sprite = next.sprite;
                int pct = next.goldCost > 0 ? Mathf.Clamp(Mathf.FloorToInt(100f * meta.Gold / next.goldCost), 0, 100) : 100;
                _nextPilotTile.Label.text = $"%{pct}";
            }
            _nextPilotTile.Badge.SetActive(next != null && meta.CanUnlock(next));

            long now = System.DateTime.UtcNow.Ticks;
            meta.CheckIn(now);
            _streakTile.Label.text = Loc.T($"GÜN {(meta.Streak - 1) % 7 + 1}");
            _streakTile.Badge.SetActive(meta.CanClaimStreak(now));
            bool missions = meta.AnyMissionClaimable(now);
            _sideMissions.Badge.SetActive(missions);
        }

        /// <summary>Avatar, name, level and ribbon for the selected pilot (called from RefreshPilot).</summary>
        private void RefreshLobbyPilot(CharacterDefinition pilot)
        {
            int level = run.Meta.GetPilotLevel(pilot.id);
            if (_avatar != null) _avatar.sprite = pilot.sprite;
            if (_pilotText != null) _pilotText.text = Loc.T(pilot.displayName);
            if (_levelText != null) _levelText.text = level.ToString();
            if (_levelFill != null) _levelFill.rectTransform.anchorMax = new Vector2(0.03f + 0.94f * level / Formulas.MaxPilotLevel, 0.83f);
            if (_pilotsTile != null) _pilotsTile.Icon.sprite = pilot.sprite;
            if (_ribbonText != null)
            {
                string gun = pilot.mainGun != null ? Loc.T(pilot.mainGun.displayName) : "";
                _ribbonText.text = gun.Length > 0
                    ? $"{Loc.T(pilot.displayName)}  <size=36><color=#FFE45C>{gun}</color></size>"
                    : Loc.T(pilot.displayName); // no ToUpper: invariant casing turns Turkish i into I, not İ
            }
            if (_heroShip != null)
            {
                _heroShip.sprite = pilot.shipSprite != null ? pilot.shipSprite : pilot.sprite;
                Feel.Juice.PopIn(_heroShip.transform);
            }
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
            else MenuToast(Loc.T($"{Clock(RecordChests.Seconds[index])} hayatta kal, sandığı aç!"));
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
            // Hero gun mods (hero-guns.md §4) of the pilots you own.
            foreach (CharacterDefinition c in run.Characters)
            {
                if (c == null || !meta.IsUnlocked(c)) continue;
                WeaponDefinition gun = run.MainGunOf(c);
                if (gun == null) continue;
                foreach (GunMod m in GunMods.For(gun.id))
                    if (meta.CanUpgradeGunMod(gun.id, m.Key)) return true;
            }
            return false;
        }

        private void UpdateMenuAnim()
        {
            if (_menu == null || !_menu.activeSelf || _heroShip == null) return;
            float t = Time.unscaledTime;
            _heroShip.rectTransform.anchoredPosition = new Vector2(0f, 14f + Mathf.Sin(t * 1.8f) * 18f);
            _heroShip.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.1f) * 3f);
            if (_platform != null) _platform.localScale = Vector3.one * (1f + 0.03f * Mathf.Sin(t * 1.8f + 1.4f));
            if (_heroSpot != null) _heroSpot.localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(t * 0.9f));
            _playButton.transform.localScale = Vector3.one * (1f + Mathf.Max(0f, Mathf.Sin(t * 3f)) * 0.03f);

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
                    _chests[i].rectTransform.localScale = Vector3.one * (ready ? 1.15f + 0.05f * Mathf.Sin(t * 5f) : 1f);
                }
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
    }
}
