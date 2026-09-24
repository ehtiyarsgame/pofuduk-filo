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
        public WeaponDefinition startingWeapon;
        public StatModifier[] modifiers = System.Array.Empty<StatModifier>();
        public CharacterPerk perk;

        [Header("Unlock")]
        public int goldCost;
        public int stardustCost;
        [Tooltip("Unlocked for free once this chapter index is cleared; -1 = not chapter-gated.")]
        public int requiresChapterCleared = -1;

        public bool IsFree => goldCost == 0 && stardustCost == 0 && requiresChapterCleared < 0;
    }
}
