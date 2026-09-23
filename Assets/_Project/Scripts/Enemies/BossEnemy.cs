using System;
using PofudukFilo.Bullets;
using PofudukFilo.Feel;
using UnityEngine;

namespace PofudukFilo.Enemies
{
    public enum BossPatternKind
    {
        /// <summary>Evenly spaced ring; rotates by <c>spinPerVolley</c> each time.</summary>
        Ring,
        /// <summary>Fan aimed at the player.</summary>
        AimedFan,
        /// <summary>A few arms that sweep round over successive volleys.</summary>
        Spiral,
        /// <summary>A curtain falling straight down with gaps (dodge lanes).</summary>
        Rain
    }

    [Serializable]
    public struct BossPattern
    {
        public BossPatternKind kind;
        public int bullets;
        public float speed;
        public float spreadDegrees;
        public float spinPerVolley;
        public float damage;
        public int bulletTypeIndex;
        [Tooltip("Special attack: telegraphed ≥0.8 s, red-edged, cannot be absorbed (art-bible §3.2).")]
        public bool special;
    }

    [Serializable]
    public sealed class BossPhase
    {
        [Tooltip("Phase starts when HP fraction drops to or below this (first phase = 1).")]
        [Range(0f, 1f)] public float hpThreshold = 1f;
        public float fireInterval = 1.2f;
        public BossPattern[] patterns = Array.Empty<BossPattern>();
    }

    /// <summary>
    /// Multi-phase boss (Queen Hen etc.). Cycles the current phase's patterns; phase changes on HP
    /// thresholds with a shake and a flush (the boss "gets cross"). Special attacks are announced
    /// by a pulsing coral warning circle before they fire.
    /// </summary>
    public sealed class BossEnemy : Enemy
    {
        [SerializeField] private BossPhase[] phases = Array.Empty<BossPhase>();
        [SerializeField] private float telegraphSeconds = 0.8f;
        [SerializeField] private int specialBulletTypeIndex;
        [SerializeField] private Color warningColor = new(1f, 0.42f, 0.37f, 0.45f); // mercan
        [SerializeField] private Color enragedTint = new(1f, 0.8f, 0.85f);

        public event Action<BossEnemy, int> PhaseChanged;

        private int _phase;
        private int _patternIndex;
        private float _spin;
        private float _telegraphTimer = -1f;
        private BossPattern _queuedSpecial;
        private Vector2 _lastPlayerPosition;

        public int Phase => _phase;

        protected override bool CanBeStunned => false;

        public override void OnSpawned()
        {
            base.OnSpawned();
            _phase = 0;
            _patternIndex = 0;
            _spin = 0f;
            _telegraphTimer = -1f;
            if (phases.Length > 0) FireInterval = phases[0].fireInterval;
            if (SpriteRenderer != null) SpriteRenderer.color = Color.white;
        }

        public override void Tick(float dt, Vector2 playerPosition)
        {
            _lastPlayerPosition = playerPosition;
            UpdatePhase();

            if (_telegraphTimer >= 0f)
            {
                _telegraphTimer -= dt;
                if (_telegraphTimer < 0f) Emit(_queuedSpecial, playerPosition);
            }

            base.Tick(dt, playerPosition);
        }

        private void UpdatePhase()
        {
            int next = _phase;
            while (next + 1 < phases.Length && HpFraction <= phases[next + 1].hpThreshold) next++;
            if (next == _phase) return;

            _phase = next;
            _patternIndex = 0;
            FireInterval = phases[_phase].fireInterval;
            if (SpriteRenderer != null) SpriteRenderer.color = Color.Lerp(Color.white, enragedTint, _phase / (float)Mathf.Max(1, phases.Length - 1));
            if (Juice.Instance != null) Juice.Instance.Shake(0.7f, 0.35f);
            PhaseChanged?.Invoke(this, _phase);
        }

        protected override void FireVolley(Vector2 playerPosition)
        {
            if (phases.Length == 0 || _telegraphTimer >= 0f) return;

            BossPattern[] patterns = phases[_phase].patterns;
            if (patterns.Length == 0) return;

            BossPattern pattern = patterns[_patternIndex % patterns.Length];
            _patternIndex++;

            if (pattern.special)
            {
                _queuedSpecial = pattern;
                _telegraphTimer = telegraphSeconds;
                if (VfxSystem.Instance != null)
                    VfxSystem.Instance.Pop(transform.position, 2.5f, warningColor, telegraphSeconds);
                return;
            }

            Emit(pattern, playerPosition);
        }

        private void Emit(in BossPattern p, Vector2 playerPosition)
        {
            BulletSystem bullets = BulletSystem.Instance;
            if (bullets == null) return;

            Vector2 origin = transform.position;
            int type = p.special ? specialBulletTypeIndex : p.bulletTypeIndex;
            int count = Mathf.Max(p.kind == BossPatternKind.Rain ? 5 : 1, p.bullets);
            bool absorbable = !p.special;

            switch (p.kind)
            {
                case BossPatternKind.Ring:
                    for (int i = 0; i < count; i++)
                        Shoot(bullets, type, origin, _spin + i * 360f / count, p, absorbable);
                    _spin += p.spinPerVolley;
                    break;

                case BossPatternKind.AimedFan:
                    Vector2 to = playerPosition - origin;
                    float aim = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
                    for (int i = 0; i < count; i++)
                    {
                        float t = count == 1 ? 0f : i / (count - 1f) - 0.5f;
                        Shoot(bullets, type, origin, aim + t * p.spreadDegrees, p, absorbable);
                    }
                    break;

                case BossPatternKind.Spiral:
                    int arms = Mathf.Max(1, Mathf.RoundToInt(p.spreadDegrees));
                    for (int i = 0; i < arms; i++)
                        Shoot(bullets, type, origin, _spin + i * 360f / arms, p, absorbable);
                    _spin += p.spinPerVolley;
                    break;

                case BossPatternKind.Rain:
                    Camera cam = Camera.main;
                    float halfW = cam != null ? cam.orthographicSize * cam.aspect : 5f;
                    float top = cam != null ? cam.transform.position.y + cam.orthographicSize : origin.y + 3f;
                    int gap = UnityEngine.Random.Range(1, count - 1);
                    for (int i = 0; i < count; i++)
                    {
                        if (Mathf.Abs(i - gap) <= 1) continue; // always leave a 3-wide dodge lane
                        float x = -halfW + (i + 0.5f) * (2f * halfW / count);
                        bullets.SpawnEnemyBullet(type, new Vector2(x, top), Vector2.down * p.speed, p.damage, 8f, absorbable);
                    }
                    break;
            }
        }

        private static void Shoot(BulletSystem bullets, int type, Vector2 origin, float angleDeg, in BossPattern p, bool absorbable)
        {
            float a = angleDeg * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            bullets.SpawnEnemyBullet(type, origin, dir * p.speed, p.damage, 8f, absorbable);
        }
    }
}
