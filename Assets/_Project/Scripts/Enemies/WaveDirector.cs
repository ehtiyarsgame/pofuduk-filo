using System;
using System.Collections.Generic;
using PofudukFilo.Bullets;
using PofudukFilo.Core;
using PofudukFilo.Player;
using UnityEngine;

namespace PofudukFilo.Enemies
{
    /// <summary>
    /// Drives a run's timeline (game-concept.md §3.2–3.3, §4.3–4.4):
    ///   • Swarm layer — buys enemies each frame from a threat budget, Formulas.SpawnBudget × DDA.
    ///   • Formation layer — Chicken Invaders formations on an interval, with a clear bonus.
    ///   • Bosses — spawned when a boss phase starts; the timeline waits while one is alive,
    ///     and a spawn-free breather follows each kill.
    /// Runs before EnemyManager so spawned enemies are ticked and proxied the same frame.
    /// </summary>
    [DefaultExecutionOrder(-60)]
    public sealed class WaveDirector : MonoBehaviour
    {
        private const int MaxSwarmSpawnsPerFrame = 8;

        [SerializeField] private RunDefinition run;
        [SerializeField] private bool startOnStart = true;

        [Header("Spawning")]
        [Tooltip("Budget can bank up to this many seconds' worth, so a quiet moment cannot store a flood.")]
        [SerializeField] private float maxBankedBudgetSeconds = 3f;
        [SerializeField] private float spawnAboveScreen = 1f;
        [SerializeField] private float formationTopPadding = 2.5f;
        [SerializeField] private float firstFormationDelay = 4f;
        [SerializeField] private float bossSlotBelowTop = 3f;

        [Header("Endless (power-match.md §3.3)")]
        [Tooltip("In Endless, a boss from the chapter's roster returns this long after the previous one dies.")]
        [SerializeField] private float endlessBossEverySeconds = 120f;

        public event Action<RunPhase> PhaseStarted;
        public event Action<FormationGroup> FormationCleared;
        public event Action<Enemy> BossSpawned;
        /// <summary>Boss, isFinalBoss. Open the chest (evolution pity rule) here.</summary>
        public event Action<Enemy, bool> BossDefeated;
        public event Action RunCompleted;
        /// <summary>Sonsuz Mod crossed a stage: the cleared chapter index and the one starting now.</summary>
        public event Action<int, int> StageAdvanced;

        private readonly DifficultyDirector _dda = new();
        private readonly List<Vector2> _formationOffsets = new(32);
        private Camera _camera;

        private bool _running;
        private float _elapsed;
        private float _nextDdaEvaluation;
        private int _phaseIndex = -1;
        private float _budget;
        private float _formationTimer;
        private int _liveFormations;
        private float _breatherUntil;
        private Enemy _currentBoss;
        private bool _currentBossIsFinal;
        private bool _endlessMode;
        private IReadOnlyList<RunDefinition> _stages;
        private int _stage;
        private float _stageStartedAt;
        private float _nextEndlessBoss;
        private int _endlessBossCycle;

        public float RunMinutes => _elapsed / 60f;
        /// <summary>Minutes into the current stage's timeline (phases start from here).</summary>
        private float StageMinutes => (_elapsed - _stageStartedAt) / 60f;
        /// <summary>1-based stage number in Sonsuz Mod.</summary>
        public int StageNumber => _stage + 1;
        public RunPhase CurrentPhase => _phaseIndex >= 0 ? run.phases[_phaseIndex] : null;
        public float DifficultyMultiplier => _dda.Multiplier;
        public bool IsBossAlive => _currentBoss != null;

        private void Start()
        {
            _camera = Camera.main;
            EnemyManager.Instance.EnemyKilled += OnEnemyKilled;
            if (PlayerHealth.Instance != null) PlayerHealth.Instance.Damaged += OnPlayerDamaged;
            if (startOnStart && run != null) StartRun(run);
        }

        private void OnDestroy()
        {
            if (EnemyManager.Instance != null) EnemyManager.Instance.EnemyKilled -= OnEnemyKilled;
            if (PlayerHealth.Instance != null) PlayerHealth.Instance.Damaged -= OnPlayerDamaged;
        }

        /// <summary>
        /// Sonsuz Mod (power-match.md §3.3): the chapters play back to back as stages; each final boss opens
        /// the next stage, and after the last one the waves repeat forever with returning bosses.
        /// </summary>
        public void StartEndless(IReadOnlyList<RunDefinition> stages)
        {
            _stages = stages;
            StartRun(stages[0], true);
        }

        /// <param name="endlessMode">The final boss does not end the run; the waves roll on.</param>
        public void StartRun(RunDefinition definition, bool endlessMode = false)
        {
            run = definition;
            _endlessMode = endlessMode;
            if (!endlessMode) _stages = null;
            _stage = 0;
            _stageStartedAt = 0f;
            _endlessBossCycle = 0;
            _running = true;
            _elapsed = 0f;
            _nextDdaEvaluation = 1f;
            _phaseIndex = -1;
            _budget = 0f;
            _liveFormations = 0;
            _breatherUntil = 0f;
            _currentBoss = null;
            IsEndless = false;
            _dda.Reset(0f);
            EnemyManager.Instance.ChapterIndex = run.chapterIndex;
            EnemyManager.Instance.ResetPower();
        }

        public void StopRun() => _running = false;

        public bool IsEndless { get; private set; }

        /// <summary>
        /// Endless mode after the final boss (game-concept.md §3.2): the last wave phase repeats
        /// forever while HP and spawn budget keep scaling with the clock.
        /// </summary>
        public void ContinueEndless()
        {
            IsEndless = true;
            for (int i = run.phases.Length - 1; i >= 0; i--)
            {
                if (run.phases[i].kind != PhaseKind.Waves) continue;
                _phaseIndex = i;
                break;
            }
            _currentBoss = null;
            _breatherUntil = _elapsed + run.breatherSeconds;
            _nextEndlessBoss = _elapsed + endlessBossEverySeconds;
            _running = true;
        }

        private void NextStage()
        {
            int cleared = run.chapterIndex;
            run = _stages[++_stage];
            _phaseIndex = -1;
            _stageStartedAt = _elapsed + run.breatherSeconds; // the new timeline starts after the breather
            _breatherUntil = _stageStartedAt;
            EnemyManager.Instance.ChapterIndex = run.chapterIndex;
            StageAdvanced?.Invoke(cleared, run.chapterIndex);
        }

        /// <summary>Endless: the chapter's bosses return in rotation, each tougher on the clock and Power Match.</summary>
        private void TickEndlessBoss()
        {
            if (IsBossAlive || _elapsed < _nextEndlessBoss) return;
            int bosses = 0;
            foreach (RunPhase p in run.phases)
                if (p.kind != PhaseKind.Waves && p.bossPrefab != null) bosses++;
            if (bosses == 0) return;

            int pick = _endlessBossCycle++ % bosses;
            foreach (RunPhase p in run.phases)
            {
                if (p.kind == PhaseKind.Waves || p.bossPrefab == null) continue;
                if (pick-- > 0) continue;
                SpawnBoss(p.bossPrefab, false);
                break;
            }
            _nextEndlessBoss = float.MaxValue; // re-armed when this boss dies
        }

        private void Update()
        {
            if (!_running) return;

            float dt = Time.deltaTime;
            _elapsed += dt;
            EnemyManager.Instance.RunMinutes = RunMinutes;

            if (_elapsed >= _nextDdaEvaluation)
            {
                _dda.Evaluate(_elapsed);
                _nextDdaEvaluation = _elapsed + 1f;
            }

            AdvancePhases();

            RunPhase phase = CurrentPhase;
            if (phase == null || _elapsed < _breatherUntil) return;

            TickSwarm(phase, dt);
            TickFormations(phase, dt);
        }

        // ---------------------------------------------------------------- Phases & bosses

        private void AdvancePhases()
        {
            // The timeline waits for a living boss, so two bosses never overlap.
            if (IsEndless)
            {
                TickEndlessBoss();
                return;
            }
            if (IsBossAlive) return;

            int target = run.PhaseIndexAt(StageMinutes);
            while (_phaseIndex < target && !IsBossAlive)
                EnterPhase(++_phaseIndex);
        }

        private void EnterPhase(int index)
        {
            RunPhase phase = run.phases[index];
            _formationTimer = firstFormationDelay;
            PhaseStarted?.Invoke(phase);

            if (phase.kind == PhaseKind.Waves || phase.bossPrefab == null) return;
            SpawnBoss(phase.bossPrefab, phase.kind == PhaseKind.FinalBoss);
        }

        private void SpawnBoss(Enemy prefab, bool isFinal)
        {
            Vector2 top = TopCenter();
            Enemy boss = EnemyManager.Instance.Spawn(prefab, top + Vector2.up * spawnAboveScreen);
            if (boss == null) return;

            // Boss prefabs should use a very long Formation Hold Seconds so they never dive away.
            boss.AssignFormationSlot(top + Vector2.down * bossSlotBelowTop);
            _currentBoss = boss;
            _currentBossIsFinal = isFinal;
            BossSpawned?.Invoke(boss);
        }

        private void OnEnemyKilled(Enemy enemy)
        {
            if (enemy != _currentBoss) return;

            bool isFinal = _currentBossIsFinal;
            _currentBoss = null;
            _breatherUntil = _elapsed + run.breatherSeconds;
            if (BulletSystem.Instance != null) BulletSystem.Instance.RequestClearEnemyBullets();
            if (IsEndless) _nextEndlessBoss = _elapsed + endlessBossEverySeconds;

            if (isFinal && _endlessMode)
            {
                // Sonsuz Mod: the final boss is a milestone, not the end.
                BossDefeated?.Invoke(enemy, false);
                if (_stages != null && _stage + 1 < _stages.Count) NextStage();
                else ContinueEndless();
                return;
            }
            BossDefeated?.Invoke(enemy, isFinal);

            if (isFinal)
            {
                _running = false;
                RunCompleted?.Invoke();
            }
        }

        private void OnPlayerDamaged(float amount) => _dda.RegisterPlayerHit(_elapsed);

        // ---------------------------------------------------------------- Swarm layer

        private void TickSwarm(RunPhase phase, float dt)
        {
            if (phase.swarm.Length == 0) return;

            float perSecond = Formulas.SpawnBudget(RunMinutes, _dda.Multiplier) * phase.budgetScale;
            _budget = Mathf.Min(_budget + perSecond * dt, perSecond * maxBankedBudgetSeconds);

            for (int i = 0; i < MaxSwarmSpawnsPerFrame; i++)
            {
                int pick = PickAffordable(phase.swarm, _budget);
                if (pick < 0) return; // save up for something

                SwarmEntry entry = phase.swarm[pick];
                if (EnemyManager.Instance.Spawn(entry.prefab, RandomTopEdgePoint()) == null) return; // enemy cap
                _budget -= entry.threatCost;
            }
        }

        private static int PickAffordable(SwarmEntry[] entries, float budget)
        {
            float total = 0f;
            for (int i = 0; i < entries.Length; i++)
                if (entries[i].threatCost <= budget) total += entries[i].weight;
            if (total <= 0f) return -1;

            float roll = UnityEngine.Random.value * total;
            int last = -1;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].threatCost > budget) continue;
                last = i;
                roll -= entries[i].weight;
                if (roll < 0f) return i;
            }
            return last;
        }

        // ---------------------------------------------------------------- Formation layer

        private void TickFormations(RunPhase phase, float dt)
        {
            if (phase.formationInterval <= 0f || phase.formations.Length == 0) return;

            _formationTimer -= dt;
            if (_formationTimer > 0f || _liveFormations >= run.maxLiveFormations) return;

            _formationTimer = phase.formationInterval;
            SpawnFormation(PickFormation(phase.formations));
        }

        private static FormationEntry PickFormation(FormationEntry[] entries)
        {
            float total = 0f;
            for (int i = 0; i < entries.Length; i++) total += Mathf.Max(0f, entries[i].weight);

            float roll = UnityEngine.Random.value * total;
            for (int i = 0; i < entries.Length; i++)
            {
                roll -= Mathf.Max(0f, entries[i].weight);
                if (roll < 0f) return entries[i];
            }
            return entries[entries.Length - 1];
        }

        private void SpawnFormation(in FormationEntry entry)
        {
            GetScreenBounds(out float left, out float right, out float top);
            FormationLayout.Build(entry.shape, entry.rows, entry.columns, entry.spacing, _formationOffsets);

            float wiggle = (right - left) * 0.15f;
            var center = new Vector2(
                (left + right) * 0.5f + UnityEngine.Random.Range(-wiggle, wiggle),
                top - formationTopPadding);

            var group = new FormationGroup(center);
            for (int i = 0; i < _formationOffsets.Count; i++)
            {
                Vector2 slot = center + _formationOffsets[i];
                slot.x = Mathf.Clamp(slot.x, left + 0.4f, right - 0.4f);

                // Enter from above the slot, keeping the formation's shape during the fly-in.
                var spawn = new Vector2(slot.x, top + spawnAboveScreen + (slot.y - center.y));
                Enemy enemy = EnemyManager.Instance.Spawn(entry.prefab, spawn);
                if (enemy == null) break;

                enemy.Group = group;
                enemy.AssignFormationSlot(slot);
                group.AddMember();
            }

            if (group.Remaining == 0) return;
            _liveFormations++;
            group.Finished += _ => _liveFormations--;
            group.Cleared += g => FormationCleared?.Invoke(g);
        }

        // ---------------------------------------------------------------- Screen helpers

        private Vector2 RandomTopEdgePoint()
        {
            GetScreenBounds(out float left, out float right, out float top);
            return new Vector2(UnityEngine.Random.Range(left + 0.5f, right - 0.5f), top + spawnAboveScreen);
        }

        private Vector2 TopCenter()
        {
            GetScreenBounds(out float left, out float right, out float top);
            return new Vector2((left + right) * 0.5f, top);
        }

        private void GetScreenBounds(out float left, out float right, out float top)
        {
            float halfH = _camera.orthographicSize;
            float halfW = halfH * _camera.aspect;
            Vector3 c = _camera.transform.position;
            left = c.x - halfW;
            right = c.x + halfW;
            top = c.y + halfH;
        }
    }
}
