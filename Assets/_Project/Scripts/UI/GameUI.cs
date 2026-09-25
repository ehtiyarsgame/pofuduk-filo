using System.Collections.Generic;
using PofudukFilo.Core;
using PofudukFilo.Enemies;
using PofudukFilo.Meta;
using PofudukFilo.Player;
using PofudukFilo.Progression;
using PofudukFilo.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace PofudukFilo.UI
{
    /// <summary>
    /// Every screen of the game, built from code and driven by RunController events:
    /// main menu, workshop, HUD, level-up cards, pause, death, run end (game-concept.md §4.5,
    /// meta-economy.md §3.4, art-bible.md §4). Screens only call RunController's public API.
    /// </summary>
    public sealed partial class GameUI : MonoBehaviour
    {
        [SerializeField] private RunController run;
        [SerializeField] private XpSystem xpSystem;
        [SerializeField] private WeaponInventory inventory;
        [SerializeField] private PlayerHealth player;
        [SerializeField] private WaveDirector waveDirector;
        [SerializeField] private PickupSystem pickups;
        [SerializeField] private Font font;
        [SerializeField] private Sprite roundedSprite;
        [SerializeField] private Sprite buttonSprite;

        private UIFactory _ui;
        private readonly Dictionary<GameState, GameObject> _screens = new();

        // HUD
        private GameObject _hud;
        [Header("HUD art")]
        [SerializeField] private Sprite barTrackSprite;
        [SerializeField] private Sprite barFillSprite;
        [SerializeField] private Sprite heartIcon;
        [SerializeField] private Sprite coinIcon;
        [SerializeField] private Sprite xpIcon;
        [SerializeField] private Sprite sugarIcon;

        private HudBar _xpBar;
        private HudBar _hpBar;
        private Text _goldText;
        private HudBar _bossHp;
        private GameObject _bossBar;
        private Enemy _boss;
        private Text _toast;
        private float _toastUntil;

        // Level-up
        private Transform _cardRow;
        private Text _levelUpTitle;
        private Button _rerollButton;
        private Button _banishButton;
        private bool _banishMode;
        private readonly List<GameObject> _cards = new(4);

        // Death / run end
        private Button _reviveButton;
        private Button _adReviveButton;
        private Text _endTitle;
        private Text _endStats;
        private Text _endGold;
        private Text _endGoal;
        private Button _upgradeButton;
        private Button _endlessButton;
        private float _goldShown;
        private int _goldTarget;

        // Menu / workshop
        private GameObject _menu;
        private GameObject _workshop;
        private WalletView _wallet;
        private Button _playButton;
        private Transform _workshopList;

        private void Start()
        {
            UIFactory.EnsureEventSystem();
            _ui = new UIFactory(font, roundedSprite, buttonSprite);

            Canvas canvas = _ui.Canvas("GameUI", 10);
            RectTransform safe = _ui.SafeArea(canvas.transform);

            BuildHud(safe);
            BuildLevelUp(safe);
            BuildPause(safe);
            BuildDeath(safe);
            BuildRunEnd(safe);
            BuildMenu(safe);
            BuildWorkshop(safe);
            BuildMetaScreens(safe);
            BuildNav(safe);
            BuildSettings(safe);

            run.StateChanged += OnStateChanged;
            run.LevelUpOffered += OnLevelUpOffered;
            run.RunEnded += OnRunEnded;
            run.ChestOpened += OnChestOpened;
            run.Meta.WalletChanged += RefreshWallet;
            xpSystem.XpChanged += OnXpChanged;
            player.HealthChanged += OnHealthChanged;
            pickups.RunGoldChanged += g => _goldText.text = Loc.T($"{g}");
            waveDirector.BossSpawned += OnBossSpawned;
            waveDirector.FormationCleared += _ => Toast("Formasyon Temizlendi!");
            waveDirector.PhaseStarted += p => { if (p.kind != PhaseKind.Waves) ShowBossBanner(p.label); };
            waveDirector.StageAdvanced += (cleared, next) => Toast($"Bölüm {next + 1} başladı!", 2.4f);
            run.LevelUpAutoRewarded += (healed, amount) =>
                Toast(healed ? Loc.T($"Seviye! +{amount} can") : Loc.T($"Seviye! +{amount} altın"), 1.4f);
            inventory.WeaponEvolved += (from, to) => Toast(Loc.T($"EVRİM! {Loc.T(from.displayName)} » {Loc.T(to.displayName)}"), 2.6f);

            OnStateChanged(run.State);
        }

        private RectTransform _topPanel;
        private static readonly Vector3[] s_corners = new Vector3[4];

        /// <summary>Keeps the world's hittable line on the HUD panel's bottom edge (Playfield.TopInset).</summary>
        private void MeasurePlayfield()
        {
            if (_topPanel == null) return;
            Camera cam = Camera.main;
            if (cam == null) return;
            _topPanel.GetWorldCorners(s_corners); // screen pixels on an overlay canvas
            Rect view = cam.pixelRect;
            if (view.height <= 0f) return;
            Core.Playfield.TopInset = Mathf.Clamp((view.yMax - s_corners[0].y) / view.height, 0.02f, 0.3f);
        }

        private void Update()
        {
            MeasurePlayfield();
            UpdateRushHud();
            UpdateLoadoutStrip();
            UpdateMenuAnim();
            UpdateRewards();
            if (_toast != null && _toast.gameObject.activeSelf && Time.unscaledTime > _toastUntil)
                _toast.gameObject.SetActive(false);

            if (_bossBar != null && _bossBar.activeSelf)
            {
                if (_boss == null || _boss.IsDead || !_boss.gameObject.activeInHierarchy) _bossBar.SetActive(false);
                else _bossHp.Set(_boss.HpFraction, Loc.T($"BOSS  %{Mathf.CeilToInt(_boss.HpFraction * 100f)}"));
            }

            // Run-end reward counter "clinks" up (game-concept.md §4.5 step 2).
            if (_endGold != null && _goldShown < _goldTarget)
            {
                _goldShown = Mathf.MoveTowards(_goldShown, _goldTarget, Mathf.Max(60f, _goldTarget) * Time.unscaledDeltaTime);
                _endGold.text = $"+{Mathf.RoundToInt(_goldShown)}";
            }
        }

        // ---------------------------------------------------------------- State routing

        private void OnStateChanged(GameState state)
        {
            foreach (KeyValuePair<GameState, GameObject> pair in _screens)
                pair.Value.SetActive(pair.Key == state);

            _hud.SetActive(state is GameState.Playing or GameState.LevelUp or GameState.Paused or GameState.Dead);
            _workshop.SetActive(false);
            HideMetaScreens();
            if (_nav != null)
            {
                _nav.SetActive(state == GameState.MainMenu); // the tab bar is there everywhere outside a run
                SelectTab(HomeTab);
            }

            if (state == GameState.MainMenu) RefreshMenu();
            if (state == GameState.Dead) RefreshDeath();
        }

        // ---------------------------------------------------------------- HUD

        private void BuildHud(Transform root)
        {
            _hud = _ui.Node("HUD", root).gameObject;

            // Top panel (design/ux/hud.md §2): HP + gold + pause on row 1, XP + sugar on row 2. Opaque and edge to
            // edge (up through any notch): enemies emerge from behind it, and become hittable the moment they show
            // (Playfield.TopInset follows its bottom edge).
            Image plate = _ui.Panel(_hud.transform, Palette.Hex(0x221733), "TopPanel");
            plate.type = Image.Type.Simple;
            plate.sprite = null;
            UIFactory.Place(plate, 0f, 0.892f, 1f, 1f);
            plate.rectTransform.offsetMin = new Vector2(-60f, 0f);
            plate.rectTransform.offsetMax = new Vector2(60f, 400f);
            plate.raycastTarget = false;
            _topPanel = plate.rectTransform;
            // A lit rim and a soft shadow under it, like a cockpit dashboard the enemies fly out from under.
            Image rim = _ui.Panel(plate.transform, Palette.Hex(0x9C7BFF), "Rim");
            rim.type = Image.Type.Simple;
            rim.sprite = null;
            rim.raycastTarget = false;
            rim.rectTransform.anchorMin = new Vector2(0f, 0f);
            rim.rectTransform.anchorMax = new Vector2(1f, 0f);
            rim.rectTransform.offsetMin = new Vector2(0f, 0f);
            rim.rectTransform.offsetMax = new Vector2(0f, 5f);
            if (vignetteSprite != null)
            {
                Image shade = _ui.Node("Shade", _hud.transform).gameObject.AddComponent<Image>();
                shade.sprite = vignetteSprite;
                shade.color = new Color(1f, 1f, 1f, 0.6f);
                shade.raycastTarget = false;
                UIFactory.Place(shade, 0f, 0.872f, 1f, 0.892f);
                shade.rectTransform.offsetMin = new Vector2(-60f, 0f);
                shade.rectTransform.offsetMax = new Vector2(60f, 0f);
            }

            _hpBar = HudBar.Create(_ui, _hud.transform, barTrackSprite, barFillSprite, Palette.HotPink, heartIcon, 32);
            UIFactory.Place(_hpBar, 0.085f, 0.943f, 0.47f, 0.975f);

            if (coinIcon != null)
            {
                Image coin = _ui.Node("CoinIcon", _hud.transform).gameObject.AddComponent<Image>();
                coin.sprite = coinIcon;
                coin.preserveAspect = true;
                coin.raycastTarget = false;
                UIFactory.Place(coin, 0.5f, 0.945f, 0.56f, 0.973f);
            }
            _goldText = _ui.Label(_hud.transform, "0", 44, Palette.Honey, TextAnchor.MiddleLeft);
            UIFactory.Place(_goldText, 0.565f, 0.94f, 0.8f, 0.978f);

            Button pause = pauseIcon != null
                ? IconButton(_hud.transform, pauseIcon, Palette.Lavender, run.Pause)
                : _ui.Button(_hud.transform, "II", Palette.Lavender, run.Pause, 52);
            UIFactory.Place(pause, 0.845f, 0.93f, 0.97f, 0.982f);

            _xpBar = HudBar.Create(_ui, _hud.transform, barTrackSprite, barFillSprite, Palette.Sky, xpIcon, 28);
            UIFactory.Place(_xpBar, 0.085f, 0.902f, 0.53f, 0.93f);
            _xpBar.Snap(0f);

            _bossBar = _ui.Node("BossBar", _hud.transform).gameObject;
            UIFactory.Place(_bossBar.GetComponent<RectTransform>(), 0.1f, 0.81f, 0.94f, 0.838f); // under the loadout strip
            _bossHp = HudBar.Create(_ui, _bossBar.transform, barTrackSprite, barFillSprite, Palette.Coral, null, 30);
            UIFactory.Place(_bossHp, 0f, 0f, 1f, 1f);
            _bossBar.SetActive(false);

            BuildRushHud(_hud.transform);
            BuildLoadoutStrip(_hud.transform);

            _toast = _ui.Label(_hud.transform, "", 80, Palette.Cream);
            UIFactory.Place(_toast, 0.05f, 0.6f, 0.95f, 0.7f);
            _toast.gameObject.SetActive(false);
        }

        private void OnXpChanged(int current, int required)
        {
            _xpBar.Set(required > 0 ? current / (float)required : 0f, Loc.T($"Sv. {xpSystem.Level}"));
        }

        private void OnHealthChanged(float current, float max) =>
            _hpBar.Set(max > 0f ? current / max : 0f, $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}");

        private void OnBossSpawned(Enemy boss)
        {
            _boss = boss;
            _bossBar.SetActive(true);
            _bossHp.Snap(1f);
            _bossHp.Set(1f, "BOSS");
        }

        private void OnChestOpened(int evolved)
        {
            if (evolved == 0) Toast("Sandık!");
            else if (evolved > 1) Toast("JACKPOT!");
        }

        private void Toast(string text, float seconds = 1.6f)
        {
            _toast.text = Loc.T(text);
            _toast.gameObject.SetActive(true);
            _toastUntil = Time.unscaledTime + seconds;
            Feel.Juice.PopIn(_toast.transform);
        }

        // ---------------------------------------------------------------- Level-up

        private void BuildLevelUp(Transform root)
        {
            GameObject screen = MakeScreen(root, GameState.LevelUp);
            _levelUpTitle = _ui.Label(screen.transform, "Seviye Atladın!", 96, Palette.Cream);
            UIFactory.Place(_levelUpTitle, 0.05f, 0.78f, 0.95f, 0.86f);

            _cardRow = _ui.Node("Cards", screen.transform);
            UIFactory.Place(_cardRow, 0.04f, 0.3f, 0.96f, 0.76f);
            var layout = _cardRow.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 36f;
            layout.childForceExpandHeight = true;
            layout.childControlHeight = true;
            layout.childControlWidth = true;

            _rerollButton = _ui.Button(screen.transform, "Yeniden Çek", Palette.Mint, run.Reroll, 52);
            UIFactory.Place(_rerollButton, 0.06f, 0.18f, 0.48f, 0.25f);
            _banishButton = _ui.Button(screen.transform, "Yasakla", Palette.Coral, ToggleBanish, 52);
            UIFactory.Place(_banishButton, 0.52f, 0.18f, 0.94f, 0.25f);
        }

        private void OnLevelUpOffered(IReadOnlyList<UpgradeOption> offer, int rerolls, int banishes)
        {
            _banishMode = false;
            _levelUpTitle.text = Loc.T($"Seviye {xpSystem.Level}!");
            foreach (GameObject c in _cards)
            {
                c.SetActive(false); // leave the layout now; Destroy is deferred to end of frame
                Destroy(c);
            }
            _cards.Clear();

            for (int i = 0; i < offer.Count; i++)
            {
                GameObject card = BuildCard(offer[i], i);
                _cards.Add(card);
                Feel.Juice.PopIn(card.transform, i * 0.08f); // cards drop in one by one
            }

            UIFactory.SetText(_rerollButton, $"Yeniden Çek ({rerolls})");
            _rerollButton.interactable = rerolls > 0;
            UIFactory.SetText(_banishButton, $"Yasakla ({banishes})");
            _banishButton.interactable = banishes > 0;
        }

        private GameObject BuildCard(in UpgradeOption option, int index)
        {
            Describe(option, out string title, out string body, out Rarity rarity);
            string tag = "";
            int split = title.IndexOf("  ", System.StringComparison.Ordinal);
            if (split > 0)
            {
                tag = title.Substring(split).Trim();
                title = title.Substring(0, split);
            }
            Sprite icon = option.Kind switch
            {
                UpgradeKind.Weapon => option.Weapon.icon,
                UpgradeKind.Passive => option.Passive.icon,
                _ => null
            };

            int captured = index;
            Color frame = Palette.Rarity[(int)rarity];
            Button button = _ui.Button(_cardRow, "", frame, () => OnCardClicked(captured), 40);
            Transform face = button.transform.Find("Face");

            Image inner = _ui.Panel(face, Color.Lerp(Palette.Lavender, Palette.Outline, 0.25f), "Inner");
            UIFactory.Place(inner, 0f, 0f, 1f, 1f, 12f);
            inner.raycastTarget = false;

            float textLeft = 0.05f;
            if (icon != null)
            {
                Image well = _ui.Panel(inner.transform, new Color(0.23f, 0.16f, 0.31f, 0.55f), "IconWell");
                UIFactory.Place(well, 0.03f, 0.1f, 0.25f, 0.9f);
                well.raycastTarget = false;
                var img = _ui.Node("Icon", well.transform).gameObject.AddComponent<Image>();
                img.sprite = icon;
                img.preserveAspect = true;
                img.raycastTarget = false;
                UIFactory.Place(img, 0.06f, 0.06f, 0.94f, 0.94f);
                textLeft = 0.29f;
            }

            // Title row: name on the left, a coloured chip on the right saying what this card is (YENİ! / Sv.3 / rarity).
            Text titleText = _ui.Label(inner.transform, title, 48, Palette.Cream, TextAnchor.MiddleLeft);
            UIFactory.Place(titleText, textLeft, 0.66f, 0.72f, 0.95f);
            titleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            titleText.resizeTextForBestFit = true;
            titleText.resizeTextMinSize = 30;
            titleText.resizeTextMaxSize = 48;
            titleText.verticalOverflow = VerticalWrapMode.Truncate;

            // Chip: what KIND of card this is (device feedback: "ana silah mı, güçlendirme mi belli değil"),
            // with the level tag (YENİ! / Sv.3) under it.
            string kind = CardKind(option);
            Image chip = _ui.Panel(inner.transform, kind == "ANA SİLAH" ? Palette.HotPink : option.Kind == UpgradeKind.Weapon ? Palette.Sky : Palette.Mint, "Chip");
            UIFactory.Place(chip, 0.72f, 0.64f, 0.98f, 0.95f);
            chip.raycastTarget = false;
            Text chipText = _ui.Label(chip.transform, kind, 26, Color.white);
            UIFactory.Place(chipText, 0.04f, 0.48f, 0.96f, 0.96f);
            Text tagLine = _ui.Label(chip.transform, tag.Length > 0 ? tag : RarityName(rarity), 24, tag == "YENİ!" ? Palette.Cream : Color.white);
            UIFactory.Place(tagLine, 0.04f, 0.06f, 0.96f, 0.5f);

            // Body: what it does, then what this level adds — never just a name (device feedback 2026-09-24).
            Text bodyText = _ui.Label(inner.transform, body, 34, Palette.White, TextAnchor.UpperLeft);
            UIFactory.Place(bodyText, textLeft, 0.05f, 0.98f, 0.64f);
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.resizeTextForBestFit = true;
            bodyText.resizeTextMinSize = 24;
            bodyText.resizeTextMaxSize = 34;
            bodyText.verticalOverflow = VerticalWrapMode.Truncate;
            return button.gameObject;
        }

        /// <summary>ANA SİLAH (the pilot's main gun and its evolutions), SİLAH, GÜÇLENDİRME (passive) or BONUS.</summary>
        private string CardKind(in UpgradeOption option)
        {
            switch (option.Kind)
            {
                case UpgradeKind.Weapon:
                    for (WeaponDefinition w = inventory.StartingWeapon; w != null; w = w.evolvesInto)
                        if (w == option.Weapon) return "ANA SİLAH";
                    return "SİLAH";
                case UpgradeKind.Passive:
                    return "GÜÇLENDİRME";
                default:
                    return "BONUS";
            }
        }

        private static string RarityName(Rarity r) => r switch
        {
            Rarity.Rare => "NADİR",
            Rarity.Epic => "EPİK",
            Rarity.Legendary => "EFSANE",
            _ => "YAYGIN"
        };

        private void Describe(in UpgradeOption option, out string title, out string body, out Rarity rarity)
        {
            switch (option.Kind)
            {
                case UpgradeKind.Weapon:
                {
                    WeaponDefinition w = option.Weapon;
                    WeaponBehaviour owned = inventory.Find(w);
                    int next = owned != null ? owned.Level + 1 : 1;
                    title = next == 1 ? $"{w.displayName}  YENİ!" : $"{w.displayName}  Sv.{next}";
                    string levelLine = Loc.T($"Seviye {next}: {Loc.T(w.GetStats(next).upgradeText)}");
                    body = next == 1 && !string.IsNullOrEmpty(w.description) ? Loc.T(w.description) + "\n" + levelLine : levelLine;
                    if (next == w.MaxLevel && w.evolutionPassive != null && w.evolvesInto != null)
                        body += "\n" + Loc.T($"EVRİM: Maks. seviye + {Loc.T(w.evolutionPassive.displayName)} » {Loc.T(w.evolvesInto.displayName)}");
                    rarity = option.Rarity;
                    return;
                }
                case UpgradeKind.Passive:
                {
                    PassiveDefinition p = option.Passive;
                    int next = inventory.GetPassiveLevel(p) + 1;
                    title = next == 1 ? $"{p.displayName}  YENİ!" : $"{p.displayName}  Sv.{next}";
                    body = (string.IsNullOrEmpty(p.description) ? "" : Loc.T(p.description) + "\n") +
                           StatLine(p.stat, p.valuePerLevel, next);
                    // The card's price, in red, so the trade-off reads at a glance.
                    if (p.drawbackPerLevel != 0f) body += "\n<color=#FF8A8A>" + StatLine(p.drawbackStat, p.drawbackPerLevel, next) + "</color>";
                    // Say which owned weapon this passive can evolve.
                    foreach (WeaponBehaviour owned in inventory.Weapons)
                        if (owned.Definition.evolutionPassive == p && owned.Definition.evolvesInto != null)
                            body += "\n" + Loc.T($"{Loc.T(owned.Definition.displayName)} evrim anahtarı");
                    rarity = option.Rarity;
                    return;
                }
                default:
                    title = "Şeker Molası";
                    body = "Can +30, can doluysa +50 altın";
                    rarity = Rarity.Common;
                    return;
            }
        }

        /// <summary>"+%15 maks. can (toplam +%30)" — or "+1 mermi (toplam +2)" for flat stats, "−%12 …" for drawbacks.</summary>
        private static string StatLine(StatType stat, float perLevel, int level)
        {
            string name = Loc.T(StatName(stat));
            bool flat = stat is StatType.Armor or StatType.ExtraProjectiles or StatType.Pierce;
            int per = Mathf.RoundToInt(Mathf.Abs(perLevel) * (flat ? 1f : 100f));
            string sign = perLevel < 0f ? "-" : "+";
            return flat
                ? Loc.T($"{sign}{per} {name} (toplam {sign}{per * level})")
                : Loc.T($"{sign}%{per} {name} (toplam {sign}%{per * level})");
        }

        private static string StatName(StatType stat) => stat switch
        {
            StatType.Damage => "hasar",
            StatType.CooldownReduction => "atış hızı",
            StatType.Area => "alan",
            StatType.Duration => "süre",
            StatType.ProjectileSpeed => "mermi hızı",
            StatType.CritChance => "kritik şansı",
            StatType.MaxHp => "maks. can",
            StatType.MagnetRadius => "mıknatıs",
            StatType.Armor => "zırh",
            StatType.ExtraProjectiles => "mermi",
            StatType.Pierce => "delme",
            StatType.CritDamage => "kritik hasarı",
            StatType.LowHpDamage => "düşük canda hasar",
            StatType.RushGain => "şeker dolumu",
            StatType.GoldGain => "altın",
            StatType.Experience => "tecrübe",
            StatType.Luck => "şans",
            _ => stat.ToString()
        };

        private void OnCardClicked(int index)
        {
            if (_banishMode) run.Banish(index);
            else run.ChooseUpgrade(index);
        }

        private void ToggleBanish()
        {
            _banishMode = !_banishMode;
            UIFactory.SetText(_banishButton, _banishMode ? "Kart seç…" : $"Yasakla ({run.BanishesLeft})");
        }

        // ---------------------------------------------------------------- Pause & death

        private void BuildPause(Transform root)
        {
            GameObject screen = MakeScreen(root, GameState.Paused);
            Text title = _ui.Label(screen.transform, "Mola", 120, Palette.Cream);
            UIFactory.Place(title, 0.1f, 0.62f, 0.9f, 0.72f);
            UIFactory.Place(_ui.Button(screen.transform, "Devam Et", Palette.HotPink, run.Resume), 0.15f, 0.48f, 0.85f, 0.56f);
            // Leave now, continue later exactly here (run-resume.md).
            UIFactory.Place(_ui.Button(screen.transform, "Kaydet ve Çık", Palette.Honey, run.SaveAndQuit, 52), 0.2f, 0.385f, 0.8f, 0.45f);
            UIFactory.Place(_ui.Button(screen.transform, "Koşuyu Bitir", Palette.Lavender, run.Abandon, 48), 0.25f, 0.305f, 0.75f, 0.36f);
            UIFactory.Place(_ui.Button(screen.transform, "Ayarlar", Palette.Mint, OpenSettings, 48), 0.25f, 0.225f, 0.75f, 0.28f);
        }

        private void BuildDeath(Transform root)
        {
            GameObject screen = MakeScreen(root, GameState.Dead);
            Text title = _ui.Label(screen.transform, "Pıtır düştü!", 110, Palette.Cream);
            UIFactory.Place(title, 0.05f, 0.64f, 0.95f, 0.74f);

            _reviveButton = _ui.Button(screen.transform, "Diril", Palette.Mint, () => run.Revive());
            UIFactory.Place(_reviveButton, 0.15f, 0.5f, 0.85f, 0.58f);
            // Optional rewarded-ad continue; never mandatory (game-concept.md §3.5). Hook an ad SDK here.
            _adReviveButton = _ui.Button(screen.transform, "Reklam İzle, Devam Et", Palette.Honey, WatchForRevive, 50);
            UIFactory.Place(_adReviveButton, 0.15f, 0.39f, 0.85f, 0.46f);
            UIFactory.Place(_ui.Button(screen.transform, "Bitir", Palette.Lavender, run.GiveUp, 52), 0.3f, 0.28f, 0.7f, 0.34f);
        }

        private void RefreshDeath()
        {
            UIFactory.SetText(_reviveButton, $"Diril ({run.RevivesLeft})");
            _reviveButton.gameObject.SetActive(run.RevivesLeft > 0);
            _adReviveButton.gameObject.SetActive(run.FreeReviveAvailable && RewardedAds.IsReady);
        }

        // ---------------------------------------------------------------- Run end

        private void BuildRunEnd(Transform root)
        {
            GameObject screen = MakeScreen(root, GameState.RunEnd);
            _endTitle = _ui.Label(screen.transform, "", 110, Palette.Cream);
            UIFactory.Place(_endTitle, 0.05f, 0.76f, 0.95f, 0.86f);
            _endStats = _ui.Label(screen.transform, "", 52, Palette.White);
            UIFactory.Place(_endStats, 0.08f, 0.6f, 0.92f, 0.75f);
            // Reward line as icons: [coin] +123   [star] +3.
            RectTransform reward = CurrencyRow(screen.transform, TextAnchor.MiddleCenter);
            UIFactory.Place(reward, 0.08f, 0.52f, 0.92f, 0.6f);
            _endGold = AddAmount(reward, coinIcon, "+0", 72, Palette.Honey);
            _endDustGap = _ui.Node("Gap", reward).gameObject;
            _endDustGap.AddComponent<LayoutElement>().preferredWidth = 50f;
            _endDust = AddAmount(reward, stardustIcon, "", 72, Palette.Hex(0xC8B6FF));
            _endDustIcon = reward.GetChild(reward.childCount - 2).gameObject;
            _endGoal = _ui.Label(screen.transform, "", 36, Palette.Mint);
            UIFactory.Place(_endGoal, 0.08f, 0.47f, 0.92f, 0.52f);

            // One big call to action (game-concept.md §4.5 step 4).
            Button again = _ui.Button(screen.transform, "Tekrar Oyna", Palette.HotPink, () => AfterRunAd(() =>
            {
                run.StartEndless();
            }), 80);
            UIFactory.Place(again, 0.1f, 0.22f, 0.9f, 0.33f);
            _upgradeButton = _ui.Button(screen.transform, "Geliştir", Palette.Lavender, OpenWorkshopFromEnd, 48);
            UIFactory.Place(_upgradeButton, 0.08f, 0.12f, 0.48f, 0.19f);
            // Back to the menu to look around (pilots, missions, gift) — device feedback 2026-09-25.
            Button home = _ui.Button(screen.transform, "Ana Menü", Palette.Outline, () => AfterRunAd(run.EnterMenu), 48);
            UIFactory.Place(home, 0.52f, 0.12f, 0.92f, 0.19f);
            _endlessButton = _ui.Button(screen.transform, "Sonsuz Mod'a Devam (+%25 altın)", Palette.Honey, run.ContinueEndless, 46);
            UIFactory.Place(_endlessButton, 0.12f, 0.345f, 0.88f, 0.395f);
            BuildAdPlacements(screen.transform);
        }

        private void OnRunEnded(RunSummary s)
        {
            _endTitle.text = Loc.T(s.NewRecord ? "YENİ REKOR!" : run.IsEndlessRun ? "Güzel uçuş!" : s.Endless ? "Sonsuz Mod bitti!" : s.Victory ? "Zafer!" : "Az kaldı!");
            _endlessButton.gameObject.SetActive(run.CanContinueEndless);
            int minutes = Mathf.FloorToInt(s.Minutes);
            int seconds = Mathf.FloorToInt((s.Minutes - minutes) * 60f);
            _endStats.text = Loc.T($"Seviye {s.Level}   ·   {s.Kills} düşman   ·   {minutes}:{seconds:00}");
            _endDust.text = $"+{s.Stardust}";
            foreach (GameObject go in new[] { _endDust.gameObject, _endDustIcon, _endDustGap }) go.SetActive(s.Stardust > 0);
            if (run.IsEndlessRun)
            {
                float best = run.Meta.BestEndlessSeconds;
                _endStats.text += "\n" + Loc.T($"Bölüm {waveDirector.StageNumber}") + "   ·   " +
                                  Loc.T($"Rekor: {Mathf.FloorToInt(best / 60f)}:{Mathf.FloorToInt(best % 60f):00}");
            }

            _goldShown = 0f;
            _goldTarget = s.Gold;
            _endGold.text = "+0";
            _endGoal.text = NextGoalText();

            bool affordable = run.Meta.AnyAffordable(run.Workshop) || run.Meta.CanUpgradeForge(ForgeTrack.Power);
            UIFactory.SetText(_upgradeButton, affordable ? "Geliştir  !" : "Geliştir");
            OnRunEndedAds(s);
        }

        /// <summary>"Unfinished business" line (game-concept.md §4.2): the cheapest next upgrade.</summary>
        private string NextGoalText()
        {
            MetaProgressionService meta = run.Meta;
            MetaUpgradeDefinition best = null;
            int bestCost = int.MaxValue;
            foreach (MetaUpgradeDefinition u in run.Workshop)
            {
                if (meta.IsMaxed(u)) continue;
                int cost = u.CostForNext(meta.GetLevel(u));
                if (cost < bestCost)
                {
                    bestCost = cost;
                    best = u;
                }
            }
            string line = "";
            if (best != null)
            {
                long missing = bestCost - meta.Gold;
                line = missing <= 0
                    ? Loc.T($"{best.displayName} geliştirmesi hazır!")
                    : Loc.T($"{best.displayName} için {missing} altın kaldı");
            }
            // The next pilot as a visible goal (retention.md): how close the gold is, in percent.
            CharacterDefinition next = NextPilot();
            if (next != null && next.goldCost > 0)
            {
                int pct = Mathf.Clamp(Mathf.FloorToInt(100f * meta.Gold / next.goldCost), 0, 100);
                line += (line.Length > 0 ? "\n" : "") + Loc.T($"Sonraki pilot: {Loc.T(next.displayName)} %{pct}");
            }
            return line;
        }

        private void OpenWorkshopFromEnd()
        {
            run.EnterMenu();
            OpenWorkshop();
        }

        private void RefreshMenu()
        {
            RefreshWallet();
            RefreshPilot();
        }

        private void RefreshWallet()
        {
            SetWallet(_wallet);
            RefreshForge();
            if (_workshop != null && _workshop.activeSelf) RefreshWorkshop();
            RefreshOpenMetaScreen();
        }

        // ---------------------------------------------------------------- Workshop

        private void BuildWorkshop(Transform root)
        {
            _workshop = _ui.Node("Workshop", root).gameObject;
            _ui.Panel(_workshop.transform, Palette.Lavender, "Background");

            Text title = _ui.Label(_workshop.transform, "Atölye", 110, Palette.Cream);
            UIFactory.Place(title, 0.05f, 0.88f, 0.95f, 0.96f);

            _workshopList = _ui.Node("List", _workshop.transform);
            UIFactory.Place(_workshopList, 0.04f, 0.14f, 0.96f, 0.86f);
            var layout = _workshopList.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 18f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            UIFactory.Place(_ui.Button(_workshop.transform, "Geri", Palette.HotPink, CloseWorkshop, 64), 0.25f, 0.03f, 0.75f, 0.1f);
            _workshop.SetActive(false);
        }

        private void OpenWorkshop() => OpenResearch(); // the old Atölye lives inside Ar-Ge now

        private void OpenWorkshopLegacy()
        {
            _workshop.SetActive(true);
            _menu.SetActive(false);
            RefreshWorkshop();
        }

        private void CloseWorkshop()
        {
            _workshop.SetActive(false);
            _menu.SetActive(true);
            RefreshWallet();
        }

        private void RefreshWorkshop()
        {
            for (int i = _workshopList.childCount - 1; i >= 0; i--)
            {
                GameObject child = _workshopList.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            MetaProgressionService meta = run.Meta;
            foreach (MetaUpgradeDefinition u in run.Workshop)
            {
                int level = meta.GetLevel(u);
                bool maxed = meta.IsMaxed(u);

                Image row = _ui.Panel(_workshopList, Palette.Outline, u.id);
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 150f;

                Text name = _ui.Label(row.transform, $"{u.displayName}  {level}/{u.maxLevel}", 50, Palette.White, TextAnchor.MiddleLeft);
                UIFactory.Place(name, 0.04f, 0.1f, 0.62f, 0.9f);

                MetaUpgradeDefinition captured = u;
                string label = maxed ? "MAKS" : $"{u.CostForNext(level)}";
                Button buy = _ui.Button(row.transform, label, maxed ? Palette.Lavender : Palette.Honey, () =>
                {
                    if (meta.TryPurchase(captured)) RefreshWorkshop();
                }, 50);
                UIFactory.Place(buy, 0.64f, 0.14f, 0.97f, 0.86f);
                buy.interactable = meta.CanAfford(u);
            }
            SetWallet(_wallet);
        }

        // ---------------------------------------------------------------- Helpers

        private GameObject MakeScreen(Transform root, GameState state, bool dim = true)
        {
            RectTransform node = _ui.Node(state.ToString(), root);
            if (dim) _ui.Dimmer(node);
            _screens[state] = node.gameObject;
            node.gameObject.SetActive(false);
            return node.gameObject;
        }
    }
}
