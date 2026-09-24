using System.Collections.Generic;
using UnityEngine;

namespace PofudukFilo.Feel
{
    /// <summary>
    /// Candy-space backdrop (art-bible §6), back to front: a fixed sky gradient, a slow tiling
    /// nebula, far and near star fields at increasing parallax speeds, and a few dimmed candy
    /// planets drifting past. Everything stays low-contrast so bullets and enemies own the
    /// foreground; the near stars gently twinkle.
    /// </summary>
    public sealed class BackgroundScroller : MonoBehaviour
    {
        [SerializeField] private Material material;
        [SerializeField] private Sprite sky;
        [SerializeField] private Sprite nebula;
        [SerializeField] private Sprite farStars;
        [SerializeField] private Sprite nearStars;
        [SerializeField] private Sprite[] props = System.Array.Empty<Sprite>();

        [Header("Parallax (world units / s)")]
        [SerializeField] private float nebulaSpeed = 0.18f;
        [SerializeField] private float farSpeed = 0.45f;
        [SerializeField] private float nearSpeed = 1.3f;
        [SerializeField] private float propSpeed = 0.8f;
        [SerializeField] private Color nebulaTint = new(1f, 1f, 1f, 0.8f);
        [SerializeField] private Color propTint = new(0.62f, 0.56f, 0.78f, 0.8f);
        [SerializeField] private Vector2 propSizeRange = new(2.2f, 4.5f);
        [SerializeField] private Vector2 propGapSeconds = new(9f, 16f);

        private sealed class TileLayer
        {
            public readonly List<Transform> Tiles = new();
            public float Speed;
            public float TileHeight;
            public SpriteRenderer First;
        }

        private readonly List<TileLayer> _layers = new();
        private readonly List<SpriteRenderer> _props = new();
        private Camera _camera;
        private Transform _sky;
        private TileLayer _near;
        private float _nextProp;
        private int _propIndex;

        private void Start()
        {
            _camera = Camera.main;
            if (_camera == null) return;
            float halfH = _camera.orthographicSize, width = halfH * 2f * _camera.aspect * 1.08f;

            if (sky != null)
            {
                SpriteRenderer r = MakeRenderer("sky", sky, -200, Color.white);
                _sky = r.transform;
                Vector2 size = sky.bounds.size;
                _sky.localScale = new Vector3(width / size.x, halfH * 2.1f / size.y, 1f);
            }

            AddTiles(nebula, nebulaSpeed, -190, nebulaTint, width);
            AddTiles(farStars, farSpeed, -180, Color.white, width);
            _near = AddTiles(nearStars, nearSpeed, -170, Color.white, width);
            _nextProp = Random.Range(0f, 3f);
        }

        private TileLayer AddTiles(Sprite sprite, float speed, int order, Color tint, float width)
        {
            if (sprite == null) return null;
            float scale = width / sprite.bounds.size.x;
            var layer = new TileLayer { Speed = speed, TileHeight = sprite.bounds.size.y * scale };
            int count = Mathf.CeilToInt(_camera.orthographicSize * 2f / layer.TileHeight) + 1;
            float bottom = _camera.transform.position.y - _camera.orthographicSize;
            for (int i = 0; i < count; i++)
            {
                SpriteRenderer r = MakeRenderer($"bg-{order}-{i}", sprite, order, tint);
                r.transform.localScale = new Vector3(scale, scale, 1f);
                r.transform.position = new Vector3(_camera.transform.position.x,
                    bottom + layer.TileHeight * (i + 0.5f), 0f);
                layer.Tiles.Add(r.transform);
                if (i == 0) layer.First = r;
            }
            _layers.Add(layer);
            return layer;
        }

        private SpriteRenderer MakeRenderer(string name, Sprite sprite, int order, Color tint)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            if (material != null) r.sharedMaterial = material;
            r.sortingOrder = order;
            r.color = tint;
            return r;
        }

        private void Update()
        {
            if (_camera == null) return;
            float dt = Time.deltaTime;
            Vector3 cam = _camera.transform.position;
            if (_sky != null) _sky.position = new Vector3(cam.x, cam.y, 0f);

            float bottom = cam.y - _camera.orthographicSize;
            foreach (TileLayer layer in _layers)
            {
                float top = float.MinValue;
                foreach (Transform t in layer.Tiles) top = Mathf.Max(top, t.position.y);
                foreach (Transform t in layer.Tiles)
                {
                    Vector3 p = t.position;
                    p.y -= layer.Speed * dt;
                    if (p.y + layer.TileHeight * 0.5f < bottom)
                    {
                        p.y = top + layer.TileHeight - layer.Speed * dt; // wrap above the highest tile, seamless
                        top = p.y;
                    }
                    t.position = p;
                }
            }

            if (_near != null)
            {
                float a = 0.8f + 0.2f * Mathf.Sin(Time.time * 2.3f);
                foreach (Transform t in _near.Tiles) t.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, a);
            }

            UpdateProps(dt, cam, bottom);
        }

        private void UpdateProps(float dt, Vector3 cam, float bottom)
        {
            for (int i = _props.Count - 1; i >= 0; i--)
            {
                SpriteRenderer r = _props[i];
                if (!r.gameObject.activeSelf) continue;
                Transform t = r.transform;
                t.position += Vector3.down * (propSpeed * dt);
                t.Rotate(0f, 0f, 4f * dt);
                if (t.position.y < bottom - 6f) r.gameObject.SetActive(false);
            }

            if (props.Length == 0) return;
            _nextProp -= dt;
            if (_nextProp > 0f) return;
            _nextProp = Random.Range(propGapSeconds.x, propGapSeconds.y);

            SpriteRenderer free = _props.Find(p => !p.gameObject.activeSelf);
            if (free == null)
            {
                free = MakeRenderer("bg-prop", props[0], -175, propTint);
                _props.Add(free);
            }
            free.sprite = props[_propIndex++ % props.Length];
            free.gameObject.SetActive(true);
            float size = Random.Range(propSizeRange.x, propSizeRange.y);
            free.transform.localScale = new Vector3(size, size, 1f);
            float halfW = _camera.orthographicSize * _camera.aspect;
            free.transform.SetPositionAndRotation(
                new Vector3(cam.x + Random.Range(-halfW, halfW), cam.y + _camera.orthographicSize + size, 0f),
                Quaternion.Euler(0f, 0f, Random.Range(-20f, 20f)));
        }
    }
}
