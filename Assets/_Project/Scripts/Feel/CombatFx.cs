using System.Collections.Generic;
using PofudukFilo.Enemies;
using UnityEngine;

namespace PofudukFilo.Feel
{
    /// <summary>
    /// Hit and kill feedback (art-bible §5): a white flash ring, a colour pop, star sparks and
    /// confetti on every kill (bigger plus a shake for elites and bosses), and floating damage
    /// numbers that merge while one enemy keeps taking hits, so a hail of bullets reads as one
    /// climbing number instead of a wall of text.
    /// </summary>
    public sealed class CombatFx : MonoBehaviour
    {
        [SerializeField] private int maxNumbers = 40;
        [SerializeField] private float numberLifetime = 0.65f;
        [SerializeField] private float mergeWindow = 0.3f;
        [SerializeField] private int sortingOrder = 70;
        [Tooltip("Optional; falls back to the built-in font.")]
        [SerializeField] private Font font;

        private sealed class Number
        {
            public TextMesh Text;
            public TextMesh Shadow;
            public Transform Root;
            public int EnemyId;
            public float Value;
            public float Age;
            public float Pop;
            public bool Big;
            public Vector3 Anchor;
        }

        private readonly List<Number> _active = new(48);
        private readonly Stack<Number> _free = new();
        private readonly Dictionary<int, Number> _byEnemy = new();
        private Font _font;
        private float _avgDamage = 10f;

        private static VfxSystem Vfx => VfxSystem.Instance;

        private void Start()
        {
            _font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (EnemyManager.Instance != null)
            {
                EnemyManager.Instance.EnemyDamaged += OnDamaged;
                EnemyManager.Instance.EnemyKilled += OnKilled;
            }
        }

        private void OnDestroy()
        {
            if (EnemyManager.Instance == null) return;
            EnemyManager.Instance.EnemyDamaged -= OnDamaged;
            EnemyManager.Instance.EnemyKilled -= OnKilled;
        }

        private void OnKilled(Enemy e)
        {
            if (Vfx == null) return;
            Vector2 p = e.transform.position;
            float size = Mathf.Max(0.6f, e.VisualScale);
            bool big = e.IsElite || e is BossEnemy;
            Color c = e.FxColor;
            Color light = Color.Lerp(c, Color.white, 0.55f);

            Vfx.Pop(p, size * 0.55f, new Color(1f, 1f, 1f, 0.9f), 0.12f);
            Vfx.Pop(p, size * (big ? 2.2f : 1.1f), new Color(c.r, c.g, c.b, 0.55f), big ? 0.45f : 0.28f);
            Vfx.Sparks(p, light, big ? 22 : 7, big ? 10f : 6.5f, big ? 0.45f : 0.3f);
            Vfx.Confetti(p, c, big ? 28 : 5);
            if (big && Juice.Instance != null)
            {
                Juice.Instance.Shake(e is BossEnemy ? 1f : 0.35f, e is BossEnemy ? 0.5f : 0.2f);
                Juice.Instance.Hitstop(e is BossEnemy ? 0.12f : 0.03f);
            }
        }

        private void OnDamaged(Enemy e, float damage)
        {
            if (!Core.GameSettings.DamageNumbers) return;
            _avgDamage = Mathf.Lerp(_avgDamage, damage, 0.02f);

            if (_byEnemy.TryGetValue(e.Id, out Number n) && n.Age < mergeWindow)
            {
                n.Value += damage;
                n.Age = 0f;
                n.Pop = 1f;
            }
            else
            {
                if (_active.Count >= maxNumbers) Recycle(0);
                n = Rent();
                n.EnemyId = e.Id;
                n.Value = damage;
                n.Age = 0f;
                n.Pop = 1f;
                n.Anchor = e.transform.position + new Vector3(Random.Range(-0.3f, 0.3f), 0.35f, 0f);
                _byEnemy[e.Id] = n;
                _active.Add(n);
            }

            n.Big = n.Value >= _avgDamage * 3f;
            string label = Mathf.CeilToInt(n.Value).ToString();
            n.Text.text = label;
            n.Shadow.text = label;
            n.Text.color = n.Big ? new Color(1f, 0.84f, 0.3f) : Color.white;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Number n = _active[i];
                n.Age += dt;
                if (n.Age >= numberLifetime)
                {
                    Recycle(i);
                    continue;
                }
                n.Pop = Mathf.MoveTowards(n.Pop, 0f, dt * 7f);
                float t = n.Age / numberLifetime;
                float scale = (n.Big ? 1.35f : 1f) * (1f + n.Pop * 0.5f) * (1f - t * t * 0.4f);
                n.Root.position = n.Anchor + new Vector3(0f, t * 0.9f, 0f);
                n.Root.localScale = new Vector3(scale, scale, 1f);
                float alpha = t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;
                Color c = n.Text.color;
                c.a = alpha;
                n.Text.color = c;
                n.Shadow.color = new Color(0.23f, 0.16f, 0.31f, alpha * 0.9f);
            }
        }

        private Number Rent()
        {
            if (_free.Count > 0)
            {
                Number f = _free.Pop();
                f.Root.gameObject.SetActive(true);
                return f;
            }

            var root = new GameObject("dmg-number").transform;
            root.SetParent(transform, false);
            var n = new Number
            {
                Root = root,
                Shadow = MakeText(root, new Vector3(0.05f, -0.05f, 0f), sortingOrder),
                Text = MakeText(root, Vector3.zero, sortingOrder + 1)
            };
            return n;
        }

        private TextMesh MakeText(Transform parent, Vector3 offset, int order)
        {
            var go = new GameObject("t");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = offset;
            var t = go.AddComponent<TextMesh>();
            t.font = _font;
            t.fontSize = 64;
            t.characterSize = 0.075f;
            t.fontStyle = FontStyle.Bold;
            t.anchor = TextAnchor.MiddleCenter;
            t.alignment = TextAlignment.Center;
            var mr = go.GetComponent<MeshRenderer>();
            if (_font != null) mr.sharedMaterial = _font.material;
            mr.sortingOrder = order;
            return t;
        }

        private void Recycle(int index)
        {
            Number n = _active[index];
            _active.RemoveAt(index);
            if (_byEnemy.TryGetValue(n.EnemyId, out Number cur) && cur == n) _byEnemy.Remove(n.EnemyId);
            n.Root.gameObject.SetActive(false);
            _free.Push(n);
        }
    }
}
