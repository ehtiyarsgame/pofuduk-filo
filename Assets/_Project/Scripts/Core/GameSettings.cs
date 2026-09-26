using System;
using UnityEngine;

namespace PofudukFilo.Core
{
    /// <summary>
    /// Per-device preferences (art-bible §4 accessibility: shake and flashes can be turned off).
    /// PlayerPrefs is fine here — these are conveniences, not progress.
    /// </summary>
    public static class GameSettings
    {
        public static event Action Changed;

        public static bool Sfx
        {
            get => Get("sfx");
            set => Set("sfx", value);
        }

        public static bool Music
        {
            get => Get("music");
            set => Set("music", value);
        }

        public static bool Vibration
        {
            get => Get("vibration");
            set => Set("vibration", value);
        }

        public static bool ScreenShake
        {
            get => Get("shake");
            set => Set("shake", value);
        }

        private static int s_damageNumbers = -1; // cached: read on every hit

        public static bool DamageNumbers
        {
            get
            {
                if (s_damageNumbers < 0) s_damageNumbers = Get("dmgnum") ? 1 : 0;
                return s_damageNumbers == 1;
            }
            set
            {
                s_damageNumbers = value ? 1 : 0;
                Set("dmgnum", value);
            }
        }

        /// <summary>First-run coach marks already shown (tutorial.md). Off by default, unlike the toggles above.</summary>
        public static bool TutorialDone
        {
            get => PlayerPrefs.GetInt("pf.tutorial", 0) == 1;
            set => Set("tutorial", value);
        }

        private static bool Get(string key) => PlayerPrefs.GetInt("pf." + key, 1) == 1;

        private static void Set(string key, bool value)
        {
            PlayerPrefs.SetInt("pf." + key, value ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
