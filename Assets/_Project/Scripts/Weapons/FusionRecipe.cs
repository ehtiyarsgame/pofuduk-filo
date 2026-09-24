using UnityEngine;

namespace PofudukFilo.Weapons
{
    /// <summary>Two evolved weapons that fuse into one legendary (weapon-system.md §3.4).</summary>
    [CreateAssetMenu(menuName = "Pofuduk Filo/Fusion Recipe", fileName = "Fusion")]
    public sealed class FusionRecipe : ScriptableObject
    {
        public WeaponDefinition a;
        public WeaponDefinition b;
        [Tooltip("Its behaviour prefab should be a FusionWeapon listing a and b as parts.")]
        public WeaponDefinition result;
        [Tooltip("Run-long damage bonus granted on fusion.")]
        public float damageBonus = 0.25f;
    }
}
