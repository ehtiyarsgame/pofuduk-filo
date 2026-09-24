using System;
using System.Collections;
using UnityEngine;

namespace PofudukFilo.Core
{
    /// <summary>Unscaled-time delayed calls for ad reloads (the ad adapter is not a MonoBehaviour).</summary>
    public sealed class AdScheduler : MonoBehaviour
    {
        private static AdScheduler s_runner;

        public static void Run(float seconds, Action action)
        {
            if (s_runner == null)
            {
                var go = new GameObject("AdScheduler");
                DontDestroyOnLoad(go);
                s_runner = go.AddComponent<AdScheduler>();
            }
            s_runner.StartCoroutine(s_runner.After(seconds, action));
        }

        private IEnumerator After(float seconds, Action action)
        {
            yield return new WaitForSecondsRealtime(seconds);
            action?.Invoke();
        }
    }
}
