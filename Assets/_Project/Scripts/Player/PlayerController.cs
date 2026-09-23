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

        private Camera _camera;
        private bool _wasTouching; // no slow-down before the first touch of the run

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
            if (!touching) return;

            float worldPerPixel = 2f * _camera.orthographicSize / Screen.height;
            Vector3 p = transform.position + (Vector3)(pixelDelta * (worldPerPixel * sensitivity));
            transform.position = Clamp(p);
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

            delta = Vector2.zero;
            return false;
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
