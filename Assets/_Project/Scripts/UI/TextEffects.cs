using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PofudukFilo.UI
{
    /// <summary>
    /// Vertical colour gradient for any UI graphic (logo letters, big button labels). Add it BEFORE
    /// Outline/Shadow so only the face is tinted, not the outline copies those effects append.
    /// </summary>
    public sealed class UIGradient : BaseMeshEffect
    {
        public Color top = Color.white;
        public Color bottom = new(0.8f, 0.8f, 0.8f, 1f);

        private static readonly List<UIVertex> s_verts = new();

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0) return;
            s_verts.Clear();
            vh.GetUIVertexStream(s_verts);
            float min = float.MaxValue, max = float.MinValue;
            foreach (UIVertex v in s_verts)
            {
                if (v.position.y < min) min = v.position.y;
                if (v.position.y > max) max = v.position.y;
            }
            float span = Mathf.Max(0.001f, max - min);
            for (int i = 0; i < s_verts.Count; i++)
            {
                UIVertex v = s_verts[i];
                Color c = Color.Lerp(bottom, top, (v.position.y - min) / span);
                v.color = (Color32)(c * (Color)v.color);
                s_verts[i] = v;
            }
            vh.Clear();
            vh.AddUIVertexTriangleStream(s_verts);
        }
    }

    /// <summary>
    /// Letters bob in a gentle wave (logo). Works on legacy Text: every glyph is one quad of six stream
    /// vertices. Add it before Outline so the outline copies follow the moved letters.
    /// </summary>
    public sealed class UIWave : BaseMeshEffect
    {
        public float amplitude = 8f;
        public float speed = 3f;
        public float phasePerLetter = 0.55f;

        private static readonly List<UIVertex> s_verts = new();

        private void Update()
        {
            if (graphic != null) graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0) return;
            s_verts.Clear();
            vh.GetUIVertexStream(s_verts);
            float t = Time.unscaledTime * speed;
            for (int i = 0; i < s_verts.Count; i++)
            {
                int letter = i / 6;
                UIVertex v = s_verts[i];
                v.position.y += Mathf.Sin(t + letter * phasePerLetter) * amplitude;
                s_verts[i] = v;
            }
            vh.Clear();
            vh.AddUIVertexTriangleStream(s_verts);
        }
    }
}
