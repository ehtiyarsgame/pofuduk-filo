namespace PofudukFilo.Bullets
{
    /// <summary>
    /// On-hit traits a main gun gains as it levels (hero-guns.md §3.2) — each level adds a new behaviour rather than
    /// only more bullets (device feedback 2026-09-25: "leveller arttıkça farklı özellik olsun").
    /// </summary>
    [System.Flags]
    public enum BulletEffect : byte
    {
        None = 0,
        /// <summary>Bursts on hit: 50 % damage to other enemies within 1 unit.</summary>
        Explode = 1,
        /// <summary>Splits into two shards flying off at ±40° (45 % damage).</summary>
        Split = 2,
        /// <summary>A spark jumps to the nearest other enemy within 3.5 units (60 % damage).</summary>
        Chain = 4,
        /// <summary>Slows the enemy hit to 55 % speed for 1.5 s.</summary>
        Slow = 8,
        /// <summary>Shoves the enemy back up the screen (Kuzu's wool shot, hero-guns.md §3.4). Bosses stand firm.</summary>
        Knockback = 16
    }
}
