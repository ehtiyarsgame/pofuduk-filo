using PofudukFilo.Bullets;
using PofudukFilo.Pooling;
using UnityEngine;

namespace PofudukFilo.Enemies
{
    /// <summary>
    /// A pooled enemy. Has no Update of its own — EnemyManager ticks every enemy from one loop
    /// (architecture.md §8 rule 3). Movement here is the swarm/formation baseline; bosses and
    /// elites subclass and override <see cref="Tick"/>.
    /// </summary>
    public class Enemy : MonoBehaviour, IPoolable
    {
        [Header("Stats")]
        [SerializeField] private float baseHp = 20f;
        [SerializeField] private float hitRadius = 0.4f;
        [SerializeField] private int xpValue = 1;
        [SerializeField] private int goldValue = 1;
        [Tooltip("Golden-crowned elite: drops a chest and triggers hitstop on death.")]
        [SerializeField] private bool isElite;

        [Header("Movement")]
        [SerializeField] private Vector2 velocity = new(0f, -1.2f);
        [SerializeField] private float swayAmplitude = 0.6f;
        [SerializeField] private float swayFrequency = 1.5f;
        [Tooltip("Speed while flying into a formation slot.")]
        [SerializeField] private float formationEntrySpeed = 4f;
        [Tooltip("Seconds a formation member holds its slot before diving down and off screen.")]
        [SerializeField] private float formationHoldSeconds = 25f;

        [Header("Attack")]
        [SerializeField] private int bulletTypeIndex;
        [SerializeField] private float fireInterval = 2.5f;
        [SerializeField] private int bulletsPerVolley = 1;
        [SerializeField] private float volleySpreadDegrees = 0f;
        [SerializeField] private float bulletSpeed = 3f;
        [SerializeField] private float bulletDamage = 10f;
        [SerializeField] private bool aimAtPlayer = true;

        [Header("Feel")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [Tooltip("Shared white-silhouette material swapped in for the hit flash. Swapping shared materials keeps\n" +
                 "sprites batched, unlike a MaterialPropertyBlock per enemy.")]
        [SerializeField] private Material flashMaterial;
        [Tooltip("Main colour for death bursts and hit sparks.")]
        [SerializeField] private Color fxColor = new(1f, 0.62f, 0.8f, 1f);
        [Tooltip("Idle squash-and-stretch amount (0 = rigid).")]
        [SerializeField] private float wobble = 0.06f;

        private static int s_nextId = 1;
        private Material _normalMaterial;
        private Vector3 _baseScale;
        private float _punch;
        private float _wobblePhase;
        private float _hp;
        private float _maxHp;
        private float _fireTimer;
        private float _age;
        private float _flashTimer;
        private float _originX;
        private bool _hasSlot;
        private Vector2 _slot;
        private float _slotTimer;
        private float _stunTimer;
        private float _slowTimer;
        private float _slowFactor = 1f;

        public int Id { get; private set; }
        public float HitRadius => hitRadius;
        public int XpValue => xpValue;
        public int GoldValue => goldValue;
        public bool IsDead => _hp <= 0f;
        public bool IsElite => isElite;
        public Color FxColor => fxColor;
        /// <summary>Visual size in world units (for burst radii).</summary>
        public float VisualScale => _baseScale.x;

        /// <summary>The prefab this instance came from; EnemyManager uses it to return it to the right pool.</summary>
        public Enemy SourcePrefab { get; set; }

        public void Initialize(float maxHp)
        {
            _hp = maxHp;
            _maxHp = maxHp;
        }

        public float HpFraction => _maxHp > 0f ? Mathf.Clamp01(_hp / _maxHp) : 0f;

        /// <summary>Seconds between volleys; bosses change it per phase.</summary>
        protected float FireInterval
        {
            get => fireInterval;
            set => fireInterval = value;
        }

        protected SpriteRenderer SpriteRenderer => spriteRenderer;

        public float BaseHp => baseHp;

        /// <summary>Formation this enemy belongs to (Chicken Invaders layer), or null for swarm enemies.</summary>
        public FormationGroup Group { get; set; }

        /// <summary>Fly to <paramref name="slot"/> and hover there (formation layer).</summary>
        public void AssignFormationSlot(Vector2 slot)
        {
            _hasSlot = true;
            _slot = slot;
            _slotTimer = formationHoldSeconds;
        }

        public virtual void OnSpawned()
        {
            Id = s_nextId++;
            _age = 0f;
            _fireTimer = fireInterval * Random.Range(0.5f, 1f);
            _originX = transform.position.x;
            _hasSlot = false;
            Group = null;
            _stunTimer = 0f;
            _slowTimer = 0f;
            _slowFactor = 1f;
            SetFlash(0f);
            if (_baseScale == Vector3.zero) _baseScale = transform.localScale;
            transform.localScale = _baseScale;
            _punch = 0f;
            _wobblePhase = Random.value * 6.28f;
        }

        public virtual void OnDespawned() { }

        /// <summary>Idle squash-and-stretch plus the hit punch; purely visual (hit radius is separate).</summary>
        private void AnimateScale(float dt)
        {
            _punch = Mathf.MoveTowards(_punch, 0f, dt * 6f);
            float w = Mathf.Sin(_age * 7f + _wobblePhase) * wobble;
            float sx = 1f + w + _punch * 0.35f, sy = 1f - w - _punch * 0.2f;
            transform.localScale = new Vector3(_baseScale.x * sx, _baseScale.y * sy, _baseScale.z);
        }

        public virtual void Tick(float dt, Vector2 playerPosition)
        {
            _age += dt;
            if (_slowTimer > 0f)
            {
                _slowTimer -= dt;
                if (_slowTimer <= 0f) _slowFactor = 1f;
            }

            float moveDt = dt * _slowFactor;
            if (_hasSlot) TickFormation(moveDt);
            else TickSwarm(moveDt);

            AnimateScale(dt);
            if (_flashTimer > 0f)
            {
                _flashTimer -= dt;
                if (_flashTimer <= 0f) SetFlash(0f);
            }

            // Stunned enemies cannot shoot (Spark Cat Lv3, weapon-system.md §3.2).
            if (_stunTimer > 0f)
            {
                _stunTimer -= dt;
                return;
            }

            // Nothing shoots from off-screen (fairness, and it cannot be shot back yet).
            if (EnemyManager.Instance != null && !EnemyManager.Instance.IsOnScreen(this)) return;

            _fireTimer -= dt;
            if (_fireTimer <= 0f)
            {
                _fireTimer = fireInterval;
                FireVolley(playerPosition);
            }
        }

        private void TickSwarm(float dt)
        {
            Vector3 p = transform.position;
            p.y += velocity.y * dt;
            _originX += velocity.x * dt;
            p.x = _originX + Mathf.Sin(_age * swayFrequency) * swayAmplitude;
            transform.position = p;
        }

        private void TickFormation(float dt)
        {
            // Hover around the slot with a gentle sway; after the hold time, dive like a swarm enemy.
            Vector2 target = _slot + new Vector2(Mathf.Sin(_age * swayFrequency) * swayAmplitude * 0.5f, 0f);
            transform.position = Vector2.MoveTowards(transform.position, target, formationEntrySpeed * dt);

            _slotTimer -= dt;
            if (_slotTimer <= 0f)
            {
                _hasSlot = false;
                _originX = transform.position.x;
            }
        }

        public void Stun(float seconds)
        {
            if (CanBeStunned) _stunTimer = Mathf.Max(_stunTimer, seconds);
        }

        /// <summary>Bosses opt out so a chain-lightning build cannot silence a whole fight.</summary>
        protected virtual bool CanBeStunned => true;

        /// <summary>Movement speed × <paramref name="factor"/> for <paramref name="seconds"/> (Gum Rings).</summary>
        public void Slow(float factor, float seconds)
        {
            _slowFactor = Mathf.Min(_slowFactor, Mathf.Clamp01(factor));
            _slowTimer = Mathf.Max(_slowTimer, seconds);
        }

        /// <summary>External push/pull (Galaxy Vortex). Keeps sway origin and formation slot in step.</summary>
        public void Nudge(Vector2 delta)
        {
            transform.position += (Vector3)delta;
            _originX += delta.x;
            if (_hasSlot) _slot += delta;
        }

        /// <returns>True if this hit killed the enemy.</returns>
        public bool TakeDamage(float amount)
        {
            if (IsDead) return false;
            _hp -= amount;
            _flashTimer = 0.06f; // one-frame-ish white flash (art-bible §5.1)
            SetFlash(1f);
            _punch = 1f;
            return IsDead;
        }

        protected virtual void FireVolley(Vector2 playerPosition)
        {
            BulletSystem bullets = BulletSystem.Instance;
            if (bullets == null || bulletsPerVolley <= 0) return;

            Vector2 origin = transform.position;
            Vector2 forward = aimAtPlayer ? (playerPosition - origin).normalized : Vector2.down;
            float baseAngle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
            float step = bulletsPerVolley > 1 ? volleySpreadDegrees / (bulletsPerVolley - 1) : 0f;
            float start = baseAngle - volleySpreadDegrees * 0.5f;

            for (int i = 0; i < bulletsPerVolley; i++)
            {
                float a = (start + step * i) * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                bullets.SpawnEnemyBullet(bulletTypeIndex, origin, dir * bulletSpeed, bulletDamage);
            }
        }

        private void SetFlash(float amount)
        {
            if (spriteRenderer == null || flashMaterial == null) return;
            if (_normalMaterial == null) _normalMaterial = spriteRenderer.sharedMaterial;
            spriteRenderer.sharedMaterial = amount > 0f ? flashMaterial : _normalMaterial;
        }
    }
}
