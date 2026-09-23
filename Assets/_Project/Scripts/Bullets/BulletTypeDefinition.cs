using UnityEngine;

namespace PofudukFilo.Bullets
{
    /// <summary>
    /// Visual + collision description of one bullet type. Each type is drawn with one
    /// instanced draw call per 1023 bullets, so the material must have GPU instancing enabled.
    /// </summary>
    [CreateAssetMenu(menuName = "Pofuduk Filo/Bullet Type", fileName = "BulletType")]
    public sealed class BulletTypeDefinition : ScriptableObject
    {
        [Tooltip("Quad mesh (1x1 unit, centred).")]
        public Mesh mesh;

        [Tooltip("URP Unlit (or custom) material with 'Enable GPU Instancing' ticked.")]
        public Material material;

        [Tooltip("Visual size in world units.")]
        public float visualScale = 0.35f;

        [Tooltip("Collision radius in world units — keep ~30 % smaller than the visual (art-bible §3).")]
        public float hitRadius = 0.12f;

        [Tooltip("Rotate the sprite to face its velocity.")]
        public bool alignToVelocity = true;
    }
}
