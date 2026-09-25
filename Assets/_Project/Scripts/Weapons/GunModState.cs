using System.Collections.Generic;
using PofudukFilo.Meta;

namespace PofudukFilo.Weapons
{
    /// <summary>
    /// Run-time bonuses from the hero gun mods (hero-guns.md §4), configured once per run by RunController like
    /// <see cref="WeaponMastery"/>. An evolution keeps its base gun's mods.
    /// </summary>
    public static class GunModState
    {
        private static readonly Dictionary<string, GunMods.Bonus> s_bonuses = new();

        public static void Configure(IEnumerable<WeaponDefinition> heroGuns, System.Func<string, int> levelOf)
        {
            s_bonuses.Clear();
            foreach (WeaponDefinition g in heroGuns)
            {
                if (g == null || !GunMods.HasMods(g.id) || s_bonuses.ContainsKey(g.id)) continue;
                GunMods.Bonus b = GunMods.Evaluate(g.id, levelOf);
                if (b.IsZero) continue;
                for (WeaponDefinition e = g; e != null && !s_bonuses.ContainsKey(e.id); e = e.evolvesInto)
                    s_bonuses[e.id] = b;
            }
        }

        public static GunMods.Bonus For(WeaponDefinition w) =>
            w != null && s_bonuses.TryGetValue(w.id, out GunMods.Bonus b) ? b : default;
    }
}
