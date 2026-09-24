using System.Collections.Generic;
using UnityEngine;

namespace PofudukFilo.Feel
{
    /// <summary>
    /// Pooled sprite objects for the few managed visuals (eggs, stars, bubbles, cats).
    /// Objects are created lazily under one parent and never destroyed during a run.
    /// </summary>
    public sealed class SpritePool
    {
        private readonly Transform _parent;
        private readonly Stack<SpriteRenderer> _free = new();
        private readonly int _sortingOrder;
        private readonly Material _material;

        public SpritePool(Transform parent, int sortingOrder, Material material)
        {
            _parent = parent;
            _sortingOrder = sortingOrder;
            _material = material;
        }

        public SpriteRenderer Get(Sprite sprite, Vector2 position, float scale, Color color)
        {
            SpriteRenderer r;
            if (_free.Count > 0)
            {
                r = _free.Pop();
                r.gameObject.SetActive(true);
            }
            else
            {
                var go = new GameObject("pooled-sprite");
                go.transform.SetParent(_parent, false);
                r = go.AddComponent<SpriteRenderer>();
                r.sortingOrder = _sortingOrder;
                if (_material != null) r.sharedMaterial = _material;
            }

            r.sprite = sprite;
            r.color = color;
            r.transform.SetPositionAndRotation(position, Quaternion.identity);
            r.transform.localScale = new Vector3(scale, scale, 1f);
            return r;
        }

        public void Release(SpriteRenderer r)
        {
            r.gameObject.SetActive(false);
            _free.Push(r);
        }
    }

    /// <summary>
    /// Cheap, soft effects that follow the art bible (§5): pastel pop circles instead of
    /// full-screen flashes, thin pastel lightning, confetti via one shared ParticleSystem.
    /// </summary>
    public sealed class VfxSystem : MonoBehaviour
    {
        public static VfxSystem Instance { get; private set; }

        [SerializeField] private Sprite circleSprite;
        [SerializeField] private Material lineMaterial;
        [Tooltip("Unlit sprite material, so pooled sprites never depend on 2D lights.")]
        [SerializeField] private Material spriteMaterial;
        [SerializeField] private ParticleSystem confetti;
        [Tooltip("Small star used by Sparks (death bursts, pickups).")]
        [SerializeField] private Sprite sparkSprite;
        [SerializeField] private int maxSparks = 220;
        [SerializeField] private int sortingOrder = 50;
        [Tooltip("Soft cap on simultaneous pop effects (VFX budget, architecture.md §7).")]
        [SerializeField] private int maxBursts = 120;

        private struct Burst
        {
            public SpriteRenderer Renderer;
            public float Age;
            public float Duration;
            public float StartScale;
            public float EndScale;
            public Color Color;
        }

        private struct Line
        {
            public LineRenderer Renderer;
            public float Age;
            public float Duration;
        }

        private struct Spark
        {
            public SpriteRenderer Renderer;
            public Vector2 Velocity;
            public float Age;
            public float Duration;
            public float Size;
            public float Spin;
            public Color Color;
        }

        private readonly List<Spark> _sparks = new(256);
        private readonly List<Burst> _bursts = new(128);
        private readonly List<Line> _lines = new(32);
        private readonly Stack<LineRenderer> _freeLines = new();
        private SpritePool _sprites;

        public Sprite CircleSprite => circleSprite;
        public SpritePool Sprites => _sprites;

        private void Awake()
        {
            Instance = this;
            _sprites = new SpritePool(transform, sortingOrder, spriteMaterial);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Expanding, fading pastel circle — explosions, pops, shockwaves.</summary>
        public void Pop(Vector2 position, float radius, Color color, float duration = 0.25f)
        {
            if (circleSprite == null) return;
            if (_bursts.Count >= maxBursts) RemoveBurst(0); // oldest first; enemy bullets are never throttled

            SpriteRenderer r = _sprites.Get(circleSprite, position, radius * 0.4f, color);
            _bursts.Add(new Burst
            {
                Renderer = r,
                Duration = duration,
                StartScale = radius * 0.4f,
                EndScale = radius * 2f,
                Color = color
            });
        }

        /// <summary>
        /// Star sparks flung outward with drag and a little gravity — the "crunch" of a kill.
        /// Unscaled time, like every effect here, so they finish during hitstop.
        /// </summary>
        public void Sparks(Vector2 position, Color color, int count, float speed = 6f, float size = 0.28f, float duration = 0.45f)
        {
            Sprite sprite = sparkSprite != null ? sparkSprite : circleSprite;
            if (sprite == null) return;
            for (int i = 0; i < count; i++)
            {
                if (_sparks.Count >= maxSparks) RemoveSpark(0);
                float a = Random.value * Mathf.PI * 2f;
                float v = speed * Random.Range(0.45f, 1f);
                float s = size * Random.Range(0.6f, 1.2f);
                SpriteRenderer r = _sprites.Get(sprite, position, s, color);
                _sparks.Add(new Spark
                {
                    Renderer = r,
                    Velocity = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * v,
                    Duration = duration * Random.Range(0.7f, 1.2f),
                    Size = s,
                    Spin = Random.Range(-720f, 720f),
                    Color = color
                });
            }
        }

        /// <summary>Confetti (hearts, stars, candy) from the shared particle system.</summary>
        public void Confetti(Vector2 position, Color color, int count = 8)
        {
            if (confetti == null) return;
            var p = new ParticleSystem.EmitParams
            {
                position = position,
                startColor = color,
                applyShapeToPosition = true
            };
            confetti.Emit(p, count);
        }

        /// <summary>Lightning / beam segment that fades out.</summary>
        public void Segment(Vector2 from, Vector2 to, Color color, float width = 0.12f, float duration = 0.12f)
        {
            if (lineMaterial == null) return;

            LineRenderer line = _freeLines.Count > 0 ? _freeLines.Pop() : CreateLine();
            line.gameObject.SetActive(true);
            line.startColor = line.endColor = color;
            line.startWidth = line.endWidth = width;
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            _lines.Add(new Line { Renderer = line, Duration = duration });
        }

        private LineRenderer CreateLine()
        {
            var go = new GameObject("vfx-line");
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = lineMaterial;
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.numCapVertices = 2;
            line.sortingOrder = sortingOrder + 1;
            return line;
        }

        private void Update()
        {
            // Unscaled so effects finish during hitstop instead of hanging mid-air.
            float dt = Time.unscaledDeltaTime;

            for (int i = _bursts.Count - 1; i >= 0; i--)
            {
                Burst b = _bursts[i];
                b.Age += dt;
                float t = b.Age / b.Duration;
                if (t >= 1f)
                {
                    RemoveBurst(i);
                    continue;
                }

                float eased = 1f - (1f - t) * (1f - t); // OutQuad
                float s = Mathf.Lerp(b.StartScale, b.EndScale, eased);
                b.Renderer.transform.localScale = new Vector3(s, s, 1f);
                Color c = b.Color;
                c.a *= 1f - t;
                b.Renderer.color = c;
                _bursts[i] = b;
            }

            for (int i = _sparks.Count - 1; i >= 0; i--)
            {
                Spark sp = _sparks[i];
                sp.Age += dt;
                float t = sp.Age / sp.Duration;
                if (t >= 1f)
                {
                    RemoveSpark(i);
                    continue;
                }
                sp.Velocity *= Mathf.Max(0f, 1f - 5f * dt);
                sp.Velocity.y -= 4f * dt;
                Transform tr = sp.Renderer.transform;
                tr.position += (Vector3)(sp.Velocity * dt);
                tr.Rotate(0f, 0f, sp.Spin * dt);
                float s = sp.Size * (1f - t * t);
                tr.localScale = new Vector3(s, s, 1f);
                Color c = sp.Color;
                c.a *= 1f - t * t;
                sp.Renderer.color = c;
                _sparks[i] = sp;
            }

            for (int i = _lines.Count - 1; i >= 0; i--)
            {
                Line l = _lines[i];
                l.Age += dt;
                if (l.Age >= l.Duration)
                {
                    l.Renderer.gameObject.SetActive(false);
                    _freeLines.Push(l.Renderer);
                    _lines.RemoveAt(i);
                    continue;
                }

                Color c = l.Renderer.startColor;
                c.a = 1f - l.Age / l.Duration;
                l.Renderer.startColor = l.Renderer.endColor = c;
                _lines[i] = l;
            }
        }

        private void RemoveSpark(int index)
        {
            _sparks[index].Renderer.transform.rotation = Quaternion.identity;
            _sprites.Release(_sparks[index].Renderer);
            _sparks.RemoveAt(index);
        }

        private void RemoveBurst(int index)
        {
            _sprites.Release(_bursts[index].Renderer);
            _bursts.RemoveAt(index);
        }
    }
}
