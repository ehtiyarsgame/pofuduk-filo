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
    public sealed class GameUI : MonoBehaviour
    {
        [SerializeField] private RunController run;
        [SerializeField] private XpSystem xpSystem;
        [SerializeField] private WeaponInventory inventory;
        [SerializeField] private PlayerHealth player;
        [SerializeField] private WaveDirector waveDirector;
        [SerializeField] private PickupSystem pickups;
        [SerializeField] private Font font;
        [SerializeField] private Sprite roundedSprite;

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
            _ui = new UIFactory(font, roundedSprite);

            Canvas canvas = _ui.Canvas("GameUI", 10);
            RectTransform safe = _ui.SafeArea(canvas.transform);

            BuildHud(safe);
            BuildLevelUp(safe);
            BuildPause(safe);
            BuildDeath(safe);
            BuildRunEnd(safe);
            BuildMenu(safe);
            BuildWorkshop(safe);

            run.StateChanged += OnStateChanged;
            run.LevelUpOffered += OnLevelUpOffered;
            run.RunEnded += OnRunEnded;
            run.ChestOpened += OnChestOpened;
            run.Meta.WalletChanged += RefreshWallet;
            xpSystem.XpChanged += OnXpChanged;
            player.HealthChanged += OnHealthChanged;
            pickups.RunGoldChanged += g => _goldText.text = $"{g}";
            waveDirector.BossSpawned += OnBossSpawned;
            waveDirector.FormationCleared += _ => Toast("Formasyon Temizlendi!");
            waveDirector.PhaseStarted += p => { if (p.kind != PhaseKind.Waves) Toast(p.label); };
            inventory.WeaponEvolved += (from, to) => Toast($"EVRİM! {to.displayName}");

            OnStateChanged(run.State);
        }

        private void Update()
        {
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
                _endGold.text = $"+{Mathf.RoundToInt(_goldShown)} altın";
            }
        }

        // ---------------------------------------------------------------- State routing

        private void OnStateChanged(GameState state)
        {
            foreach (KeyValuePair<GameState, GameObject> pair in _screens)
                pair.Value.SetActive(pair.Key == state);

            _hud.SetActive(state is GameState.Playing or GameState.LevelUp or GameState.Paused or GameState.Dead);
            _workshop.SetActive(false);

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

            _toast = _ui.Label(_hud.transform, "", 80, Palette.Cream);
            UIFactory.Place(_toast, 0.05f, 0.6f, 0.95f, 0.7f);
            _toast.gameObject.SetActive(false);
        }

        private void OnXpChanged(int current, int required)
        {
            _xpFill.fillAmount = required > 0 ? current / (float)required : 0f;
            _levelText.text = $"Sv. {xpSystem.Level}";
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
            _toast.text = text;
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
            _levelUpTitle.text = $"Seviye {xpSystem.Level}!";
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

            int captured = index;
            Button button = _ui.Button(_cardRow, "", Palette.Rarity[(int)rarity], () => OnCardClicked(captured), 40);
            Transform face = button.transform.Find("Face");

            Image inner = _ui.Panel(face, Palette.Lavender, "Inner");
            UIFactory.Place(inner, 0f, 0f, 1f, 1f, 12f);
            inner.raycastTarget = false;

            Text titleText = _ui.Label(inner.transform, title, 60, Palette.Cream, TextAnchor.UpperLeft);
            UIFactory.Place(titleText, 0.05f, 0.5f, 0.95f, 0.92f);
            Text bodyText = _ui.Label(inner.transform, body, 42, Palette.White, TextAnchor.UpperLeft);
            UIFactory.Place(bodyText, 0.05f, 0.06f, 0.95f, 0.5f);
            return button.gameObject;
        }

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
        }

        private void BuildDeath(Transform root)
        {
            GameObject screen = MakeScreen(root, GameState.Dead);
            Text title = _ui.Label(screen.transform, "Pıtır düştü!", 110, Palette.Cream);
            UIFactory.Place(title, 0.05f, 0.64f, 0.95f, 0.74f);

            _reviveButton = _ui.Button(screen.transform, "Diril", Palette.Mint, () => run.Revive());
            UIFactory.Place(_reviveButton, 0.15f, 0.5f, 0.85f, 0.58f);
            // Optional rewarded-ad continue; never mandatory (game-concept.md §3.5). Hook an ad SDK here.
            _adReviveButton = _ui.Button(screen.transform, "Reklam İzle, Devam Et", Palette.Honey, () => run.Revive(free: true), 50);
            UIFactory.Place(_adReviveButton, 0.15f, 0.39f, 0.85f, 0.46f);
            UIFactory.Place(_ui.Button(screen.transform, "Bitir", Palette.Lavender, run.GiveUp, 52), 0.3f, 0.28f, 0.7f, 0.34f);
        }

        private void RefreshDeath()
        {
            UIFactory.SetText(_reviveButton, $"Diril ({run.RevivesLeft})");
            _reviveButton.gameObject.SetActive(run.RevivesLeft > 0);
            _adReviveButton.gameObject.SetActive(run.FreeReviveAvailable);
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
            UIFactory.Place(_endGold, 0.08f, 0.5f, 0.92f, 0.58f);
            _endGoal = _ui.Label(screen.transform, "", 46, Palette.Mint);
            UIFactory.Place(_endGoal, 0.08f, 0.4f, 0.92f, 0.49f);

            // One big call to action (game-concept.md §4.5 step 4).
            Button again = _ui.Button(screen.transform, "Tekrar Oyna", Palette.HotPink, () => run.StartRun(run.ChapterIndex), 80);
            UIFactory.Place(again, 0.1f, 0.22f, 0.9f, 0.33f);
            _upgradeButton = _ui.Button(screen.transform, "Geliştir", Palette.Lavender, OpenWorkshopFromEnd, 52);
            UIFactory.Place(_upgradeButton, 0.3f, 0.12f, 0.7f, 0.19f);
        }

        private void OnRunEnded(RunSummary s)
        {
            _endTitle.text = s.Victory ? "Zafer!" : "Az kaldı!";
            int minutes = Mathf.FloorToInt(s.Minutes);
            int seconds = Mathf.FloorToInt((s.Minutes - minutes) * 60f);
            _endStats.text = $"Seviye {s.Level}   ·   {s.Kills} düşman   ·   {minutes}:{seconds:00}";
            if (s.Stardust > 0) _endStats.text += $"\n+{s.Stardust} Yıldız Tozu";

            _goldShown = 0f;
            _goldTarget = s.Gold;
            _endGold.text = "+0 altın";
            _endGoal.text = NextGoalText();

            bool affordable = run.Meta.AnyAffordable(run.Workshop);
            UIFactory.SetText(_upgradeButton, affordable ? "Geliştir  !" : "Geliştir");
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

        private void BuildMenu(Transform root)
        {
            _menu = MakeScreen(root, GameState.MainMenu, dim: false);

            Text title = _ui.Label(_menu.transform, "Pofuduk Filo", 140, Palette.Cream);
            UIFactory.Place(title, 0.05f, 0.72f, 0.95f, 0.86f);

            _walletText = _ui.Label(_menu.transform, "", 56, Palette.Honey);
            UIFactory.Place(_walletText, 0.05f, 0.64f, 0.95f, 0.7f);

            UIFactory.Place(_ui.Button(_menu.transform, "<", Palette.Lavender, () => SelectChapter(-1)), 0.06f, 0.5f, 0.2f, 0.57f);
            UIFactory.Place(_ui.Button(_menu.transform, ">", Palette.Lavender, () => SelectChapter(1)), 0.8f, 0.5f, 0.94f, 0.57f);
            _chapterText = _ui.Label(_menu.transform, "", 60, Palette.White);
            UIFactory.Place(_chapterText, 0.22f, 0.5f, 0.78f, 0.57f);

            _playButton = _ui.Button(_menu.transform, "OYNA", Palette.HotPink, () => run.StartRun(_selectedChapter), 110);
            UIFactory.Place(_playButton, 0.12f, 0.33f, 0.88f, 0.46f);
            UIFactory.Place(_ui.Button(_menu.transform, "Atölye", Palette.Mint, OpenWorkshop, 64), 0.25f, 0.2f, 0.75f, 0.28f);
        }

        private void RefreshMenu()
        {
            _selectedChapter = Mathf.Clamp(_selectedChapter, 0, Mathf.Max(0, run.Chapters.Count - 1));
            RefreshWallet();
            SelectChapter(0);
        }

        private void RefreshWallet()
        {
            if (_walletText != null) _walletText.text = $"{run.Meta.Gold} altın   ·   {run.Meta.Stardust} Yıldız Tozu";
            if (_workshop != null && _workshop.activeSelf) RefreshWorkshop();
        }

        private void SelectChapter(int delta)
        {
            int count = run.Chapters.Count;
            if (count == 0) return;
            _selectedChapter = Mathf.Clamp(_selectedChapter + delta, 0, count - 1);
            bool unlocked = run.IsChapterUnlocked(_selectedChapter);
            _chapterText.text = unlocked ? $"Bölüm {_selectedChapter + 1}" : $"Bölüm {_selectedChapter + 1}  (kilitli)";
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
            if (_walletText != null) _walletText.text = $"{meta.Gold} altın   ·   {meta.Stardust} Yıldız Tozu";
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
