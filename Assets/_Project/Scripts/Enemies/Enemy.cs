using PofudukFilo.Bullets;
using PofudukFilo.Pooling;
using PofudukFilo.Core;
using PofudukFilo.Player;
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

        [Header("Attack identity (enemy-attacks.md)")]
        [Tooltip("Seconds the enemy glows warm before each volley (0 = no warning).")]
        [SerializeField] private float shotTelegraphSeconds;
        [Tooltip("Volleys per attack, fired burstGap apart (cookie robot 3-round burst, ice-cream spiral).")]
        [SerializeField] private int burstCount = 1;
        [SerializeField] private float burstGap = 0.15f;
        [Tooltip("Degrees the volley turns after every shot (spirals). Only for volleys that are not aimed.")]
        [SerializeField] private float spinPerShot;
        [Tooltip("> 0: the volley is a full ring with one gap of this many degrees at a random angle (marshmallow).")]
        [SerializeField] private float ringGapDegrees;
        [Tooltip("Bullets thrown in a ring when this enemy dies (gum balloon). 0 = none.")]
        [SerializeField] private int deathBurstBullets;
        [SerializeField] private float deathBurstSpeed = 2.6f;
        [Tooltip("> 0: every shot bursts into three smaller shots (splitTypeIndex) after this many seconds (jelly bear).")]
        [SerializeField] private float splitFuse;
        [SerializeField] private int splitTypeIndex = -1;
        [Tooltip("Seconds between lunges toward the ship (elite). 0 = never.")]
        [SerializeField] private float lungeInterval;
        [SerializeField] private float lungeSpeed = 5f;
        [SerializeField] private float lungeSeconds = 0.6f;
        [Tooltip("Donut UFO: a laser beam straight down instead of bullets — warning line first, then the beam.")]
        [SerializeField] private Sprite laserBeamSprite;
        [SerializeField] private Sprite laserWarningSprite;
        [SerializeField] private float laserWarnSeconds = 0.9f;
        [SerializeField] private float laserFireSeconds = 0.6f;
        [SerializeField] private float laserHalfWidth = 0.24f;

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
        private float _flashCooldown; // under constant fire the flash must blink, not stay white
        private float _originX;
        private bool _hasSlot;
        private Vector2 _slot;
        private float _slotTimer;
        private float _stunTimer;
        private float _slowTimer;
        private float _slowFactor = 1f;
        private int _burstLeft;
        private float _burstTimer;
        private float _spin;
        private float _lungeTimer;
        private float _lungeLeft;
        private float _laserTimer = -1f; // counts down warn + fire; < 0 = idle
        private SpriteRenderer _beam;
        private SpriteRenderer _warn;
        private static readonly Color TelegraphColor = new(1f, 0.5f, 0.42f, 1f);

        public int Id { get; private set; }
        public float HitRadius => hitRadius;
        public int XpValue => xpValue;
        public int GoldValue => goldValue;
        public bool IsDead => _hp <= 0f;
        public float CurrentHp => _hp;
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
            SeenAt = -1f;
        }

        /// <summary>EnemyManager clock time when this enemy first entered the playfield (−1 = not yet). Power Match reads it.</summary>
        public float SeenAt { get; set; } = -1f;

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
            _burstLeft = 0;
            _spin = 0f;
            _lungeTimer = lungeInterval * Random.Range(0.6f, 1f);
            _lungeLeft = 0f;
            StopLaser();
            if (shotTelegraphSeconds > 0f && spriteRenderer != null) spriteRenderer.color = Color.white;
            SetFlash(0f);
            if (_baseScale == Vector3.zero) _baseScale = transform.localScale;
            transform.localScale = _baseScale;
            _punch = 0f;
            _wobblePhase = Random.value * 6.28f;
        }

        public virtual void OnDespawned() => StopLaser();

        /// <summary>Called by EnemyManager when this enemy is killed (not when it leaks): the gum balloon's pop.</summary>
        public virtual void OnKilled()
        {
            StopLaser();
            BulletSystem bullets = BulletSystem.Instance;
            if (deathBurstBullets <= 0 || bullets == null) return;
            float start = Random.value * 360f;
            for (int i = 0; i < deathBurstBullets; i++)
            {
                float a = (start + 360f * i / deathBurstBullets) * Mathf.Deg2Rad;
                bullets.SpawnEnemyBullet(bulletTypeIndex, transform.position, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * deathBurstSpeed, bulletDamage);
            }
        }

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
            if (_laserTimer >= 0f) moveDt *= 0.2f; // the UFO all but stops while it charges and fires
            if (_hasSlot) TickFormation(moveDt);
            else TickSwarm(moveDt, playerPosition);

            AnimateScale(dt);
            _flashCooldown -= dt;
            if (_flashTimer > 0f)
            {
                _flashTimer -= dt;
                if (_flashTimer <= 0f) SetFlash(0f);
            }

            // Stunned enemies cannot shoot (Spark Cat Lv3, weapon-system.md §3.2).
            if (_stunTimer > 0f)
            {
                _stunTimer -= dt;
                _burstLeft = 0;
                StopLaser();
                Telegraph(0f);
                return;
            }

            // Nothing shoots from off-screen (fairness, and it cannot be shot back yet).
            if (EnemyManager.Instance != null && !EnemyManager.Instance.IsOnScreen(this))
            {
                StopLaser();
                return;
            }

            if (_laserTimer >= 0f) TickLaser(dt);

            // The rest of a burst (cookie robot, ice-cream spiral).
            if (_burstLeft > 0)
            {
                _burstTimer -= dt;
                if (_burstTimer <= 0f)
                {
                    _burstLeft--;
                    _burstTimer = burstGap;
                    FireVolley(playerPosition);
                }
            }

            // Aggression ramp: enemies fire more often as the run goes on (threat.md §3.1),
            // so a strong late build still has to dodge (device feedback: late game too easy).
            float minutes = EnemyManager.Instance != null ? EnemyManager.Instance.ThreatMinutes : 0f;
            _fireTimer -= dt * Formulas.EnemyFireRateScale(minutes);
            // A warm pulse before the shot says "this one is about to fire" (enemy-attacks.md §3.2).
            if (shotTelegraphSeconds > 0f)
                Telegraph(_fireTimer < shotTelegraphSeconds ? 1f - _fireTimer / shotTelegraphSeconds : 0f);
            if (_fireTimer <= 0f)
            {
                _fireTimer = fireInterval;
                Telegraph(0f);
                if (laserBeamSprite != null) StartLaser();
                else
                {
                    FireVolley(playerPosition);
                    _burstLeft = burstCount - 1;
                    _burstTimer = burstGap;
                }
            }
        }

        private void Telegraph(float amount)
        {
            if (shotTelegraphSeconds <= 0f || spriteRenderer == null) return;
            float pulse = amount > 0f ? 0.55f + 0.45f * Mathf.Sin(_age * 30f) : 0f;
            spriteRenderer.color = Color.Lerp(Color.white, TelegraphColor, amount * pulse);
        }

        private void TickSwarm(float dt, Vector2 playerPosition)
        {
            Vector3 p = transform.position;
            p.y += velocity.y * dt;
            _originX += velocity.x * dt;
            // Elite lunge: every few seconds it rushes at the ship for a moment (enemy-attacks.md §3.2).
            if (lungeInterval > 0f)
            {
                if (_lungeLeft > 0f)
                {
                    _lungeLeft -= dt;
                    Vector2 to = playerPosition - (Vector2)p;
                    Vector2 step = to.normalized * lungeSpeed * dt;
                    _originX += step.x;
                    p.y += step.y;
                }
                else if ((_lungeTimer -= dt) <= 0f)
                {
                    _lungeTimer = lungeInterval;
                    _lungeLeft = lungeSeconds;
                }
            }
            p.x = _originX + Mathf.Sin(_age * swayFrequency) * swayAmplitude;
            if (EnemyManager.Instance != null) p.x = EnemyManager.Instance.ClampToPlayfieldX(p.x, hitRadius);
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
            if (_flashCooldown <= 0f)
            {
                _flashTimer = 0.06f; // one-frame-ish white flash (art-bible §5.1)
                _flashCooldown = 0.16f;
                SetFlash(1f);
                _punch = 1f;
            }
            return IsDead;
        }

        protected virtual void FireVolley(Vector2 playerPosition)
        {
            BulletSystem bullets = BulletSystem.Instance;
            if (bullets == null || bulletsPerVolley <= 0) return;

            Vector2 origin = transform.position;
            Vector2 forward = aimAtPlayer ? (playerPosition - origin).normalized : Vector2.down;
            float baseAngle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
            if (!aimAtPlayer)
            {
                baseAngle += _spin;
                _spin += spinPerShot;
            }
            float spread = volleySpreadDegrees;
            if (ringGapDegrees > 0f)
            {
                // Full ring with one gap somewhere random: find the gap and slip through it.
                spread = 360f - ringGapDegrees;
                baseAngle = Random.value * 360f;
            }
            float step = bulletsPerVolley > 1 ? spread / (bulletsPerVolley - 1) : 0f;
            float start = baseAngle - spread * 0.5f;

            for (int i = 0; i < bulletsPerVolley; i++)
            {
                float a = (start + step * i) * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                bullets.SpawnEnemyBullet(bulletTypeIndex, origin, dir * bulletSpeed, bulletDamage,
                    splitFuse > 0f ? splitFuse : 8f, true, splitFuse > 0f ? splitTypeIndex : -1);
            }
        }

        // ---------------------------------------------------------------- Laser (donut UFO)

        private void StartLaser()
        {
            if (_beam == null)
            {
                _beam = MakeBeamRenderer("LaserBeam", laserBeamSprite);
                _warn = MakeBeamRenderer("LaserWarning", laserWarningSprite);
            }
            _laserTimer = laserWarnSeconds + laserFireSeconds;
        }

        private SpriteRenderer MakeBeamRenderer(string name, Sprite sprite)
        {
            var go = new GameObject(name);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            if (spriteRenderer != null)
            {
                r.sharedMaterial = _normalMaterial != null ? _normalMaterial : spriteRenderer.sharedMaterial;
                r.sortingLayerID = spriteRenderer.sortingLayerID;
                r.sortingOrder = spriteRenderer.sortingOrder - 1; // under the UFO, over the background
            }
            go.SetActive(false);
            return r;
        }

        private void TickLaser(float dt)
        {
            _laserTimer -= dt;
            if (_laserTimer < 0f)
            {
                StopLaser();
                return;
            }
            bool firing = _laserTimer < laserFireSeconds;
            Camera cam = Camera.main;
            float bottom = cam != null ? cam.transform.position.y - cam.orthographicSize - 1f : -12f;
            Vector3 top = transform.position;
            float length = Mathf.Max(0.1f, top.y - bottom);
            PlaceBeam(_warn, !firing, top, length, 0.1f, 0.6f + 0.4f * Mathf.Sin(_age * 25f));
            PlaceBeam(_beam, firing, top, length, laserHalfWidth * 2.4f, 1f);

            PlayerHealth player = PlayerHealth.Instance;
            if (firing && player != null && player.IsAlive)
            {
                Vector2 pp = player.transform.position;
                if (pp.y < top.y && Mathf.Abs(pp.x - top.x) < laserHalfWidth + player.HitRadius)
                {
                    float minutes = EnemyManager.Instance != null ? EnemyManager.Instance.ThreatMinutes : 0f;
                    player.TakeDamage(bulletDamage * Formulas.EnemyDamageScale(minutes) * Meta.Maps.Current.DamageMultiplier); // i-frames: once per ~0.9 s
                }
            }
        }

        private static void PlaceBeam(SpriteRenderer r, bool on, Vector3 top, float length, float width, float alpha)
        {
            if (r == null) return;
            if (r.gameObject.activeSelf != on) r.gameObject.SetActive(on);
            if (!on || r.sprite == null) return;
            Vector2 size = r.sprite.bounds.size;
            r.transform.position = new Vector3(top.x, top.y - length * 0.5f, top.z + 0.01f);
            r.transform.localScale = new Vector3(width / size.x, length / size.y, 1f);
            Color c = r.color;
            c.a = alpha;
            r.color = c;
        }

        private void StopLaser()
        {
            _laserTimer = -1f;
            if (_beam != null) _beam.gameObject.SetActive(false);
            if (_warn != null) _warn.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_beam != null) Destroy(_beam.gameObject);
            if (_warn != null) Destroy(_warn.gameObject);
        }

        private void SetFlash(float amount)
        {
            if (spriteRenderer == null || flashMaterial == null) return;
            if (_normalMaterial == null) _normalMaterial = spriteRenderer.sharedMaterial;
            spriteRenderer.sharedMaterial = amount > 0f ? flashMaterial : _normalMaterial;
        }
    }
}
