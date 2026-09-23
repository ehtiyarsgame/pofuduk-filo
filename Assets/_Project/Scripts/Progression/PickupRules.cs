namespace PofudukFilo.Progression
{
    public enum PickupKind : byte
    {
        XpSmall,   // blue, 1
        XpMedium,  // green, 5
        XpLarge,   // pink, 25
        Gold,
        Magnet,
        Heart,
        Bomb
    }

    /// <summary>Pure pickup rules, unit-tested without the engine.</summary>
    public static class PickupRules
    {
        /// <summary>Gem colour by value: blue 1–4, green 5–24, pink 25+ (game-concept.md §3.4).</summary>
        public static PickupKind XpKindFor(int value) =>
            value >= 25 ? PickupKind.XpLarge : value >= 5 ? PickupKind.XpMedium : PickupKind.XpSmall;

        public static bool IsXp(PickupKind kind) => kind <= PickupKind.XpLarge;
    }
}
