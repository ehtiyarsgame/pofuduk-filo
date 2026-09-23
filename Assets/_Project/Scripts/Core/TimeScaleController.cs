using UnityEngine;

namespace PofudukFilo.Core
{
    /// <summary>
    /// Single owner of Time.timeScale. Pause, hitstop and the "finger lifted" slow-down are
    /// independent layers multiplied together, so they never overwrite each other.
    /// </summary>
    public static class TimeScaleController
    {
        private static float s_pause = 1f;
        private static float s_hitstop = 1f;
        private static float s_fingerLifted = 1f;
        private static float s_hitstopUntil;

        public static bool IsPaused => s_pause == 0f;

        // Static state survives play sessions when domain reload is disabled (Enter Play Mode Options).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_pause = s_hitstop = s_fingerLifted = 1f;
            s_hitstopUntil = 0f;
        }

        public static void SetPaused(bool paused)
        {
            s_pause = paused ? 0f : 1f;
            Apply();
        }

        /// <summary>game-concept.md §3.1: lifting the finger slows time to 30 %.</summary>
        public static void SetFingerLifted(bool lifted, float slowScale = 0.3f)
        {
            s_fingerLifted = lifted ? slowScale : 1f;
            Apply();
        }

        /// <summary>Freeze-frame on elite/boss kills (art-bible §5.1: 40–60 ms at 0.05 scale).</summary>
        public static void Hitstop(float realSeconds, float scale = 0.05f)
        {
            s_hitstop = scale;
            s_hitstopUntil = Mathf.Max(s_hitstopUntil, Time.unscaledTime + realSeconds);
            Apply();
        }

        /// <summary>Call once per frame (e.g. from the run director) to release an expired hitstop.</summary>
        public static void Tick()
        {
            if (s_hitstop < 1f && Time.unscaledTime >= s_hitstopUntil)
            {
                s_hitstop = 1f;
                Apply();
            }
        }

        private static void Apply() => Time.timeScale = s_pause * s_hitstop * s_fingerLifted;
    }
}
