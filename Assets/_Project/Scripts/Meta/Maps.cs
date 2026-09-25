using System;

namespace PofudukFilo.Meta
{
    /// <summary>One map: the same endless run, dialled harder and paying more (maps.md §3).</summary>
    public readonly struct MapDef
    {
        public readonly string Id;
        public readonly string Name;
        /// <summary>Enemy HP × this.</summary>
        public readonly float HpMultiplier;
        /// <summary>Swarm spawn budget × this.</summary>
        public readonly float SpawnMultiplier;
        /// <summary>Enemy bullet, contact and laser damage × this.</summary>
        public readonly float DamageMultiplier;
        /// <summary>Minutes added to the threat clock: the map opens as hard as minute N of map 1.</summary>
        public readonly float ThreatOffsetMinutes;
        /// <summary>Coin value × this.</summary>
        public readonly float GoldMultiplier;
        /// <summary>Background tint (0xRRGGBB) so each map looks like a different place.</summary>
        public readonly int Tint;

        public MapDef(string id, string name, float hp, float spawn, float damage, float threatOffset, float gold, int tint)
        {
            Id = id;
            Name = name;
            HpMultiplier = hp;
            SpawnMultiplier = spawn;
            DamageMultiplier = damage;
            ThreatOffsetMinutes = threatOffset;
            GoldMultiplier = gold;
            Tint = tint;
        }
    }

    /// <summary>
    /// Maps (maps.md, owner 2026-09-25): "eğer düşman basit görürse 2 tane daha haritamız olsun, çok çok daha zor olsun,
    /// ama parası da çok olacak". Map N+1 opens once the player survives <see cref="UnlockSeconds"/> on map N
    /// ("15 dakika"). The runtime reads <see cref="Current"/>, set by RunController at the start of each run. Pure rules.
    /// </summary>
    public static class Maps
    {
        /// <summary>Survive this long on a map to open the next one (15:00; owner changed it from 25:00).</summary>
        public const float UnlockSeconds = 900f;

        public static readonly MapDef[] All =
        {
            new("candy", "Şekerkamışı", 1f, 1f, 1f, 0f, 1f, 0xFFFFFF),
            new("jelly", "Jöle Nebulası", 1.8f, 1.3f, 1.4f, 3f, 2.5f, 0xB8FFD0),
            new("cookie", "Kurabiye Kuşağı", 3f, 1.6f, 1.8f, 6f, 5f, 0xFFD2A0)
        };

        public static int Count => All.Length;

        /// <summary>The map of the run in progress (map 1 outside a run).</summary>
        public static MapDef Current { get; private set; } = All[0];

        public static int CurrentIndex { get; private set; }

        public static void Select(int index)
        {
            CurrentIndex = Math.Clamp(index, 0, All.Length - 1);
            Current = All[CurrentIndex];
        }

        /// <summary>Map 1 is always open; map N+1 needs <see cref="UnlockSeconds"/> survived on map N.</summary>
        public static bool IsUnlocked(int index, Func<int, float> bestSecondsOf) =>
            index <= 0 || (index < All.Length && bestSecondsOf(index - 1) >= UnlockSeconds);

        /// <summary>Threat-clock minutes on the current map: run minutes plus the map's offset.</summary>
        public static float ThreatMinutes(float runMinutes) => runMinutes + Current.ThreatOffsetMinutes;
    }
}
