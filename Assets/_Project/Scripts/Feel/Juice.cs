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
        [SerializeField] private float maxShakeStrength = 0.15f;
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

        private void Update() => TimeScaleController.Tick();

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
            target.localScale = Vector3.one;
#endif
        }
    }
}
