using System;
using System.Collections.Generic;

namespace PofudukFilo.Meta
{
    /// <summary>What a gun mod improves. Count means the gun's own count: lanes, balls, chain jumps or beams.</summary>
    public enum GunModKind
    {
        Damage,
        FireRate,
        Count,
        Area,
        Pierce,
        Duration,
        /// <summary>Extra bounces (Balonbaş).</summary>
        Bounces,
        /// <summary>Homing turn rate × (1 + v) (Yıldızpati).</summary>
        Homing
    }

    /// <summary>One permanent upgrade track of one hero gun (hero-guns.md §4).</summary>
    public readonly struct GunMod
    {
        public readonly string Key;
        public readonly string Name;
        public readonly string Description;
        public readonly GunModKind Kind;
        public readonly float PerLevel;

        public GunMod(string key, string name, string description, GunModKind kind, float perLevel)
        {
            Key = key;
            Name = name;
            Description = description;
            Kind = kind;
            PerLevel = perLevel;
        }
    }

    /// <summary>
    /// Hero gun mods (hero-guns.md §4, owner 2026-09-25: "her silahın kendine özgü geliştirmesi olsun"): every hero
    /// gun has three permanent tracks of its own, bought in the Weapons screen. Each track grows what makes that gun
    /// that gun — lanes for Pıtır, blast size for Cıvık, chain jumps for Mırnav, freeze for Pengu. Pure rules.
    /// </summary>
    public static class GunMods
    {
        public const int MaxLevel = 5;

        /// <summary>Gold for the next level of one track: 250·1.6^L, rounded to 10 (250, 400, 640, 1 020, 1 640).</summary>
        public static int Cost(int level)
        {
            if (level < 0 || level >= MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            return (int)(Math.Round(250 * Math.Pow(1.6, level) / 10.0, MidpointRounding.AwayFromZero) * 10);
        }

        private static readonly Dictionary<string, GunMod[]> Table = new()
        {
            ["feather_blaster"] = new[]
            {
                new GunMod("akis", "Ek Akış", "Sv3 ve Sv5'te +1 tüy akışı", GunModKind.Count, 0.4f),
                new GunMod("hiz", "Hızlı Kanat", "Atış hızı +%4 / sv", GunModKind.FireRate, 0.04f),
                new GunMod("hasar", "Keskin Tüy", "Hasar +%5 / sv", GunModKind.Damage, 0.05f)
            },
            ["chick_cannon"] = new[]
            {
                new GunMod("alan", "Büyük Patlama", "Patlama alanı +%10 / sv", GunModKind.Area, 0.10f),
                new GunMod("top", "Ek Top", "Sv3 ve Sv5'te +1 top", GunModKind.Count, 0.4f),
                new GunMod("hasar", "Ağır Top", "Hasar +%5 / sv", GunModKind.Damage, 0.05f)
            },
            ["spark_pistol"] = new[]
            {
                new GunMod("zincir", "Uzun Zincir", "Sv2 ve Sv4'te +1 zıplama", GunModKind.Count, 0.5f),
                new GunMod("menzil", "Geniş Menzil", "Menzil +%8 / sv", GunModKind.Area, 0.08f),
                new GunMod("sersem", "Şok", "Sersemletme süresi +%15 / sv", GunModKind.Duration, 0.15f)
            },
            ["ice_gun"] = new[]
            {
                new GunMod("kalinlik", "Kalın Işın", "Işın kalınlığı +%8 / sv", GunModKind.Area, 0.08f),
                new GunMod("donma", "Derin Donma", "Dondurma süresi +%12 / sv", GunModKind.Duration, 0.12f),
                new GunMod("soguk", "Keskin Soğuk", "Hasar +%5 / sv", GunModKind.Damage, 0.05f)
            },
            ["bubble_rifle"] = new[]
            {
                new GunMod("sekme", "Lastik Sakız", "Sv2 ve Sv4'te +1 sekme", GunModKind.Bounces, 0.5f),
                new GunMod("adet", "Ek Balon", "Sv3 ve Sv5'te +1 balon", GunModKind.Count, 0.4f),
                new GunMod("hasar", "Sert Balon", "Hasar +%5 / sv", GunModKind.Damage, 0.05f)
            },
            ["star_bow"] = new[]
            {
                new GunMod("gudum", "Yıldız Pusulası", "Hedefe dönüş +%15 / sv", GunModKind.Homing, 0.15f),
                new GunMod("adet", "Ek Yıldız", "Sv3 ve Sv5'te +1 yıldız", GunModKind.Count, 0.4f),
                new GunMod("hasar", "Parlak Yıldız", "Hasar +%5 / sv", GunModKind.Damage, 0.05f)
            },
            ["yarn_launcher"] = new[]
            {
                new GunMod("sacma", "Ek Saçma", "Her sv +1 saçma", GunModKind.Count, 1f),
                new GunMod("menzil", "Uzun Namlu", "Menzil +%10 / sv", GunModKind.Duration, 0.10f),
                new GunMod("hasar", "Sıkı Yün", "Hasar +%5 / sv", GunModKind.Damage, 0.05f)
            }
        };

        public static IReadOnlyList<GunMod> For(string gunId) =>
            gunId != null && Table.TryGetValue(gunId, out GunMod[] mods) ? mods : Array.Empty<GunMod>();

        public static bool HasMods(string gunId) => For(gunId).Count > 0;

        /// <summary>Save key of one track: "gun:mod".</summary>
        public static string SaveKey(string gunId, string modKey) => gunId + ":" + modKey;

        /// <summary>The summed effect of a gun's three tracks at the given levels.</summary>
        public struct Bonus
        {
            public float Damage;
            public float FireRate;
            public float Area;
            public float Duration;
            public int Count;
            public int Pierce;
            public int Bounces;
            public float Homing;

            public bool IsZero => Damage == 0f && FireRate == 0f && Area == 0f && Duration == 0f && Count == 0 && Pierce == 0
                && Bounces == 0 && Homing == 0f;
        }

        public static Bonus Evaluate(string gunId, Func<string, int> levelOf)
        {
            var b = new Bonus();
            foreach (GunMod m in For(gunId))
            {
                int level = Math.Clamp(levelOf(SaveKey(gunId, m.Key)), 0, MaxLevel);
                float v = m.PerLevel * level;
                switch (m.Kind)
                {
                    case GunModKind.Damage: b.Damage += v; break;
                    case GunModKind.FireRate: b.FireRate += v; break;
                    case GunModKind.Area: b.Area += v; break;
                    case GunModKind.Duration: b.Duration += v; break;
                    case GunModKind.Count: b.Count += (int)Math.Floor(v + 1e-4); break;
                    case GunModKind.Pierce: b.Pierce += (int)Math.Floor(v + 1e-4); break;
                    case GunModKind.Bounces: b.Bounces += (int)Math.Floor(v + 1e-4); break;
                    case GunModKind.Homing: b.Homing += v; break;
                }
            }
            return b;
        }
    }
}
