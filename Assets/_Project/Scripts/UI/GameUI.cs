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
        private Image _xpFill;
        private Text _levelText;
        private Image _hpFill;
        private Text _goldText;
        private Image _bossFill;
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
        private Text _walletText;
        private Text _chapterText;
        private Button _playButton;
        private int _selectedChapter;
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
            waveDirector.PhaseStarted += p => { if (p.kind != PhaseKind.Waves) Toast(p.label); };
            inventory.WeaponEvolved += (from, to) => Toast($"EVRİM! {to.displayName}");

            OnStateChanged(run.State);
        }

        private void Update()
        {
            UpdateRushHud();
            UpdateMenuAnim();
            if (_toast != null && _toast.gameObject.activeSelf && Time.unscaledTime > _toastUntil)
                _toast.gameObject.SetActive(false);

            if (_bossBar != null && _bossBar.activeSelf)
            {
                if (_boss == null || _boss.IsDead || !_boss.gameObject.activeInHierarchy) _bossBar.SetActive(false);
                else _bossFill.fillAmount = _boss.HpFraction;
            }

            // Run-end reward counter "clinks" up (game-concept.md §4.5 step 2).
            if (_endGold != null && _goldShown < _goldTarget)
            {
                _goldShown = Mathf.MoveTowards(_goldShown, _goldTarget, Mathf.Max(60f, _goldTarget) * Time.unscaledDeltaTime);
                _endGold.text = Loc.T($"+{Mathf.RoundToInt(_goldShown)} altın");
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

            if (state == GameState.MainMenu) RefreshMenu();
            if (state == GameState.Dead) RefreshDeath();
        }

        // ---------------------------------------------------------------- HUD

        private void BuildHud(Transform root)
        {
            _hud = _ui.Node("HUD", root).gameObject;

            // Thin XP bar across the top (art-bible §4: minimal HUD, nothing in the thumb zone).
            _xpFill = _ui.Bar(_hud.transform, Palette.Outline, Palette.Sky);
            UIFactory.Place(_xpFill.transform.parent, 0.02f, 0.975f, 0.98f, 0.995f);
            _levelText = _ui.Label(_hud.transform, "Sv. 1", 44, Palette.White);
            UIFactory.Place(_levelText, 0.38f, 0.935f, 0.62f, 0.972f);

            _hpFill = _ui.Bar(_hud.transform, Palette.Outline, Palette.HotPink);
            UIFactory.Place(_hpFill.transform.parent, 0.03f, 0.94f, 0.33f, 0.965f);

            _goldText = _ui.Label(_hud.transform, "0", 44, Palette.Honey, TextAnchor.MiddleLeft);
            UIFactory.Place(_goldText, 0.03f, 0.905f, 0.33f, 0.938f);

            Button pause = _ui.Button(_hud.transform, "II", Palette.Lavender, run.Pause, 56);
            UIFactory.Place(pause, 0.84f, 0.925f, 0.97f, 0.97f);

            _bossBar = _ui.Node("BossBar", _hud.transform).gameObject;
            UIFactory.Place(_bossBar.GetComponent<RectTransform>(), 0.1f, 0.87f, 0.9f, 0.895f);
            _bossFill = _ui.Bar(_bossBar.transform, Palette.Outline, Palette.Coral);
            _bossBar.SetActive(false);

            BuildRushHud(_hud.transform);

            _toast = _ui.Label(_hud.transform, "", 80, Palette.Cream);
            UIFactory.Place(_toast, 0.05f, 0.6f, 0.95f, 0.7f);
            _toast.gameObject.SetActive(false);
        }

        private void OnXpChanged(int current, int required)
        {
            _xpFill.fillAmount = required > 0 ? current / (float)required : 0f;
            _levelText.text = Loc.T($"Sv. {xpSystem.Level}");
        }

        private void OnHealthChanged(float current, float max) => _hpFill.fillAmount = max > 0f ? current / max : 0f;

        private void OnBossSpawned(Enemy boss)
        {
            _boss = boss;
            _bossBar.SetActive(true);
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

            Text titleText = _ui.Label(inner.transform, title, 52, Palette.Cream, TextAnchor.UpperLeft);
            UIFactory.Place(titleText, textLeft, 0.58f, 0.72f, 0.92f);
            titleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            titleText.resizeTextForBestFit = true;
            titleText.resizeTextMinSize = 34;
            titleText.resizeTextMaxSize = 52;
            titleText.verticalOverflow = VerticalWrapMode.Truncate;
            Text tagText = _ui.Label(inner.transform, tag, 40, tag == "YENİ!" ? Palette.Mint : Palette.Honey, TextAnchor.UpperLeft);
            UIFactory.Place(tagText, textLeft, 0.42f, 0.97f, 0.6f);
            Text bodyText = _ui.Label(inner.transform, body, 38, Palette.White, TextAnchor.UpperLeft);
            UIFactory.Place(bodyText, textLeft, 0.05f, 0.97f, 0.43f);

            Image chip = _ui.Panel(inner.transform, frame, "Rarity");
            UIFactory.Place(chip, 0.72f, 0.8f, 0.98f, 0.98f);
            chip.raycastTarget = false;
            Text chipText = _ui.Label(chip.transform, RarityName(rarity), 30, Palette.Outline);
            chipText.GetComponent<Outline>().enabled = false;
            return button.gameObject;
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
                    WeaponBehaviour owned = inventory.Find(option.Weapon);
                    int next = owned != null ? owned.Level + 1 : 1;
                    title = next == 1 ? $"{option.Weapon.displayName}  YENİ!" : $"{option.Weapon.displayName}  Sv.{next}";
                    body = option.Weapon.GetStats(next).upgradeText;
                    if (next == option.Weapon.MaxLevel && option.Weapon.evolutionPassive != null)
                        body += $"\nEvrim anahtarı: {option.Weapon.evolutionPassive.displayName}";
                    rarity = option.Rarity;
                    return;
                }
                case UpgradeKind.Passive:
                {
                    int next = inventory.GetPassiveLevel(option.Passive) + 1;
                    title = next == 1 ? $"{option.Passive.displayName}  YENİ!" : $"{option.Passive.displayName}  Sv.{next}";
                    body = $"+%{Mathf.RoundToInt(option.Passive.valuePerLevel * 100f)} {StatName(option.Passive.stat)}";
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
            UIFactory.Place(_ui.Button(screen.transform, "Devam Et", Palette.HotPink, run.Resume), 0.15f, 0.46f, 0.85f, 0.54f);
            UIFactory.Place(_ui.Button(screen.transform, "Koşuyu Bitir", Palette.Lavender, run.Abandon, 52), 0.25f, 0.34f, 0.75f, 0.4f);
            UIFactory.Place(_ui.Button(screen.transform, "Ayarlar", Palette.Mint, OpenSettings, 52), 0.25f, 0.25f, 0.75f, 0.31f);
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
            _endGold = _ui.Label(screen.transform, "", 72, Palette.Honey);
            UIFactory.Place(_endGold, 0.08f, 0.52f, 0.92f, 0.6f);
            _endGoal = _ui.Label(screen.transform, "", 46, Palette.Mint);
            UIFactory.Place(_endGoal, 0.08f, 0.47f, 0.92f, 0.52f);

            // One big call to action (game-concept.md §4.5 step 4).
            Button again = _ui.Button(screen.transform, "Tekrar Oyna", Palette.HotPink, () => AfterRunAd(() => run.StartRun(run.ChapterIndex)), 80);
            UIFactory.Place(again, 0.1f, 0.22f, 0.9f, 0.33f);
            _upgradeButton = _ui.Button(screen.transform, "Geliştir", Palette.Lavender, OpenWorkshopFromEnd, 52);
            UIFactory.Place(_upgradeButton, 0.3f, 0.12f, 0.7f, 0.19f);
            _endlessButton = _ui.Button(screen.transform, "Sonsuz Mod'a Devam (+%50 altın)", Palette.Honey, run.ContinueEndless, 46);
            UIFactory.Place(_endlessButton, 0.12f, 0.345f, 0.88f, 0.395f);
            BuildAdPlacements(screen.transform);
        }

        private void OnRunEnded(RunSummary s)
        {
            _endTitle.text = Loc.T(s.Endless ? "Sonsuz Mod bitti!" : s.Victory ? "Zafer!" : "Az kaldı!");
            _endlessButton.gameObject.SetActive(run.CanContinueEndless);
            int minutes = Mathf.FloorToInt(s.Minutes);
            int seconds = Mathf.FloorToInt((s.Minutes - minutes) * 60f);
            _endStats.text = Loc.T($"Seviye {s.Level}   ·   {s.Kills} düşman   ·   {minutes}:{seconds:00}");
            if (s.Stardust > 0) _endStats.text += Loc.T($"\n+{s.Stardust} Yıldız Tozu");

            _goldShown = 0f;
            _goldTarget = s.Gold;
            _endGold.text = Loc.T("+0 altın");
            _endGoal.text = Loc.T(NextGoalText());

            bool affordable = run.Meta.AnyAffordable(run.Workshop);
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
            if (best == null) return "";
            long missing = bestCost - meta.Gold;
            return missing <= 0
                ? $"{best.displayName} geliştirmesi hazır!"
                : $"{best.displayName} için {missing} altın kaldı";
        }

        private void OpenWorkshopFromEnd()
        {
            run.EnterMenu();
            OpenWorkshop();
        }

        // ---------------------------------------------------------------- Main menu

        private Image _heroShip;
        private Text _titleTop;
        private Text _titleBottom;

        private void BuildMenu(Transform root)
        {
            _menu = MakeScreen(root, GameState.MainMenu, dim: false);

            // Top bar: wallet left, settings right.
            Image bar = _ui.Panel(_menu.transform, new Color(0.23f, 0.16f, 0.31f, 0.7f), "TopBar");
            UIFactory.Place(bar, 0.02f, 0.925f, 0.98f, 0.985f);
            _walletText = _ui.Label(bar.transform, "", 44, Palette.Honey, TextAnchor.MiddleLeft);
            UIFactory.Place(_walletText, 0.04f, 0f, 0.7f, 1f);
            UIFactory.Place(_ui.Button(bar.transform, "Ayarlar", Palette.Lavender, OpenSettings, 38), 0.72f, 0.1f, 0.98f, 0.9f);

            // Two-tone logo that bobs gently (UpdateMenuAnim).
            _titleTop = _ui.Label(_menu.transform, "POFUDUK", 150, Palette.Pink);
            UIFactory.Place(_titleTop, 0.02f, 0.8f, 0.98f, 0.9f);
            _titleBottom = _ui.Label(_menu.transform, "FİLO", 170, Palette.Cream);
            UIFactory.Place(_titleBottom, 0.02f, 0.71f, 0.98f, 0.81f);
            foreach (Text t in new[] { _titleTop, _titleBottom })
            {
                var o = t.GetComponent<Outline>();
                o.effectDistance = new Vector2(6f, -6f);
                var shadow = t.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0.1f, 0.05f, 0.18f, 0.6f);
                shadow.effectDistance = new Vector2(0f, -14f);
            }

            // Hero: the selected pilot's ship.
            _heroShip = _ui.Node("HeroShip", _menu.transform).gameObject.AddComponent<Image>();
            _heroShip.preserveAspect = true;
            _heroShip.raycastTarget = false;
            UIFactory.Place(_heroShip, 0.25f, 0.5f, 0.75f, 0.7f);

            _pilotText = _ui.Label(_menu.transform, "", 46, Palette.Pink);
            UIFactory.Place(_pilotText, 0.1f, 0.47f, 0.9f, 0.51f);

            UIFactory.Place(_ui.Button(_menu.transform, "<", Palette.Lavender, () => SelectChapter(-1)), 0.08f, 0.41f, 0.22f, 0.465f);
            UIFactory.Place(_ui.Button(_menu.transform, ">", Palette.Lavender, () => SelectChapter(1)), 0.78f, 0.41f, 0.92f, 0.465f);
            _chapterText = _ui.Label(_menu.transform, "", 60, Palette.White);
            UIFactory.Place(_chapterText, 0.22f, 0.41f, 0.78f, 0.465f);

            _playButton = _ui.Button(_menu.transform, "OYNA", Palette.HotPink, () => run.StartRun(_selectedChapter), 120);
            UIFactory.Place(_playButton, 0.12f, 0.27f, 0.88f, 0.39f);
            UIFactory.Place(_ui.Button(_menu.transform, "Atölye", Palette.Mint, OpenWorkshop, 64), 0.25f, 0.18f, 0.75f, 0.25f);

            // Meta hub row (meta-economy.md §3.3 B–D).
            UIFactory.Place(_ui.Button(_menu.transform, "Hangar", Palette.Lavender, OpenHangar, 48), 0.04f, 0.08f, 0.34f, 0.15f);
            UIFactory.Place(_ui.Button(_menu.transform, "Laboratuvar", Palette.Lavender, OpenLab, 44), 0.35f, 0.08f, 0.65f, 0.15f);
            UIFactory.Place(_ui.Button(_menu.transform, "Takımyıldız", Palette.Lavender, OpenConstellation, 44), 0.66f, 0.08f, 0.96f, 0.15f);
        }

        private void UpdateMenuAnim()
        {
            if (_menu == null || !_menu.activeSelf || _heroShip == null) return;
            float t = Time.unscaledTime;
            _heroShip.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Sin(t * 1.8f) * 18f);
            _heroShip.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.1f) * 4f);
            float s = 1f + Mathf.Sin(t * 2.4f) * 0.02f;
            _titleTop.rectTransform.localScale = new Vector3(s, s, 1f);
            _titleBottom.rectTransform.localScale = new Vector3(2f - s, 2f - s, 1f);
            _playButton.transform.localScale = Vector3.one * (1f + Mathf.Max(0f, Mathf.Sin(t * 3f)) * 0.04f);
        }

        private void RefreshMenu()
        {
            _selectedChapter = Mathf.Clamp(_selectedChapter, 0, Mathf.Max(0, run.Chapters.Count - 1));
            RefreshWallet();
            SelectChapter(0);
            RefreshPilot();
        }

        private void RefreshWallet()
        {
            if (_walletText != null) _walletText.text = Loc.T($"{run.Meta.Gold} altın   ·   {run.Meta.Stardust} Yıldız Tozu");
            if (_workshop != null && _workshop.activeSelf) RefreshWorkshop();
            RefreshOpenMetaScreen();
        }

        private void SelectChapter(int delta)
        {
            int count = run.Chapters.Count;
            if (count == 0) return;
            _selectedChapter = Mathf.Clamp(_selectedChapter + delta, 0, count - 1);
            bool unlocked = run.IsChapterUnlocked(_selectedChapter);
            _chapterText.text = Loc.T(unlocked ? $"Bölüm {_selectedChapter + 1}" : $"Bölüm {_selectedChapter + 1}  (kilitli)");
            _playButton.interactable = unlocked;
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

        private void OpenWorkshop()
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
            if (_walletText != null) _walletText.text = Loc.T($"{meta.Gold} altın   ·   {meta.Stardust} Yıldız Tozu");
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
