using UnityEngine;

namespace PofudukFilo.Player
{
    /// <summary>
    /// Makes the ship feel flown rather than dragged: twin engine flames that flicker and stretch
    /// with speed, a bank into horizontal moves, a gentle idle hover, and a blink while
    /// invulnerable after a hit. Purely cosmetic — gameplay uses the root transform.
    /// </summary>
    public sealed class ShipVisual : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        [SerializeField] private SpriteRenderer shipRenderer;
        [SerializeField] private Sprite flameSprite;
        [SerializeField] private Material material;
        [SerializeField] private Vector2 engineOffset = new(0.117f, -0.2f);
        [SerializeField] private Color flameOuter = new(0.25f, 0.95f, 0.85f, 0.85f);
        [SerializeField] private Color flameCore = new(1f, 1f, 1f, 0.95f);
        [SerializeField] private float maxBankDegrees = 14f;
        [Header("Guns (device feedback 2026-09-24: 'how does it shoot?')")]
        [Tooltip("Blaster art pointing right; mounted pointing up on both wings.")]
        [SerializeField] private Sprite gunSprite;
        [SerializeField] private Vector2 gunOffset = new(0.3f, -0.1f);
        [SerializeField] private float gunSize = 0.24f;
        [SerializeField] private Color muzzleColor = new(0.55f, 1f, 0.9f, 0.9f);

        private Transform[] _guns;
        private float _recoil;

        private Transform[] _outer;
        private Transform[] _core;
        private Vector3 _lastPosition;
        private float _bank;
        private float _speed;
        private Vector3 _visualBase;
        private PlayerHealth _health;
        private SpriteRenderer[] _renderers;
        private float _hurt; // 1 → 0 after a hit: red flash + wobble
        private bool _shown = true;

        private void Start()
        {
            _health = GetComponent<PlayerHealth>();
            if (_health != null) _health.Damaged += OnHurt;
            _lastPosition = transform.position;
            if (visual != null) _visualBase = visual.localPosition;
            BuildGuns();
            Weapons.WeaponBehaviour.AnyFired += OnFired;
            if (flameSprite == null) return;

            _outer = new Transform[2];
            _core = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -engineOffset.x : engineOffset.x;
                _outer[i] = Flame($"flame-{i}", new Vector3(x, engineOffset.y, 0f), flameOuter, 9);
                _core[i] = Flame($"flame-core-{i}", new Vector3(x, engineOffset.y + 0.02f, 0f), flameCore, 9);
            }
            _renderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        private void OnDestroy() => Weapons.WeaponBehaviour.AnyFired -= OnFired;

        private void BuildGuns()
        {
            if (gunSprite == null || visual == null) return;
            _guns = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject(i == 0 ? "Gun-L" : "Gun-R");
                go.transform.SetParent(visual, false);
                go.transform.localPosition = new Vector3(i == 0 ? -gunOffset.x : gunOffset.x, gunOffset.y, 0f);
                go.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                float s = gunSize / Mathf.Max(0.01f, gunSprite.bounds.size.x);
                go.transform.localScale = new Vector3(s, i == 0 ? s : -s, 1f); // mirror the right gun
                var r = go.AddComponent<SpriteRenderer>();
                r.sprite = gunSprite;
                r.sortingOrder = 11;
                if (material != null) r.sharedMaterial = material;
                _guns[i] = go.transform;
            }
        }

        /// <summary>Recoil kick and a muzzle pop on both guns, at most every 0.08 s so rapid fire stays readable.</summary>
        private float _nextFlash;

        private void OnFired(Weapons.WeaponBehaviour weapon)
        {
            // Only the main gun fires from the wing barrels; orbiters, cats and mortars have their own visuals.
            if (_guns == null || !_shown || !(weapon is Weapons.FeatherBlaster)) return;
            _recoil = 1f;
            if (Time.time < _nextFlash || Feel.VfxSystem.Instance == null) return;
            _nextFlash = Time.time + 0.08f;
            foreach (Transform g in _guns)
                Feel.VfxSystem.Instance.Pop((Vector2)g.position + Vector2.up * gunSize * 0.55f, 0.28f, muzzleColor, 0.09f);
        }

        private void OnHurt(float damage)
        {
            _hurt = 1f;
            if (Feel.Juice.Instance != null)
            {
                Feel.Juice.Instance.Shake(0.55f, 0.3f);
                Feel.Juice.Instance.Hitstop(0.07f);
                Feel.Juice.Instance.Haptic();
            }
            if (Feel.VfxSystem.Instance != null)
            {
                Feel.VfxSystem.Instance.Pop(transform.position, 1.1f, new Color(1f, 0.25f, 0.3f, 0.75f), 0.25f);
                Feel.VfxSystem.Instance.Sparks(transform.position, new Color(1f, 0.45f, 0.4f), 10, 7f, 0.3f);
            }
        }

        /// <summary>The ship only exists in play; menus and the run-end screen show the hero art instead.</summary>
        private void SetShown(bool show)
        {
            if (show == _shown) return;
            _shown = show;
            if (_renderers == null) _renderers = GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer r in _renderers) r.enabled = show;
        }

        private Transform Flame(string name, Vector3 local, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = local;
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = flameSprite;
            r.color = color;
            r.sortingOrder = order;
            if (material != null) r.sharedMaterial = material;
            return go.transform;
        }

        private void LateUpdate()
        {
            Core.RunController run = Core.RunController.Instance;
            SetShown(run == null || run.State is not (Core.GameState.MainMenu or Core.GameState.RunEnd));
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f) return;
            Vector3 delta = transform.position - _lastPosition;
            _lastPosition = transform.position;
            float vx = Time.deltaTime > 0f ? delta.x / Time.deltaTime : 0f;
            float vy = Time.deltaTime > 0f ? delta.y / Time.deltaTime : 0f;
            _speed = Mathf.Lerp(_speed, Mathf.Sqrt(vx * vx + vy * vy), 1f - Mathf.Exp(-dt * 10f));
            _bank = Mathf.Lerp(_bank, Mathf.Clamp(-vx * 2.2f, -maxBankDegrees, maxBankDegrees), 1f - Mathf.Exp(-dt * 12f));

            _hurt = Mathf.MoveTowards(_hurt, 0f, dt * 2.5f);
            if (visual != null)
            {
                float hover = Mathf.Sin(Time.time * 3.1f) * 0.03f;
                // Stagger: a decaying shiver and a lurch.
                float wobble = Mathf.Sin(Time.unscaledTime * 55f) * 18f * _hurt * _hurt;
                Vector3 jitter = _hurt > 0f ? (Vector3)(Random.insideUnitCircle * 0.06f * _hurt) : Vector3.zero;
                visual.localPosition = _visualBase + new Vector3(0f, hover, 0f) + jitter;
                if (_guns != null)
                {
                    _recoil = Mathf.MoveTowards(_recoil, 0f, dt * 10f);
                    for (int i = 0; i < _guns.Length; i++)
                        _guns[i].localPosition = new Vector3(i == 0 ? -gunOffset.x : gunOffset.x, gunOffset.y - _recoil * 0.05f, 0f);
                }
                visual.localRotation = Quaternion.Euler(0f, 0f, _bank + wobble);
            }

            if (_outer != null)
            {
                // Flames stretch when flying up/fast and flicker every frame.
                float boost = Mathf.Clamp01(_speed / 12f) + Mathf.Clamp01(vy / 10f) * 0.5f;
                for (int i = 0; i < 2; i++)
                {
                    float flicker = 0.85f + Random.value * 0.3f;
                    float len = (0.22f + 0.22f * boost) * flicker;
                    _outer[i].localScale = new Vector3(0.13f, len, 1f);
                    _outer[i].localPosition = new Vector3(_outer[i].localPosition.x, engineOffset.y - len * 0.45f, 0f);
                    _core[i].localScale = new Vector3(0.06f, len * 0.55f, 1f);
                    _core[i].localPosition = new Vector3(_core[i].localPosition.x, engineOffset.y - len * 0.3f, 0f);
                }
            }

            if (shipRenderer != null && _health != null)
            {
                bool blinkOff = _health.IsInvulnerable && Mathf.Repeat(Time.time * 12f, 1f) < 0.5f;
                Color c = Color.Lerp(Color.white, new Color(1f, 0.35f, 0.35f), Mathf.Clamp01(_hurt * 1.6f));
                c.a = blinkOff ? 0.35f : 1f;
                shipRenderer.color = c;
            }
        }
    }
}
