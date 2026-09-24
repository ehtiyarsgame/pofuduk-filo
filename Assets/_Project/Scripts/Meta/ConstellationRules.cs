using System;
using System.Collections.Generic;

namespace PofudukFilo.Meta
{
    /// <summary>Pure board rules, unit-tested without the engine.</summary>
    public static class ConstellationRules
    {
        public const long RespecCooldownTicks = TimeSpan.TicksPerDay;

        public static bool PrerequisitesMet(IReadOnlyList<string> requires, ICollection<string> owned)
        {
            for (int i = 0; i < requires.Count; i++)
                if (!owned.Contains(requires[i])) return false;
            return true;
        }

        public static bool CanBuy(string id, int cost, IReadOnlyList<string> requires, ICollection<string> owned, int stardust) =>
            !owned.Contains(id) && stardust >= cost && PrerequisitesMet(requires, owned);

        /// <summary>Free full refund, once every 24 h (meta-economy.md §5).</summary>
        public static bool CanRespec(long lastRespecTicks, long nowTicks) =>
            lastRespecTicks <= 0 || nowTicks - lastRespecTicks >= RespecCooldownTicks;
    }
}
