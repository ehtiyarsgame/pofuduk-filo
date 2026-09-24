using System.Collections.Generic;
using PofudukFilo.Core;

namespace PofudukFilo.Weapons
{
    /// <summary>
    /// Run-time damage multipliers from permanent weapon mastery (meta-economy.md §3.6). An
    /// evolution inherits its base weapon's mastery, so investing in a weapon keeps paying off
    /// after it evolves. Configured once per run by RunController.
    /// </summary>
    public static class WeaponMastery
    {
        private static readonly Dictionary<string, float> s_multipliers = new();

        public static void Configure(IEnumerable<WeaponDefinition> baseWeapons, System.Func<string, int> levelOf)
        {
            s_multipliers.Clear();
            foreach (WeaponDefinition w in baseWeapons)
            {
                if (w == null) continue;
                float m = Formulas.MasteryMultiplier(levelOf(w.id));
                s_multipliers[w.id] = m;
                for (WeaponDefinition e = w.evolvesInto; e != null && !s_multipliers.ContainsKey(e.id); e = e.evolvesInto)
                    s_multipliers[e.id] = m;
            }
        }

        public static float Multiplier(WeaponDefinition w) =>
            w != null && s_multipliers.TryGetValue(w.id, out float m) ? m : 1f;
    }
}
