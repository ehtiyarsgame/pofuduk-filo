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

        private static int s_nextId = 1;
        private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");

        private MaterialPropertyBlock _mpb;
        private float _hp;
        private float _fireTimer;
        private float _age;
        private float _flashTimer;
        private float _originX;
        private bool _hasSlot;
        private Vector2 _slot;
        private float _slotTimer;

        public int Id { get; private set; }
        public float HitRadius => hitRadius;
        public int XpValue => xpValue;
        public int GoldValue => goldValue;
        public bool IsDead => _hp <= 0f;

        /// <summary>The prefab this instance came from; EnemyManager uses it to return it to the right pool.</summary>
        public Enemy SourcePrefab { get; set; }

        public void Initialize(float maxHp)
        {
            _hp = maxHp;
        }

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
            SetFlash(0f);
        }

        public virtual void OnDespawned() { }

        public virtual void Tick(float dt, Vector2 playerPosition)
        {
            _age += dt;
            if (_hasSlot) TickFormation(dt);
            else TickSwarm(dt);

            if (_flashTimer > 0f)
            {
                _flashTimer -= dt;
                if (_flashTimer <= 0f) SetFlash(0f);
            }

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

        /// <returns>True if this hit killed the enemy.</returns>
        public bool TakeDamage(float amount)
        {
            if (IsDead) return false;
            _hp -= amount;
            _flashTimer = 0.05f; // one-frame-ish white flash (art-bible §5.1)
            SetFlash(1f);
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
            if (spriteRenderer == null) return;
            _mpb ??= new MaterialPropertyBlock();
            spriteRenderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(FlashAmountId, amount);
            spriteRenderer.SetPropertyBlock(_mpb);
        }
    }
}
