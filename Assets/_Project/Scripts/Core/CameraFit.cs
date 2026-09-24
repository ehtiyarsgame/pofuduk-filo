using UnityEngine;

namespace PofudukFilo.Core
{
    /// <summary>
    /// Fits the gameplay camera to any portrait screen (design/ux/screen-fit.md, <see cref="Formulas.FitCamera"/>):
    /// tall phones see more height at the designed width; tablets and other wide screens get a centred 9:16
    /// column with plum bars drawn by a background camera. Runs before everything that reads the camera bounds
    /// (background, spawns, player clamp) and re-applies if the resolution changes (foldables, split screen).
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFit : MonoBehaviour
    {
        [SerializeField] private float baseSize = 10.8f;
        [SerializeField] private Color barColor = new(0.1f, 0.06f, 0.17f, 1f);

        private Camera _camera;
        private int _width;
        private int _height;

        /// <summary>The playfield's share of the screen width (1 on phones). The UI column matches it.</summary>
        public static float ViewportWidth { get; private set; } = 1f;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            var bars = new GameObject("Pillarbox Camera").AddComponent<Camera>();
            bars.clearFlags = CameraClearFlags.SolidColor;
            bars.backgroundColor = barColor;
            bars.cullingMask = 0;
            bars.depth = _camera.depth - 1;
            bars.orthographic = true;
            Apply();
        }

        private void Update()
        {
            if (Screen.width != _width || Screen.height != _height) Apply();
        }

        private void Apply()
        {
            _width = Screen.width;
            _height = Screen.height;
            (float size, float viewport) = Formulas.FitCamera(_height > 0 ? (float)_width / _height : 0f, baseSize);
            _camera.orthographicSize = size;
            _camera.rect = new Rect((1f - viewport) * 0.5f, 0f, viewport, 1f);
            ViewportWidth = viewport;
        }
    }
}
