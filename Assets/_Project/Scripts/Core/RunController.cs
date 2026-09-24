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

        public RunSummary(bool victory, int gold, int stardust, int level, int kills, float minutes, int chapterIndex)
        {
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
        [Tooltip("Weapons and passives shown in the Weapon Lab (the draft pool).")]
        [SerializeField] private WeaponDefinition[] labWeapons = Array.Empty<WeaponDefinition>();
        [SerializeField] private PassiveDefinition[] labPassives = Array.Empty<PassiveDefinition>();
        [SerializeField] private Vector2 playerStartPosition = new(0f, -6f);

        [Header("Rules")]
        [SerializeField] private float baseMaxHp = 100f;
        [SerializeField] private int fallbackHeal = 30;
        [SerializeField] private int fallbackGold = 50;
        [SerializeField] private float reviveHpFraction = 0.5f;
        [SerializeField] private int victoryStardustBase = 3;
        [SerializeField, Range(0f, 1f)] private float victoryGoldBonusFraction = 0.4f;

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
            EnemyManager.Instance.EnemyKilled += OnEnemyKilled;
            inventory.PassivesChanged += OnPassivesChanged;
            inventory.WeaponEvolved += OnWeaponEvolved;
            // Owned weapons stay offered even if locked in the Lab (a character's starting weapon).
            draft.WeaponFilter = w => Meta.IsUnlocked(w) || inventory.Find(w) != null;
            draft.PassiveFilter = p => Meta.IsUnlocked(p);
            EnterMenu();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnApplicationPause(bool paused)
        {
            // Backgrounding mid-run pauses instead of letting the run die off-screen.
            if (paused && State == GameState.Playing) Pause();
        }

        // ---------------------------------------------------------------- Menu & run lifecycle

        public void EnterMenu()
        {
            waveDirector.StopRun();
            ClearWorld();
            SetState(GameState.MainMenu);
        }

        public bool IsChapterUnlocked(int index) => index <= Meta.HighestChapterCleared + 1;

        public void StartRun(int chapterIndex)
        {
            _chapterIndex = Mathf.Clamp(chapterIndex, 0, chapters.Length - 1);
            ClearWorld();

            // Layers: Workshop (meta) → Constellation + character (run) → passives (in-run).
            PlayerStats stats = inventory.Stats;
            Meta.ApplyTo(stats, workshop);
            stats.ClearRunBonuses();
            Meta.ApplyConstellation(stats, constellation);

            CurrentCharacter = ResolveCharacter();
            if (CurrentCharacter != null)
            {
                foreach (StatModifier m in CurrentCharacter.modifiers) stats.AddRunBonus(m.stat, m.value);
                if (CurrentCharacter.startingWeapon != null) inventory.StartingWeapon = CurrentCharacter.startingWeapon;
                if (playerSprite != null && CurrentCharacter.sprite != null) playerSprite.sprite = CurrentCharacter.sprite;
            }
            inventory.ResetLoadout();
            if (CurrentCharacter != null && CurrentCharacter.perk == CharacterPerk.RandomPassive) GrantRandomPassive();

            player.transform.position = playerStartPosition;
            player.SetMaxHp(baseMaxHp * (1f + stats.GetBonus(StatType.MaxHp)));
            player.Armor = stats.GetBonus(StatType.Armor);
            xpSystem.ResetRun();
            xpSystem.XpBonus = stats.GetBonus(StatType.Experience);
            draft.ClearBanished();
            draft.Luck = stats.GetBonus(StatType.Luck);

            _rerollsLeft = Mathf.RoundToInt(stats.GetBonus(StatType.Rerolls));
            _banishesLeft = Mathf.RoundToInt(stats.GetBonus(StatType.Banishes));
            _revivesLeft = Mathf.RoundToInt(stats.GetBonus(StatType.Revives));
            _kills = 0;
            _fallbackGoldEarned = 0;
            _freeReviveUsed = false;

            waveDirector.StartRun(chapters[_chapterIndex]);
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
            player.SetMaxHpKeepRatio(baseMaxHp * (1f + stats.GetBonus(StatType.MaxHp)));
            draft.Luck = stats.GetBonus(StatType.Luck);
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
            if (evolved > 0 && juice != null) juice.Shake(0.8f, 0.4f);
            ChestOpened?.Invoke(evolved);
        }

        private void OnRunCompleted() => EndRun(true);

        /// <summary>Yıldızpati: every evolution adds permanent damage for the rest of the run.</summary>
        private void OnWeaponEvolved(WeaponDefinition from, WeaponDefinition to)
        {
            float perEvolution = inventory.Stats.GetBonus(StatType.EvolutionDamage);
            if (perEvolution > 0f) inventory.Stats.AddRunBonus(StatType.Damage, perEvolution);
        }

        private CharacterDefinition ResolveCharacter()
        {
            foreach (CharacterDefinition c in characters)
                if (c.id == Meta.SelectedCharacterId && Meta.IsUnlocked(c)) return c;
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
            SetState(GameState.Playing);
        }

        public void GiveUp()
        {
            if (State == GameState.Dead) EndRun(false);
        }

        // ---------------------------------------------------------------- Rewards

        private void EndRun(bool victory)
        {
            waveDirector.StopRun();

            int gold = RunGold;
            int stardust = 0;
            if (victory)
            {
                gold += Mathf.RoundToInt(Formulas.ExpectedRunGold(_chapterIndex) * victoryGoldBonusFraction);
                stardust = victoryStardustBase + _chapterIndex;
            }

            // Gold is always kept, even on death (game-concept.md §3.5).
            Meta.GrantRunRewards(gold, stardust, victory ? _chapterIndex : -1);

            var summary = new RunSummary(victory, gold, stardust, xpSystem.Level, _kills,
                waveDirector.RunMinutes, _chapterIndex);
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
