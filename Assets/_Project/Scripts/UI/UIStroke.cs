using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PofudukFilo.UI
{
    /// <summary>
    /// Round text stroke plus drop in one pass (design/ux/main-menu.md §6). Stacking Outline components compounds:
    /// every effect copies the copies of the ones before it, so three Outlines and a Shadow draw 250 copies, the
    /// offsets add up and the stroke fills the gaps between letters into a dark box (QA run 59). This effect copies
    /// the original glyphs once per direction: a drop first, then eight ring copies, then the face on top.
    /// </summary>
    public sealed class UIStroke : BaseMeshEffect
    {
        public Color color = new(0.114f, 0.078f, 0.251f, 1f);
        public float width = 5f;
        public float drop = 6f;

        private static readonly List<UIVertex> s_face = new();
        private static readonly List<UIVertex> s_out = new();

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0) return;
            s_face.Clear();
            s_out.Clear();
            vh.GetUIVertexStream(s_face);
            if (drop > 0f) AddCopy(new Vector2(0f, -drop));
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                AddCopy(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * width);
            }
            s_out.AddRange(s_face);
            vh.Clear();
            vh.AddUIVertexTriangleStream(s_out);
        }

        private void AddCopy(Vector2 offset)
        {
            for (int i = 0; i < s_face.Count; i++)
            {
                UIVertex v = s_face[i];
                v.position += (Vector3)offset;
                Color32 c = color;
                c.a = (byte)(c.a * v.color.a / 255);
                v.color = c;
                s_out.Add(v);
            }
        }
    }
}
