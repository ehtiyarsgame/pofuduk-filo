using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace PofudukFilo.Core
{
    public enum Language
    {
        Turkish,
        English
    }

    /// <summary>
    /// Localization with Turkish as the source language (every string in code and generated data
    /// is Turkish). <see cref="T"/> maps a finished display string to the current language:
    /// exact entries first, then templates such as <c>"Seviye {0}!"</c> whose captures are
    /// translated recursively — so <c>"EVRİM! Tüy Blaster"</c> becomes
    /// <c>"EVOLVED! Feather Blaster"</c> without touching the call sites that build it.
    /// Untranslated text falls through unchanged (a visible gap, never a crash).
    /// </summary>
    public static class Loc
    {
        private const string PrefKey = "pf.lang";

        private static Language? s_current;
        private static readonly Dictionary<string, string> s_cache = new();
        private static List<(Regex pattern, string format)> s_templates;

        public static event Action Changed;

        public static Language Current
        {
            get
            {
                if (s_current == null)
                {
                    int saved = PlayerPrefs.GetInt(PrefKey, -1);
                    s_current = saved >= 0
                        ? (Language)saved
                        : Application.systemLanguage == SystemLanguage.Turkish ? Language.Turkish : Language.English;
                }
                return s_current.Value;
            }
            set
            {
                if (s_current == value) return;
                s_current = value;
                s_cache.Clear();
                PlayerPrefs.SetInt(PrefKey, (int)value);
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        /// <summary>Game title — "Galaxy Paws" in every language (renamed 2026-09-24 from Pofuduk Filo / Fluffy Fleet).</summary>
        public static string GameTitle => "Galaxy Paws";

        public static string T(string text)
        {
            if (string.IsNullOrEmpty(text) || Current == Language.Turkish) return text;
            if (s_cache.TryGetValue(text, out string hit)) return hit;
            string result = Translate(text, 0);
            if (s_cache.Count > 4000) s_cache.Clear();
            s_cache[text] = result;
            return result;
        }

        /// <summary>Formats a Turkish template and translates the result.</summary>
        public static string F(string turkishFormat, params object[] args) => T(string.Format(turkishFormat, args));

        /// <summary>Pure lookup, for tests: translate <paramref name="text"/> into English.</summary>
        public static string ToEnglish(string text) => Translate(text, 0);

        private static string Translate(string text, int depth)
        {
            if (LocTableEn.Exact.TryGetValue(text, out string exact)) return exact;
            if (depth > 4 || text.Length == 0) return text;

            s_templates ??= BuildTemplates();
            foreach ((Regex pattern, string format) in s_templates)
            {
                Match m = pattern.Match(text);
                if (!m.Success) continue;
                var args = new object[m.Groups.Count - 1];
                for (int g = 1; g < m.Groups.Count; g++) args[g - 1] = Translate(m.Groups[g].Value, depth + 1);
                return string.Format(format, args);
            }

            // Multi-line strings no template claimed: translate each line on its own.
            if (text.IndexOf('\n') >= 0)
            {
                string[] lines = text.Split('\n');
                var sb = new StringBuilder();
                for (int i = 0; i < lines.Length; i++)
                {
                    if (i > 0) sb.Append('\n');
                    sb.Append(Translate(lines[i], depth + 1));
                }
                return sb.ToString();
            }
            return text;
        }

        private static List<(Regex, string)> BuildTemplates()
        {
            var list = new List<(Regex, string)>();
            var keys = new List<string>(LocTableEn.Templates.Keys);
            // Most specific first: more literal characters wins.
            keys.Sort((a, b) => LiteralLength(b).CompareTo(LiteralLength(a)));
            foreach (string key in keys)
            {
                string regex = "^" + Regex.Replace(Regex.Escape(key), @"\\\{(\d+)}", "(.+?)") + "$";
                list.Add((new Regex(regex, RegexOptions.Singleline | RegexOptions.CultureInvariant), LocTableEn.Templates[key]));
            }
            return list;
        }

        private static int LiteralLength(string template) => Regex.Replace(template, @"\{\d+\}", "").Length;
    }
}
