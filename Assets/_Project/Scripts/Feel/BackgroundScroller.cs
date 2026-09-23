using System.Collections.Generic;
using UnityEngine;

namespace PofudukFilo.Feel
{
    /// <summary>
    /// Low-contrast parallax (art-bible §6): soft blobs in a few layers drift down and wrap.
    /// Dims slightly as the fight gets busy so the foreground can breathe (art-bible §5.3).
    /// </summary>
    public sealed class BackgroundScroller : MonoBehaviour
    {
        [SerializeField] private Sprite blobSprite;
        [SerializeField] private Material blobMaterial;
        [SerializeField] private Color[] layerColors =
        {
            new(0.97f, 0.65f, 0.76f, 0.10f),
            new(0.50f, 0.88f, 0.77f, 0.08f),
            new(1f, 0.91f, 0.64f, 0.12f)
        };
        [SerializeField] private float[] layerSpeeds = { 0.4f, 0.9f, 1.8f };
        [SerializeField] private int blobsPerLayer = 10;
        [SerializeField] private Vector2 sizeRange = new(0.6f, 3.5f);

        private struct Blob
        {
            public Transform Transform;
            public int Layer;
        }

        private readonly List<Blob> _blobs = new(32);
        private Camera _camera;

        private void Start()
        {
            _camera = Camera.main;
            if (blobSprite == null || _camera == null) return;

            for (int layer = 0; layer < layerColors.Length; layer++)
            for (int i = 0; i < blobsPerLayer; i++)
            {
                var go = new GameObject($"bg-{layer}-{i}");
                go.transform.SetParent(transform, false);
                var r = go.AddComponent<SpriteRenderer>();
                r.sprite = blobSprite;
                if (blobMaterial != null) r.sharedMaterial = blobMaterial;
                r.color = layerColors[layer];
                r.sortingOrder = -100 + layer;
                float size = Random.Range(sizeRange.x, sizeRange.y) * (1f - layer * 0.25f);
                go.transform.localScale = new Vector3(size, size, 1f);
                go.transform.position = RandomPoint(Random.Range(-1f, 1f));
                _blobs.Add(new Blob { Transform = go.transform, Layer = layer });
            }
        }

        private void Update()
        {
            if (_camera == null) return;
            float bottom = _camera.transform.position.y - _camera.orthographicSize - 4f;

            for (int i = 0; i < _blobs.Count; i++)
            {
                Blob b = _blobs[i];
                Vector3 p = b.Transform.position;
                p.y -= layerSpeeds[b.Layer % layerSpeeds.Length] * Time.deltaTime;
                if (p.y < bottom) p = RandomPoint(1.2f);
                b.Transform.position = p;
            }
        }

        private Vector3 RandomPoint(float heightFactor)
        {
            float halfW = _camera.orthographicSize * _camera.aspect + 1f;
            Vector3 c = _camera.transform.position;
            return new Vector3(c.x + Random.Range(-halfW, halfW), c.y + _camera.orthographicSize * heightFactor + 2f, 0f);
        }
    }
}
