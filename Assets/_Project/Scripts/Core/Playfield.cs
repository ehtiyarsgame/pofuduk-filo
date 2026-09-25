using UnityEngine;

namespace PofudukFilo.Core
{
    /// <summary>
    /// The visible playfield (design/ux/hud.md §2): the top HUD panel is opaque and enemies emerge from behind it,
    /// so an enemy is hittable exactly when it can be seen. <see cref="TopInset"/> is the fraction of the camera's
    /// height the panel covers; GameUI measures it from the laid-out panel every frame, so notches, tablets and
    /// canvas scaling all agree with the world line.
    /// </summary>
    public static class Playfield
    {
        /// <summary>Fraction of the camera view (from the top) hidden by the HUD panel.</summary>
        public static float TopInset { get; set; } = 0.12f;

        /// <summary>World Y of the panel's bottom edge — the top of the fight.</summary>
        public static float TopY(Camera cam)
        {
            float c = cam.transform.position.y, h = cam.orthographicSize;
            return c + h - 2f * h * TopInset;
        }
    }
}
