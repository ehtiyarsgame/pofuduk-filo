using UnityEngine;

namespace PofudukFilo.Bullets
{
    /// <summary>A set of circles that eat absorbable enemy bullets (see BulletSystem.RegisterAbsorber).</summary>
    public interface IBulletAbsorber
    {
        int AbsorberCount { get; }
        Vector2 GetAbsorberCenter(int index);
        float GetAbsorberRadius(int index);
        /// <summary>False while this circle is recharging.</summary>
        bool CanAbsorb(int index);
        void OnAbsorbed(int index, Vector2 bulletPosition);
    }
}
