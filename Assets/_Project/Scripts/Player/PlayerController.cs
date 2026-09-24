using PofudukFilo.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace PofudukFilo.Player
{
    /// <summary>
    /// Relative drag: the ship moves by the finger's delta, so it never hides under the thumb
    /// (game-concept.md §3.1). Mouse drag is supported for editor testing.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private float sensitivity = 1.25f;
        [Tooltip("Fraction of the screen height (from the bottom) the ship may occupy.")]
        [SerializeField, Range(0.3f, 1f)] private float playableHeightFraction = 0.65f;
        [SerializeField] private float edgePadding = 0.35f;
        [SerializeField] private bool slowTimeWhenFingerLifted = true;

        [Header("Hit stagger")]
        [SerializeField] private float staggerSeconds = 0.4f;
        [Tooltip("Share of finger movement that still steers the ship while staggered.")]
        [SerializeField, Range(0f, 1f)] private float staggerControl = 0.3f;
        [SerializeField] private float knockbackSpeed = 7f;

        private Camera _camera;
        private bool _wasTouching; // no slow-down before the first touch of the run
        private float _staggerLeft;
        private Vector2 _knock;
        private PlayerHealth _health;

        /// <summary>
        /// Knock the ship back and dull the controls briefly, so a hit is felt in the hands and not
        /// only seen in the HP bar. Direction defaults to "down and away from the centre".
        /// </summary>
        public void Stagger(Vector2 direction)
        {
            if (direction == Vector2.zero) direction = new Vector2(-Mathf.Sign(transform.position.x + 0.001f) * 0.4f, -1f);
            _knock = direction.normalized * knockbackSpeed;
            _staggerLeft = staggerSeconds;
        }

        private void Start()
        {
            _health = GetComponent<PlayerHealth>();
            if (_health != null) _health.Damaged += _ => Stagger(Vector2.zero);
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            _camera = Camera.main;
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
            TimeScaleController.SetFingerLifted(false);
        }

        private void Update()
        {
            if (TimeScaleController.IsPaused) return;

            bool touching = TryGetDragDelta(out Vector2 pixelDelta);
            if (slowTimeWhenFingerLifted && touching != _wasTouching)
            {
                TimeScaleController.SetFingerLifted(!touching);
                _wasTouching = touching;
            }

            Vector3 p = transform.position;
            if (_staggerLeft > 0f)
            {
                _staggerLeft -= Time.deltaTime;
                p += (Vector3)(_knock * Time.deltaTime);
                _knock = Vector2.Lerp(_knock, Vector2.zero, 1f - Mathf.Exp(-Time.deltaTime * 9f));
            }
            if (touching)
            {
                float control = _staggerLeft > 0f ? staggerControl : 1f;
                float worldPerPixel = 2f * _camera.orthographicSize / Screen.height;
                p += (Vector3)(pixelDelta * (worldPerPixel * sensitivity * control));
            }
            if (touching || _staggerLeft > 0f) transform.position = Clamp(p);
        }

        private static bool TryGetDragDelta(out Vector2 delta)
        {
            if (Touch.activeTouches.Count > 0)
            {
                delta = Touch.activeTouches[0].delta;
                return true;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                delta = mouse.delta.ReadValue();
                return true;
            }

            return TryLegacyTouch(out delta);
        }

        private static bool s_legacyInputUnavailable;

        /// <summary>
        /// Fallback for builds where the Input System backend is not active (Player Settings ▸ Active
        /// Input Handling = Input Manager). Throws when only the new system is enabled, so it disables itself.
        /// </summary>
        private static bool TryLegacyTouch(out Vector2 delta)
        {
            delta = Vector2.zero;
            if (s_legacyInputUnavailable) return false;
            try
            {
                if (UnityEngine.Input.touchCount == 0) return false;
                delta = UnityEngine.Input.GetTouch(0).deltaPosition;
                return true;
            }
            catch (System.InvalidOperationException)
            {
                s_legacyInputUnavailable = true;
                return false;
            }
        }

        private Vector3 Clamp(Vector3 p)
        {
            float halfH = _camera.orthographicSize;
            float halfW = halfH * _camera.aspect;
            Vector3 c = _camera.transform.position;

            float minY = c.y - halfH + edgePadding;
            float maxY = c.y - halfH + 2f * halfH * playableHeightFraction;
            p.x = Mathf.Clamp(p.x, c.x - halfW + edgePadding, c.x + halfW - edgePadding);
            p.y = Mathf.Clamp(p.y, minY, maxY);
            return p;
        }
    }
}
