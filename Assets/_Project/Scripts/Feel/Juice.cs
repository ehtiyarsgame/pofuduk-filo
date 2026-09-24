using System.Collections.Generic;
using PofudukFilo.Core;
using UnityEngine;
#if DOTWEEN
using DG.Tweening;
#endif

namespace PofudukFilo.Feel
{
    /// <summary>
    /// Game-feel entry points (art-bible §5.1). DOTween is used only for rare events and UI —
    /// never per bullet (architecture.md §6). Code is guarded by the DOTWEEN scripting define
    /// so the project compiles before DOTween is imported.
    /// </summary>
    public sealed class Juice : MonoBehaviour
    {
        public static Juice Instance { get; private set; }

        [SerializeField] private Transform cameraRig;
        [SerializeField] private float maxShakeStrength = 0.35f;
        [SerializeField] private bool shakeEnabled = true;
        [SerializeField] private bool hapticsEnabled = true;

        private void Awake()
        {
            Instance = this;
#if DOTWEEN
            // Pre-size tween capacity so no allocation happens mid-run.
            DOTween.Init(recycleAllByDefault: true, useSafeMode: Debug.isDebugBuild).SetCapacity(500, 50);
#endif
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // Built-in fallbacks for when DOTween is not imported (the default): a camera shake and
        // tiny unscaled scale tweens for UI. Without them every Shake/PunchUI/PopIn was a no-op.
        private struct UiTween
        {
            public Transform Target;
            public float Delay;
            public float Age;
            public float Duration;
            public bool Pop; // true = PopIn (0 → 1, OutBack), false = punch (1 → 1.12 → 1)
        }

        private static readonly List<UiTween> s_tweens = new(16);
        private float _shakeLeft;
        private float _shakeDuration;
        private float _shakeStrength;
        private Vector3 _rigBase;
        private bool _rigBaseSet;

        private void Update()
        {
            TimeScaleController.Tick();
            float dt = Time.unscaledDeltaTime;
            for (int i = s_tweens.Count - 1; i >= 0; i--)
            {
                UiTween t = s_tweens[i];
                if (t.Target == null)
                {
                    s_tweens.RemoveAt(i);
                    continue;
                }
                if (t.Delay > 0f)
                {
                    t.Delay -= dt;
                    s_tweens[i] = t;
                    continue;
                }
                t.Age += dt;
                float k = Mathf.Clamp01(t.Age / t.Duration);
                float s = t.Pop ? OutBack(k) : 1f + Mathf.Sin(k * Mathf.PI) * 0.12f * (1f - k * 0.5f);
                t.Target.localScale = new Vector3(s, s, 1f);
                if (k >= 1f)
                {
                    t.Target.localScale = Vector3.one;
                    s_tweens.RemoveAt(i);
                }
                else s_tweens[i] = t;
            }
        }

        private void LateUpdate()
        {
#if !DOTWEEN
            if (cameraRig == null) return;
            if (!_rigBaseSet)
            {
                _rigBase = cameraRig.localPosition;
                _rigBaseSet = true;
            }
            if (_shakeLeft > 0f)
            {
                _shakeLeft -= Time.unscaledDeltaTime;
                float fade = Mathf.Clamp01(_shakeLeft / _shakeDuration);
                Vector2 o = Random.insideUnitCircle * (_shakeStrength * fade);
                cameraRig.localPosition = _rigBase + new Vector3(o.x, o.y, 0f);
            }
            else cameraRig.localPosition = _rigBase;
#endif
        }

        private static float OutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float x = t - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }

        private static void AddTween(Transform target, bool pop, float delay, float duration)
        {
            for (int i = s_tweens.Count - 1; i >= 0; i--)
                if (s_tweens[i].Target == target) s_tweens.RemoveAt(i);
            s_tweens.Add(new UiTween { Target = target, Pop = pop, Delay = delay, Duration = duration });
            if (pop) target.localScale = delay > 0f ? Vector3.zero : Vector3.one * 0.01f;
        }

        /// <summary>Elite/boss kill: 40–60 ms freeze.</summary>
        public void Hitstop(float seconds = 0.05f) => TimeScaleController.Hitstop(seconds);

        /// <summary>Big events only (evolution, boss phase, bomb). Strength 0..1.</summary>
        public void Shake(float strength01, float duration = 0.25f)
        {
            if (!shakeEnabled || !Core.GameSettings.ScreenShake || cameraRig == null) return;
#if DOTWEEN
            cameraRig.DOKill(complete: true);
            cameraRig.DOShakePosition(duration, maxShakeStrength * Mathf.Clamp01(strength01), vibrato: 12,
                randomness: 70f, fadeOut: true).SetUpdate(true);
#else
            float strength = maxShakeStrength * Mathf.Clamp01(strength01);
            if (_shakeLeft > 0f && strength < _shakeStrength * (_shakeLeft / _shakeDuration)) return; // keep the bigger shake
            _shakeStrength = strength;
            _shakeDuration = _shakeLeft = Mathf.Max(0.05f, duration);
#endif
        }

        public void Haptic()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (hapticsEnabled && Core.GameSettings.Vibration) Handheld.Vibrate();
#endif
        }

        /// <summary>Jelly button punch (art-bible §4). Uses unscaled time so it plays during pause/hitstop.</summary>
        public static void PunchUI(Transform target)
        {
#if DOTWEEN
            target.DOKill(complete: true);
            target.DOPunchScale(Vector3.one * 0.08f, 0.25f, vibrato: 6, elasticity: 0.6f).SetUpdate(true);
#else
            if (target != null) AddTween(target, false, 0f, 0.2f);
#endif
        }

        /// <summary>Card / panel entrance: drop in with OutBack.</summary>
        public static void PopIn(Transform target, float delay = 0f)
        {
#if DOTWEEN
            target.DOKill();
            target.localScale = Vector3.zero;
            target.DOScale(1f, 0.35f).SetDelay(delay).SetEase(Ease.OutBack).SetUpdate(true);
#else
            if (target != null) AddTween(target, true, delay, 0.32f);
#endif
        }
    }
}
