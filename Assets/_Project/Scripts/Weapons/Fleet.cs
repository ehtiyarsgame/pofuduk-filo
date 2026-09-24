using System;
using System.Collections.Generic;
using PofudukFilo.Bullets;
using PofudukFilo.Core;
using PofudukFilo.Enemies;
using PofudukFilo.Feel;
using PofudukFilo.Player;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PofudukFilo.Weapons
{
    /// <summary>
    /// Pofuduk Filo — the fleet that gives the game its name (design/gdd/sugar-rush.md §Fleet).
    /// Elites, bosses and kill milestones drop a rescue bubble with a pilot friend inside; fly
    /// into it and the friend joins as a wingman in formation, auto-firing stars at the nearest
    /// enemy. The fleet caps at <see cref="MaxWingmen"/>; extra rescues power every wingman up.
    /// Wingmen scale with the player's damage stat and fire faster during Şeker Hücumu.
    /// </summary>
    public sealed class Fleet : MonoBehaviour
    {
        public const int MaxWingmen = 4;

        [SerializeField] private WeaponInventory inventory;
        [SerializeField] private Sprite[] pilotSprites = Array.Empty<Sprite>();
        [SerializeField] private Sprite bubbleSprite;
        [SerializeField] private Material material;
        [SerializeField] private int bulletTypeIndex = 3;
        [SerializeField] private float baseDamage = 7f;
        [SerializeField] private float fireInterval = 0.6f;
        [SerializeField] private float bulletSpeed = 13f;
        [SerializeField] private float targetRange = 13f;
        [SerializeField] private float powerPerExtraRescue = 0.25f;
        [SerializeField] private int[] killMilestones = { 40, 160, 400, 800 };
        [SerializeField] private float capsuleFallSpeed = 1.3f;
        [SerializeField] private float pickupRadius = 1.3f;

        private static readonly Vector2[] Slots =
        {
            new(-1.15f, -0.35f), new(1.15f, -0.35f), new(-2.1f, -0.95f), new(2.1f, -0.95f)
        };

        private sealed class Wingman
        {
            public Transform Transform;
            public Vector2 Velocity;
            public float Cooldown;
            public float Phase;
        }

        private sealed class Capsule
        {
            public Transform Root;
            public Sprite Pilot;
        }

        public event Action<int> WingmanJoined; // fleet size
        public int Count => _wingmen.Count;

        private readonly List<Wingman> _wingmen = new(MaxWingmen);
        private readonly List<Capsule> _capsules = new();
        private float _power = 1f;
        private int _milestoneIndex;
        private int _kills;
        private int _nextPilot;

        private void Start()
        {
            if (EnemyManager.Instance != null) EnemyManager.Instance.EnemyKilled += OnKilled;
            if (RunController.Instance != null) RunController.Instance.StateChanged += OnState;
        }

        private void OnDestroy()
        {
            if (EnemyManager.Instance != null) EnemyManager.Instance.EnemyKilled -= OnKilled;
            if (RunController.Instance != null) RunController.Instance.StateChanged -= OnState;
        }

        private void OnState(GameState state)
        {
            if (state is GameState.MainMenu or GameState.RunEnd) ResetRun();
        }

        public void ResetRun()
        {
            foreach (Wingman w in _wingmen) Destroy(w.Transform.gameObject);
            foreach (Capsule c in _capsules) Destroy(c.Root.gameObject);
            _wingmen.Clear();
            _capsules.Clear();
            _power = 1f;
            _milestoneIndex = 0;
            _kills = 0;
            _nextPilot = 0;
        }

        private void OnKilled(Enemy e)
        {
            _kills++;
            bool milestone = _milestoneIndex < killMilestones.Length && _kills >= killMilestones[_milestoneIndex];
            if (milestone) _milestoneIndex++;
            if (milestone || e.IsElite || e is BossEnemy) SpawnCapsule(e.transform.position);
        }

        private void SpawnCapsule(Vector2 position)
        {
            if (pilotSprites.Length == 0) return;
            var root = new GameObject("rescue-bubble").transform;
            root.SetParent(transform, false);
            root.position = position;
            Sprite pilot = pilotSprites[_nextPilot++ % pilotSprites.Length];
            MakeSprite(root, pilot, 0.62f, 60);
            MakeSprite(root, bubbleSprite, 1.25f, 61);
            _capsules.Add(new Capsule { Root = root, Pilot = pilot });
        }

        private SpriteRenderer MakeSprite(Transform parent, Sprite sprite, float scale, int order)
        {
            var go = new GameObject("s");
            go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.sortingOrder = order;
            if (material != null) r.sharedMaterial = material;
            return r;
        }

        private void Update()
        {
            PlayerHealth player = PlayerHealth.Instance;
            if (player == null || !player.IsAlive) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Vector2 ship = player.transform.position;

            UpdateCapsules(dt, ship);
            UpdateWingmen(dt, ship);
        }

        private void UpdateCapsules(float dt, Vector2 ship)
        {
            for (int i = _capsules.Count - 1; i >= 0; i--)
            {
                Capsule c = _capsules[i];
                Vector3 p = c.Root.position;
                p.y -= capsuleFallSpeed * dt;
                p.x += Mathf.Sin(Time.time * 2f + i) * 0.4f * dt;
                // Gentle homing once close, so a near-miss still counts.
                Vector2 toShip = ship - (Vector2)p;
                if (toShip.magnitude < pickupRadius * 2.2f) p += (Vector3)(toShip.normalized * (5f * dt));
                c.Root.position = p;
                float wob = 1f + Mathf.Sin(Time.time * 5f + i) * 0.05f;
                c.Root.localScale = new Vector3(wob, 2f - wob, 1f);

                if (toShip.magnitude <= pickupRadius)
                {
                    Rescue(c);
                    _capsules.RemoveAt(i);
                }
                else if (p.y < ship.y - 12f)
                {
                    Destroy(c.Root.gameObject);
                    _capsules.RemoveAt(i);
                }
            }
        }

        private void Rescue(Capsule c)
        {
            Vector2 at = c.Root.position;
            Destroy(c.Root.gameObject);
            if (VfxSystem.Instance != null)
            {
                VfxSystem.Instance.Pop(at, 1.4f, new Color(1f, 0.7f, 0.9f, 0.7f), 0.3f);
                VfxSystem.Instance.Sparks(at, new Color(1f, 0.95f, 0.7f), 14, 7f, 0.35f);
            }
            if (Audio.AudioManager.Instance != null) Audio.AudioManager.Instance.Play(Audio.SfxId.LevelUp, 0.05f);

            if (_wingmen.Count < MaxWingmen)
            {
                var t = new GameObject("wingman").transform;
                t.SetParent(transform, false);
                t.position = at;
                MakeSprite(t, c.Pilot, 0.62f, 9);
                _wingmen.Add(new Wingman { Transform = t, Phase = Random.value * 6f, Cooldown = 0.2f });
            }
            else
            {
                _power += powerPerExtraRescue;
            }
            WingmanJoined?.Invoke(_wingmen.Count);
        }

        private void UpdateWingmen(float dt, Vector2 ship)
        {
            BulletSystem bullets = BulletSystem.Instance;
            PlayerStats stats = inventory != null ? inventory.Stats : null;
            for (int i = 0; i < _wingmen.Count; i++)
            {
                Wingman w = _wingmen[i];
                w.Phase += dt;
                Vector2 target = ship + Slots[i] + new Vector2(0f, Mathf.Sin(w.Phase * 3f) * 0.12f);
                Vector2 pos = w.Transform.position;
                // Critically damped follow: a little lag makes the formation feel alive.
                Vector2 next = Vector2.SmoothDamp(pos, target, ref w.Velocity, 0.12f, 60f, dt);
                w.Transform.position = next;
                w.Transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Clamp(-w.Velocity.x * 3f, -20f, 20f));

                w.Cooldown -= dt * SugarRush.FireRate * Forge.FireRate;
                if (w.Cooldown > 0f || bullets == null) continue;
                w.Cooldown = fireInterval;

                Enemy e = EnemyManager.Instance != null ? EnemyManager.Instance.FindNearest(next, targetRange) : null;
                Vector2 dir = e != null ? ((Vector2)e.transform.position - next).normalized : Vector2.up;
                if (dir.y < 0.2f) dir = new Vector2(dir.x, 0.2f).normalized; // never shoot backwards into the player's space
                float dmg = baseDamage * _power * (stats != null ? stats.DamageMultiplier : 1f) * Forge.DamageMultiplier;
                bullets.SpawnPlayerBullet(bulletTypeIndex, next + dir * 0.3f, dir * bulletSpeed, dmg, 0, 2f);
            }
        }
    }
}
