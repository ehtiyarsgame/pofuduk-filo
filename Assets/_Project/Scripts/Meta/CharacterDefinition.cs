using PofudukFilo.Weapons;
using UnityEngine;

namespace PofudukFilo.Meta
{
    public enum CharacterPerk
    {
        None,
        /// <summary>Pıtır: +1 level-up card for every 10 levels.</summary>
        CardEvery10Levels,
        /// <summary>Secret character: starts each run with a random passive.</summary>
        RandomPassive
    }

    /// <summary>
    /// A Hangar character (meta-economy.md §3.3 B): its own starting weapon and perk, i.e. a new
    /// way to play — the strongest "one more run" hook.
    /// </summary>
    [CreateAssetMenu(menuName = "Pofuduk Filo/Character", fileName = "Character")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string perkText;
        public Sprite sprite;
        [Tooltip("In-run ship (the shared hull with this pilot in the cockpit). Falls back to the portrait.")]
        public Sprite shipSprite;
        [Tooltip("The hero's own main gun (ANA SİLAH, fires from the ship). Null = the Feather Blaster.")]
        public WeaponDefinition mainGun;
        [Tooltip("The hero's signature second weapon.")]
        public WeaponDefinition startingWeapon;
        public StatModifier[] modifiers = System.Array.Empty<StatModifier>();
        public CharacterPerk perk;

        [Header("Unlock")]
        public int goldCost;
        public int stardustCost;
        [Tooltip("Rewarded ads that also unlock it (ad-rewards.md); 0 = gold only.")]
        public int adsToUnlock;
        [Tooltip("Unlocked for free once this chapter index is cleared; -1 = not chapter-gated.")]
        public int requiresChapterCleared = -1;

        public bool IsFree => goldCost == 0 && stardustCost == 0 && requiresChapterCleared < 0;
    }
}
