using System;
using System.Collections.Generic;
using PofudukFilo.Bullets;
using PofudukFilo.Enemies;
using PofudukFilo.Feel;
using PofudukFilo.Meta;
using PofudukFilo.Player;
using PofudukFilo.Progression;
using PofudukFilo.Weapons;
using UnityEngine;

namespace PofudukFilo.Core
{
    public enum GameState
    {
        MainMenu,
        Playing,
        LevelUp,
        Paused,
        Dead,
        RunEnd
    }

    public readonly struct RunSummary
    {
        public readonly bool Victory;
        public readonly int Gold;
        public readonly int Stardust;
        public readonly int Level;
        public readonly int Kills;
        public readonly float Minutes;
        public readonly int ChapterIndex;
        public readonly bool Endless;
        /// <summary>Sonsuz Mod run that beat the time record.</summary>
        public readonly bool NewRecord;

        public RunSummary(bool victory, int gold, int stardust, int level, int kills, float minutes, int chapterIndex,
            bool endless = false, bool newRecord = false)
        {
            Endless = endless;
            NewRecord = newRecord;
            Victory = victory;
            Gold = gold;
            Stardust = stardust;
            Level = level;
            Kills = kills;
            Minutes = minutes;
            ChapterIndex = chapterIndex;
        }
    }

    /// <summary>
    /// Game flow state machine: menu → run → level-up drafts → death/revive → run end → rewards.
    /// UI never touches gameplay systems directly; it listens to these events and calls the
    /// public methods (game-concept.md §3.4–3.5, §4.5; meta-economy.md).
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class RunController : MonoBehaviour
    {
        public static RunController Instance { get; private set; }

        [Header("Systems")]
        [SerializeField] private WaveDirector waveDirector;
        [SerializeField] private XpSystem xpSystem;
        [SerializeField] private UpgradeDraft draft;
        [SerializeField] private WeaponInventory inventory;
        [SerializeField] private PickupSystem pickups;
        [SerializeField] private PlayerHealth player;
        [SerializeField] private Juice juice;
        [SerializeField] private SpriteRenderer playerSprite;

        [Header("Content")]
        [SerializeField] private RunDefinition[] chapters = Array.Empty<RunDefinition>();
        [SerializeField] private MetaUpgradeDefinition[] workshop = Array.Empty<MetaUpgradeDefinition>();
        [SerializeField] private CharacterDefinition[] characters = Array.Empty<CharacterDefinition>();
        [SerializeField] private ConstellationDefinition constellation;
        [SerializeField] private FusionRecipe[] fusions = Array.Empty<FusionRecipe>();
        [Tooltip("Weapons and passives shown in the Weapon Lab (the draft pool).")]
        [SerializeField] private WeaponDefinition[] labWeapons = Array.Empty<WeaponDefinition>();
        [SerializeField] private PassiveDefinition[] labPassives = Array.Empty<PassiveDefinition>();
        [SerializeField] private Vector2 playerStartPosition = new(0f, -6f);

        [Header("Rules")]
        [SerializeField] private float baseMaxHp = 100f;
        [SerializeField] private int fallbackHeal = 20;
        [SerializeField] private int fallbackGold = 50;
        [SerializeField] private float reviveHpFraction = 0.5f;
        [SerializeField] private float reviveShockwaveDamage = 60f;
        [SerializeField] private int victoryStardustBase = 3;
        [SerializeField, Range(0f, 1f)] private float victoryGoldBonusFraction = 0.4f;
        [SerializeField] private float endlessGoldMultiplier = 1.5f;

        public event Action<GameState> StateChanged;
        /// <summary>The cards on offer; rerolls and banishes left.</summary>
        public event Action<IReadOnlyList<UpgradeOption>, int, int> LevelUpOffered;
        /// <summary>Revives left (0 = only "give up").</summary>
        public event Action<int> DeathOffered;
        public event Action<RunSummary> RunEnded;
        /// <summary>Number of weapons evolved by a boss/elite chest.</summary>
        public event Action<int> ChestOpened;

        private readonly List<UpgradeOption> _offer = new(4);
        private int _chapterIndex;
        private int _rerollsLeft;
        private int _banishesLeft;
        private int _revivesLeft;
        private int _kills;
        private int _fallbackGoldEarned;
        private bool _freeReviveUsed;
        private bool _endless;
        private int _goldAlreadyGranted;
        private bool _lastRunWasVictory;
        private bool _endlessFromStart;
        private int _stageGold;
        private int _stageStardust;
        private int _highestStageCleared = -1;

        public GameState State { get; private set; } = GameState.MainMenu;
        public MetaProgressionService Meta { get; private set; }
        public IReadOnlyList<MetaUpgradeDefinition> Workshop => workshop;
        public IReadOnlyList<RunDefinition> Chapters => chapters;
        public IReadOnlyList<CharacterDefinition> Characters => characters;
        public ConstellationDefinition Constellation => constellation;
        public IReadOnlyList<WeaponDefinition> LabWeapons => labWeapons;
        public IReadOnlyList<PassiveDefinition> LabPassives => labPassives;
        public CharacterDefinition CurrentCharacter { get; private set; }
        public IReadOnlyList<UpgradeOption> CurrentOffer => _offer;
        public int RerollsLeft => _rerollsLeft;
        public int BanishesLeft => _banishesLeft;
        public int ChapterIndex => _chapterIndex;
        public int Kills => _kills;
        /// <summary>The once-per-run rewarded-ad continue is still available.</summary>
        public bool FreeReviveAvailable => !_freeReviveUsed;
        public int RevivesLeft => _revivesLeft;
        /// <summary>After a victory the run can continue in Endless mode (once).</summary>
        public bool CanContinueEndless => State == GameState.RunEnd && _lastRunWasVictory && !_endless;
        /// <summary>The current/last run was started from the menu's SONSUZ button.</summary>
        public bool IsEndlessRun => _endlessFromStart;
        public int RunGold => (pickups != null ? pickups.RunGold : 0) + _fallbackGoldEarned;

        private void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Meta = new MetaProgressionService(SaveService.Load());
        }

        private void Start()
        {
            xpSystem.LevelUpQueued += OnLevelUpQueued;
            player.Died += OnPlayerDied;
            waveDirector.BossDefeated += OnBossDefeated;
            waveDirector.RunCompleted += OnRunCompleted;
            waveDirector.StageAdvanced += OnStageAdvanced;
            EnemyManager.Instance.EnemyKilled += OnEnemyKilled;
            inventory.PassivesChanged += OnPassivesChanged;
            inventory.WeaponEvolved += OnWeaponEvolved;
            inventory.WeaponFused += OnWeaponFused;
            // Owned weapons stay offered even if locked in the Lab (a character's starting weapon).
            draft.WeaponFilter = w => (Meta.IsUnlocked(w) || inventory.Find(w) != null) && !IsRetired(w);
            draft.PassiveFilter = p => Meta.IsUnlocked(p);
            EnterMenu();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnApplicationPause(bool paused)
        {
            // Backgrounding mid-run pauses instead of letting the run die off-screen — and saves it, because
            // the OS may kill a backgrounded app without another callback (run-resume.md).
            if (!paused) return;
            if (State == GameState.Playing) Pause();
            SaveRunSnapshot();
        }

        private void OnApplicationQuit() => SaveRunSnapshot();

        // ---------------------------------------------------------------- Run in flight (run-resume.md)

        [Header("Resume")]
        [SerializeField] private float autosaveSeconds = 15f;
        private float _autosaveTimer;
        private string _forcedCharacterId;
        private float _extraRunDamage;

        /// <summary>A saved run is waiting on the menu's DEVAM ET.</summary>
        public bool HasSavedRun => SaveService.HasRun;

        /// <summary>The saved run's stage (1-based) and minutes, for the menu line; (0, 0) if none.</summary>
        public (int stage, float minutes) SavedRunInfo()
        {
            RunSnapshot s = SaveService.LoadRun();
            return s == null ? (0, 0f) : (s.stage + 1, s.elapsed / 60f);
        }

        private bool InRun => State is GameState.Playing or GameState.Paused or GameState.LevelUp;

        private void Update()
        {
            if (State != GameState.Playing) return;
            _autosaveTimer += Time.unscaledDeltaTime;
            if (_autosaveTimer < autosaveSeconds) return;
            _autosaveTimer = 0f;
            SaveRunSnapshot();
        }

        /// <summary>Stores the run in flight; a no-op outside a run (death is never saved — no save-scumming).</summary>
        public void SaveRunSnapshot()
        {
            if (!InRun || !_endlessFromStart) return;
            var s = new RunSnapshot
            {
                characterId = CurrentCharacter != null ? CurrentCharacter.id : null,
                hp = player.CurrentHp,
                level = xpSystem.Level,
                xp = xpSystem.CurrentXp,
                // A draft on screen is owed again on resume.
                pendingLevelUps = xpSystem.PendingLevelUps,
                extraRunDamage = _extraRunDamage,
                runGold = pickups != null ? pickups.RunGold : 0,
                fallbackGold = _fallbackGoldEarned,
                kills = _kills,
                revivesLeft = _revivesLeft,
                freeReviveUsed = _freeReviveUsed,
                rerollsLeft = _rerollsLeft,
                banishesLeft = _banishesLeft,
                stageGold = _stageGold,
                stageStardust = _stageStardust,
                highestStageCleared = _highestStageCleared
            };
            waveDirector.WriteSnapshot(s);
            PowerMatch pm = EnemyManager.Instance.Power;
            s.powerScale = pm.Scale;
            s.powerAverage = pm.AverageTtk;
            s.powerBaseline = pm.BaselineTtk;
            s.powerElapsed = pm.Elapsed;
            foreach (WeaponBehaviour w in inventory.Weapons)
                s.weapons.Add(new UpgradeLevelEntry { id = w.Definition.id, level = w.Level });
            foreach (KeyValuePair<PassiveDefinition, int> p in inventory.Passives)
                s.passives.Add(new UpgradeLevelEntry { id = p.Key.id, level = p.Value });
            Fleet fleet = FindAnyObjectByType<Fleet>();
            if (fleet != null)
            {
                s.wingmen = fleet.Count;
                s.fleetPower = fleet.Power;
                s.fleetKills = fleet.KillCount;
                s.fleetMilestone = fleet.MilestoneIndex;
                s.fleetNextPilot = fleet.NextPilot;
            }
            SaveService.SaveRun(s);
        }

        /// <summary>Pause menu: keep the run for later and go to the menu (no rewards yet, nothing lost).</summary>
        public void SaveAndQuit()
        {
            if (!InRun) return;
            SaveRunSnapshot();
            EnterMenu();
        }

        /// <summary>DEVAM ET: continue the saved run exactly where it was left.</summary>
        public void ResumeRun()
        {
            RunSnapshot s = SaveService.LoadRun();
            if (s == null)
            {
                SaveService.DeleteRun();
                return;
            }

            _forcedCharacterId = s.characterId;
            StartRun(s.stage, true);
            _forcedCharacterId = null;

            var weapons = new List<(WeaponDefinition, int)>();
            foreach (UpgradeLevelEntry e in s.weapons)
                if (FindWeapon(e.id) is WeaponDefinition w) weapons.Add((w, e.level));
            var passives = new List<(PassiveDefinition, int)>();
            foreach (UpgradeLevelEntry e in s.passives)
                if (FindPassive(e.id) is PassiveDefinition p) passives.Add((p, e.level));
            if (weapons.Count > 0) inventory.RestoreLoadout(weapons, passives);
            _extraRunDamage = s.extraRunDamage;
            if (_extraRunDamage > 0f) inventory.Stats.AddRunBonus(StatType.Damage, _extraRunDamage);
            OnPassivesChanged();

            xpSystem.Restore(s.level, s.xp, s.pendingLevelUps);
            player.RestoreHp(s.hp);
            if (pickups != null) pickups.RestoreRunGold(s.runGold);
            _fallbackGoldEarned = s.fallbackGold;
            _kills = s.kills;
            _revivesLeft = s.revivesLeft;
            _freeReviveUsed = s.freeReviveUsed;
            _rerollsLeft = s.rerollsLeft;
            _banishesLeft = s.banishesLeft;
            _stageGold = s.stageGold;
            _stageStardust = s.stageStardust;
            _highestStageCleared = s.highestStageCleared;

            waveDirector.RestoreSnapshot(s);
            EnemyManager.Instance.Power.Restore(s.powerScale, s.powerAverage, s.powerBaseline, s.powerElapsed);
            Fleet fleet = FindAnyObjectByType<Fleet>();
            if (fleet != null) fleet.Restore(s.wingmen, s.fleetPower, s.fleetKills, s.fleetMilestone, s.fleetNextPilot);

            _autosaveTimer = autosaveSeconds; // write the resumed state back on the next frame
            if (xpSystem.PendingLevelUps > 0) OfferNextDraft();
        }

        /// <summary>
        /// A base weapon whose evolution (or a fusion built from it) is owned is retired: it must not be offered
        /// again as "YENİ!" (device feedback 2026-09-24: an evolved weapon could be bought again from level 1).
        /// </summary>
        private bool IsRetired(WeaponDefinition w)
        {
            if (inventory.Find(w) != null) return false;
            foreach (WeaponBehaviour owned in inventory.Weapons)
            {
                WeaponDefinition have = owned.Definition;
                for (WeaponDefinition e = w.evolvesInto; e != null; e = e.evolvesInto)
                    if (e == have) return true;
                foreach (FusionRecipe f in fusions)
                {
                    if (f == null || f.result != have) continue;
                    for (WeaponDefinition e = w; e != null; e = e.evolvesInto)
                        if (e == f.a || e == f.b) return true;
                }
            }
            return false;
        }

        private WeaponDefinition FindWeapon(string id)
        {
            foreach (WeaponDefinition w in labWeapons)
                for (WeaponDefinition e = w; e != null; e = e.evolvesInto)
                    if (e.id == id) return e;
            foreach (FusionRecipe f in fusions)
                for (WeaponDefinition e = f != null ? f.result : null; e != null; e = e.evolvesInto)
                    if (e.id == id) return e;
            foreach (CharacterDefinition c in characters)
                for (WeaponDefinition e = c.startingWeapon; e != null; e = e.evolvesInto)
                    if (e.id == id) return e;
            return null;
        }

        private PassiveDefinition FindPassive(string id)
        {
            foreach (PassiveDefinition p in labPassives)
                if (p != null && p.id == id) return p;
            foreach (PassiveDefinition p in draft.PassivePool)
                if (p != null && p.id == id) return p;
            return null;
        }

        // ---------------------------------------------------------------- Menu & run lifecycle

        public void EnterMenu()
        {
            waveDirector.StopRun();
            ClearWorld();
            inventory.ClearLoadout();
            SetState(GameState.MainMenu);
        }

        public bool IsChapterUnlocked(int index) => index <= Meta.HighestChapterCleared + 1;

        /// <summary>
        /// The game's one mode (Ball Blast-style, power-match.md §3.3): every chapter plays back to back as a
        /// stage, then endless waves with returning bosses, until the player falls. OYNA starts it.
        /// </summary>
        public void StartEndless() => StartRun(0, true);

        public void StartRun(int chapterIndex) => StartRun(chapterIndex, false);

        // ---------------------------------------------------------------- Trials (ad-rewards.md §3.2)

        private WeaponDefinition _trialWeapon;

        /// <summary>What the current run is trying out after a rewarded ad (display name), or null.</summary>
        public string TrialName { get; private set; }

        /// <summary>"Dene": one run with a locked pilot, paid for by a rewarded ad.</summary>
        public void StartTrial(CharacterDefinition pilot) => StartTrial(pilot, null);

        /// <summary>"Dene": one run that starts with a locked weapon next to the pilot's own.</summary>
        public void StartTrial(WeaponDefinition weapon) => StartTrial(null, weapon);

        /// <summary>A trial with a pilot, a weapon, or both (the QA autopilot plays both new ones at once).</summary>
        public void StartTrial(CharacterDefinition pilot, WeaponDefinition weapon)
        {
            _forcedCharacterId = pilot != null ? pilot.id : null;
            _trialWeapon = weapon;
            StartEndless();
            _forcedCharacterId = null;
            _trialWeapon = null;
            TrialName = pilot != null ? pilot.displayName : weapon != null ? weapon.displayName : null;
        }

        /// <summary>Lab weapon by id (base or evolution), or null.</summary>
        public WeaponDefinition WeaponById(string id) => FindWeapon(id);

        private void StartRun(int chapterIndex, bool endless)
        {
            SaveService.DeleteRun(); // a new run replaces any saved one (ResumeRun re-saves as it plays)
            _autosaveTimer = 0f;
            _extraRunDamage = 0f;
            _endlessFromStart = endless;
            _chapterIndex = Mathf.Clamp(chapterIndex, 0, chapters.Length - 1);
            ClearWorld();

            // Layers: Workshop (meta) → Constellation + character (run) → passives (in-run).
            PlayerStats stats = inventory.Stats;
            Meta.ApplyTo(stats, workshop);
            stats.ClearRunBonuses();
            Meta.ApplyConstellation(stats, constellation);

            CurrentCharacter = ResolveCharacter();
            WeaponMastery.Configure(labWeapons, Meta.GetMastery);
            Forge.Configure(Meta.GetForgeLevel(ForgeTrack.Power), Meta.GetForgeLevel(ForgeTrack.Speed));
            if (CurrentCharacter != null)
            {
                // Pilot level: +3 % damage and max HP per level above 1 (meta-economy.md §3.6).
                float pilotBonus = Formulas.PilotBonusPerLevel * (Meta.GetPilotLevel(CurrentCharacter.id) - 1);
                if (pilotBonus > 0f)
                {
                    stats.AddRunBonus(StatType.Damage, pilotBonus);
                    stats.AddRunBonus(StatType.MaxHp, pilotBonus);
                }
                foreach (StatModifier m in CurrentCharacter.modifiers) stats.AddRunBonus(m.stat, m.value);
                if (CurrentCharacter.startingWeapon != null) inventory.StartingWeapon = CurrentCharacter.startingWeapon;
                Sprite ship = CurrentCharacter.shipSprite != null ? CurrentCharacter.shipSprite : CurrentCharacter.sprite;
                if (playerSprite != null && ship != null) playerSprite.sprite = ship;
            }
            inventory.ResetLoadout();
            TrialName = null;
            if (_trialWeapon != null && inventory.Find(_trialWeapon) == null && inventory.CanTake(_trialWeapon))
                inventory.AddOrLevelWeapon(_trialWeapon);
            if (CurrentCharacter != null && CurrentCharacter.perk == CharacterPerk.RandomPassive) GrantRandomPassive();

            player.transform.position = playerStartPosition;
            player.SetMaxHp(baseMaxHp * (1f + stats.GetBonus(StatType.MaxHp)));
            player.Armor = stats.GetBonus(StatType.Armor);
            xpSystem.ResetRun();
            xpSystem.XpBonus = stats.GetBonus(StatType.Experience);
            if (SugarRush.Instance != null) SugarRush.Instance.GainBonus = stats.GetBonus(StatType.RushGain);
            draft.ClearBanished();
            draft.Luck = stats.GetBonus(StatType.Luck);

            _rerollsLeft = Mathf.RoundToInt(stats.GetBonus(StatType.Rerolls));
            _banishesLeft = Mathf.RoundToInt(stats.GetBonus(StatType.Banishes));
            _revivesLeft = Mathf.RoundToInt(stats.GetBonus(StatType.Revives));
            _kills = 0;
            _fallbackGoldEarned = 0;
            _freeReviveUsed = false;
            _endless = false;
            _goldAlreadyGranted = 0;
            _lastRunWasVictory = false;

            _stageGold = 0;
            _stageStardust = 0;
            _highestStageCleared = -1;
            if (endless) waveDirector.StartEndless(chapters, _chapterIndex);
            else waveDirector.StartRun(chapters[_chapterIndex]);
            SetState(GameState.Playing);
        }

        public void Pause()
        {
            if (State != GameState.Playing) return;
            SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            SetState(GameState.Playing);
        }

        /// <summary>Quit from the pause menu: counts as a loss, gold collected so far is kept.</summary>
        public void Abandon()
        {
            if (State is GameState.Paused or GameState.Dead) EndRun(false);
        }

        // ---------------------------------------------------------------- Level-up drafts

        private void OnLevelUpQueued(int level)
        {
            if (State == GameState.Playing) OfferNextDraft();
        }

        private void OfferNextDraft()
        {
            // 3 cards + Constellation bonus; Pıtır gets one more every 10 levels (meta-economy.md §3.3 B).
            int extra = Mathf.RoundToInt(inventory.Stats.GetBonus(StatType.DraftChoices));
            if (CurrentCharacter != null && CurrentCharacter.perk == CharacterPerk.CardEvery10Levels)
                extra += xpSystem.Level / 10;
            draft.Choices = 3 + extra;
            draft.Roll(_offer);
            SetState(GameState.LevelUp);
            LevelUpOffered?.Invoke(_offer, _rerollsLeft, _banishesLeft);
        }

        public void ChooseUpgrade(int index)
        {
            if (State != GameState.LevelUp || (uint)index >= (uint)_offer.Count) return;

            UpgradeOption option = _offer[index];
            if (option.Kind == UpgradeKind.Fallback) ApplyFallback();
            else draft.Apply(option);

            xpSystem.ConsumePendingLevelUp();
            if (xpSystem.PendingLevelUps > 0) OfferNextDraft();
            else SetState(GameState.Playing);
        }

        public void Reroll()
        {
            if (State != GameState.LevelUp || _rerollsLeft <= 0) return;
            _rerollsLeft--;
            OfferNextDraft();
        }

        public void Banish(int index)
        {
            if (State != GameState.LevelUp || _banishesLeft <= 0 || (uint)index >= (uint)_offer.Count) return;

            UpgradeOption option = _offer[index];
            ScriptableObject card = option.Kind == UpgradeKind.Weapon ? option.Weapon
                : option.Kind == UpgradeKind.Passive ? option.Passive : null;
            if (card == null) return;

            _banishesLeft--;
            draft.Banish(card);
            OfferNextDraft();
        }

        private void ApplyFallback()
        {
            // game-concept.md §6: full inventory → heal if hurt, otherwise gold.
            if (player.CurrentHp < player.MaxHp) player.Heal(fallbackHeal);
            else _fallbackGoldEarned += fallbackGold;
        }

        /// <summary>Stats that live outside the weapons (max HP, luck) follow passive changes mid-run.</summary>
        private void OnPassivesChanged()
        {
            PlayerStats stats = inventory.Stats;
            // Cam Top can push max HP down; never below 30 % of the base.
            player.SetMaxHpKeepRatio(baseMaxHp * Mathf.Max(0.3f, 1f + stats.GetBonus(StatType.MaxHp)));
            draft.Luck = stats.GetBonus(StatType.Luck);
            // Build cards that change run-wide systems (passives.md §3.2).
            player.Armor = stats.GetBonus(StatType.Armor);
            xpSystem.XpBonus = stats.GetBonus(StatType.Experience);
            if (SugarRush.Instance != null) SugarRush.Instance.GainBonus = stats.GetBonus(StatType.RushGain);
        }

        // ---------------------------------------------------------------- Combat events

        private void OnEnemyKilled(Enemy enemy)
        {
            _kills++;
            if (!enemy.IsElite) return;

            if (juice != null) juice.Hitstop();
            OpenChest();
        }

        private void OnBossDefeated(Enemy boss, bool isFinal)
        {
            if (juice != null)
            {
                juice.Hitstop(0.06f);
                juice.Shake(1f);
            }
            if (!isFinal) OpenChest();
        }

        /// <summary>Boss/elite chest: the evolution pity rule — every eligible weapon evolves.</summary>
        private void OpenChest()
        {
            int evolved = inventory.EvolveAllEligible();
            if (evolved > 0)
            {
                // Constellation: evolution chests also level up passives.
                int bonusLevels = Mathf.RoundToInt(inventory.Stats.GetBonus(StatType.EvolutionChestLevels));
                for (int i = 0; i < bonusLevels; i++) LevelRandomOwnedPassive();
            }
            evolved += inventory.FuseAllEligible(fusions);
            if (evolved > 0 && juice != null) juice.Shake(0.8f, 0.4f);
            ChestOpened?.Invoke(evolved);
        }

        private void OnRunCompleted() => EndRun(true);

        /// <summary>A stage cleared in Sonsuz Mod pays what a chapter victory used to; it is banked for the run end.</summary>
        private void OnStageAdvanced(int cleared, int next)
        {
            _chapterIndex = next;
            _stageGold += Mathf.RoundToInt(Formulas.ExpectedRunGold(cleared) * victoryGoldBonusFraction);
            _stageStardust += victoryStardustBase + cleared;
            _highestStageCleared = Mathf.Max(_highestStageCleared, cleared);
        }

        /// <summary>Yıldızpati: every evolution adds permanent damage for the rest of the run.</summary>
        private void OnWeaponEvolved(WeaponDefinition from, WeaponDefinition to)
        {
            float perEvolution = inventory.Stats.GetBonus(StatType.EvolutionDamage);
            if (perEvolution > 0f)
            {
                inventory.Stats.AddRunBonus(StatType.Damage, perEvolution);
                _extraRunDamage += perEvolution;
            }
        }

        private void OnWeaponFused(FusionRecipe recipe)
        {
            inventory.Stats.AddRunBonus(StatType.Damage, recipe.damageBonus);
            _extraRunDamage += recipe.damageBonus;
            if (juice != null) juice.Shake(1f, 0.5f);
        }

        /// <summary>Continue the won run with endless waves; rewards from here on are multiplied.</summary>
        public void ContinueEndless()
        {
            if (!CanContinueEndless) return;
            _endless = true;
            _goldAlreadyGranted = RunGold;
            waveDirector.ContinueEndless();
            SetState(GameState.Playing);
        }

        private CharacterDefinition ResolveCharacter()
        {
            string want = _forcedCharacterId ?? Meta.SelectedCharacterId;
            foreach (CharacterDefinition c in characters)
                if (c.id == want && (_forcedCharacterId != null || Meta.IsUnlocked(c))) return c;
            return characters.Length > 0 ? characters[0] : null;
        }

        private void GrantRandomPassive()
        {
            var candidates = new List<PassiveDefinition>();
            foreach (PassiveDefinition p in draft.PassivePool)
                if (Meta.IsUnlocked(p)) candidates.Add(p);
            if (candidates.Count > 0) inventory.AddOrLevelPassive(candidates[UnityEngine.Random.Range(0, candidates.Count)]);
        }

        private void LevelRandomOwnedPassive()
        {
            var candidates = new List<PassiveDefinition>();
            foreach (KeyValuePair<PassiveDefinition, int> pair in inventory.Passives)
                if (pair.Value < pair.Key.maxLevel) candidates.Add(pair.Key);
            if (candidates.Count > 0) inventory.AddOrLevelPassive(candidates[UnityEngine.Random.Range(0, candidates.Count)]);
        }

        // ---------------------------------------------------------------- Death & revive

        private void OnPlayerDied()
        {
            if (State != GameState.Playing) return;
            SaveService.DeleteRun(); // quitting on the death screen must not bring back a living snapshot
            SetState(GameState.Dead);
            DeathOffered?.Invoke(_revivesLeft);
        }

        /// <param name="free">True for the once-per-run rewarded-ad continue (game-concept.md §3.5).</param>
        public void Revive(bool free = false)
        {
            if (State != GameState.Dead) return;
            if (free)
            {
                if (_freeReviveUsed) return;
                _freeReviveUsed = true;
            }
            else
            {
                if (_revivesLeft <= 0) return;
                _revivesLeft--;
            }

            BulletSystem.Instance.ClearAll();
            player.Revive(reviveHpFraction);
            _autosaveTimer = autosaveSeconds; // re-save on the next frame
            // Revive shockwave: without it a revive lands in the same crowd that just killed you
            // (QA run 12 died again 4 s after reviving).
            if (EnemyManager.Instance != null) EnemyManager.Instance.DamageAll(reviveShockwaveDamage);
            if (Feel.VfxSystem.Instance != null)
                Feel.VfxSystem.Instance.Pop(player.transform.position, 14f, new Color(1f, 0.8f, 0.95f, 0.6f), 0.6f);
            if (juice != null) juice.Shake(0.8f, 0.4f);
            SetState(GameState.Playing);
        }

        public void GiveUp()
        {
            if (State == GameState.Dead) EndRun(false);
        }

        // ---------------------------------------------------------------- Rewards

        private void EndRun(bool victory)
        {
            SaveService.DeleteRun();
            waveDirector.StopRun();

            int gold = RunGold;
            int stardust = 0;
            bool newRecord = false;
            int cleared = victory ? _chapterIndex : -1;
            if (_endlessFromStart)
            {
                // Every run ends in a fall that still pays; stages cleared add their bonus; the record is the goal.
                gold += _stageGold;
                stardust = _stageStardust;
                cleared = _highestStageCleared;
                newRecord = Meta.RecordEndless(waveDirector.RunMinutes * 60f, _kills);
            }
            else if (_endless)
            {
                // Only what was earned after the victory, with the endless bonus.
                gold = Mathf.RoundToInt((RunGold - _goldAlreadyGranted) * endlessGoldMultiplier);
            }
            else if (victory)
            {
                gold += Mathf.RoundToInt(Formulas.ExpectedRunGold(_chapterIndex) * victoryGoldBonusFraction);
                stardust = victoryStardustBase + _chapterIndex;
            }

            // Gold is always kept, even on death (game-concept.md §3.5).
            Meta.GrantRunRewards(gold, stardust, cleared);
            _lastRunWasVictory = victory;

            var summary = new RunSummary(victory, gold, stardust, xpSystem.Level, _kills,
                waveDirector.RunMinutes, _chapterIndex, _endless || _endlessFromStart, newRecord);
            SetState(GameState.RunEnd);
            RunEnded?.Invoke(summary);
        }

        // ---------------------------------------------------------------- Helpers

        private void ClearWorld()
        {
            if (BulletSystem.Instance != null) BulletSystem.Instance.ClearAll();
            if (EnemyManager.Instance != null) EnemyManager.Instance.ClearAll();
            if (pickups != null) pickups.ResetRun();
        }

        private void SetState(GameState state)
        {
            State = state;
            TimeScaleController.SetPaused(state != GameState.Playing);
            StateChanged?.Invoke(state);
        }
    }
}
