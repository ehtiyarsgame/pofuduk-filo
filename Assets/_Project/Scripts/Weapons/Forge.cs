using PofudukFilo.Core;

namespace PofudukFilo.Weapons
{
    /// <summary>The two Forge tracks bought on the main menu (power-match.md §3.4).</summary>
    public enum ForgeTrack
    {
        Power,
        Speed
    }

    /// <summary>
    /// Ocak (the Forge) — Ball Blast's always-something-to-buy loop: Ateş Gücü (damage) and Ateş Hızı
    /// (fire rate), levelled with gold between runs, Ateş Gücü without a cap. Applies to every weapon
    /// and wingman. Configured once per run by RunController.
    /// </summary>
    public static class Forge
    {
        public static float DamageMultiplier { get; private set; } = 1f;
        public static float FireRate { get; private set; } = 1f;

        public static void Configure(int powerLevel, int speedLevel)
        {
            DamageMultiplier = Formulas.ForgePowerMultiplier(powerLevel);
            FireRate = Formulas.ForgeSpeedMultiplier(speedLevel);
        }
    }
}
