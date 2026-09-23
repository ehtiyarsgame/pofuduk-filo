using UnityEngine;

namespace PofudukFilo.Core
{
    /// <summary>
    /// Batches quads per visual type into Graphics.RenderMeshInstanced calls of up to 1023
    /// instances. Shared by BulletSystem and PickupSystem (architecture.md §4.4).
    /// </summary>
    public sealed class InstancedDrawer
    {
        private const int InstancesPerDraw = 1023;

        private readonly Mesh[] _meshes;
        private readonly RenderParams[] _params;
        private readonly Matrix4x4[][] _matrices;
        private readonly int[] _counts;

        public InstancedDrawer(Mesh[] meshes, Material[] materials, int layer)
        {
            _meshes = meshes;
            _params = new RenderParams[meshes.Length];
            _matrices = new Matrix4x4[meshes.Length][];
            _counts = new int[meshes.Length];
            for (int i = 0; i < meshes.Length; i++)
            {
                _params[i] = new RenderParams(materials[i]) { layer = layer };
                _matrices[i] = new Matrix4x4[InstancesPerDraw];
            }
        }

        /// <param name="z">Depth: with 2D orthographic transparency sorting, smaller z draws on top.</param>
        public void Add(int type, Vector2 position, float angleDegrees, float scale, float z = 0f)
        {
            _matrices[type][_counts[type]++] = Matrix4x4.TRS(
                new Vector3(position.x, position.y, z),
                Quaternion.Euler(0f, 0f, angleDegrees),
                new Vector3(scale, scale, 1f));

            if (_counts[type] == InstancesPerDraw) Flush(type);
        }

        /// <summary>Draws everything still buffered. Call once after the last Add of the frame.</summary>
        public void FlushAll()
        {
            for (int t = 0; t < _counts.Length; t++)
                if (_counts[t] > 0) Flush(t);
        }

        private void Flush(int type)
        {
            if (_meshes[type] != null && _params[type].material != null)
                Graphics.RenderMeshInstanced(_params[type], _meshes[type], 0, _matrices[type], _counts[type]);
            _counts[type] = 0;
        }
    }
}
