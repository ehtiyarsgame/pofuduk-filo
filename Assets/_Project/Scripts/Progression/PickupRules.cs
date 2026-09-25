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
        Bomb,
        // Richer coins (economy.md §3.2): appended so saved kinds keep their numbers.
        GoldBig,   // a stack of coins
        GoldBar    // a gold bar
    }

    /// <summary>Pure pickup rules, unit-tested without the engine.</summary>
    public static class PickupRules
    {
        /// <summary>Gem colour by value: blue 1–4, green 5–24, pink 25+ (game-concept.md §3.4).</summary>
        public static PickupKind XpKindFor(int value) =>
            value >= 25 ? PickupKind.XpLarge : value >= 5 ? PickupKind.XpMedium : PickupKind.XpSmall;

        public static bool IsXp(PickupKind kind) => kind <= PickupKind.XpLarge;

        public static bool IsGold(PickupKind kind) => kind is PickupKind.Gold or PickupKind.GoldBig or PickupKind.GoldBar;

        /// <summary>Coin look by value: small coin under 15, coin stack 15–59, gold bar 60+ (economy.md §3.2).</summary>
        public static PickupKind GoldKindFor(int value) =>
            value >= 60 ? PickupKind.GoldBar : value >= 15 ? PickupKind.GoldBig : PickupKind.Gold;
    }
}
