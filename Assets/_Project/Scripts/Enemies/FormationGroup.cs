using System;
using System.Collections.Generic;
using UnityEngine;

namespace PofudukFilo.Enemies
{
    public enum FormationShape
    {
        Grid,
        V,
        Arc,
        Line
    }

    /// <summary>
    /// Tracks one Chicken Invaders–style formation. Raises <see cref="Cleared"/> only when every
    /// member was killed — a member diving off screen forfeits the bonus (game-concept.md §3.3).
    /// </summary>
    public sealed class FormationGroup
    {
        private int _remaining;
        private bool _lostAny;

        /// <summary>Every member was killed — award the "Formation Cleared!" bonus.</summary>
        public event Action<FormationGroup> Cleared;

        /// <summary>No members left, cleared or not. Lets the director free the formation slot.</summary>
        public event Action<FormationGroup> Finished;

        public Vector2 Center { get; }
        public int Remaining => _remaining;

        public FormationGroup(Vector2 center)
        {
            Center = center;
        }

        public void AddMember() => _remaining++;

        public void OnMemberKilled()
        {
            _remaining--;
            if (_remaining > 0) return;
            if (!_lostAny) Cleared?.Invoke(this);
            Finished?.Invoke(this);
        }

        public void OnMemberLost()
        {
            _remaining--;
            _lostAny = true;
            if (_remaining == 0) Finished?.Invoke(this);
        }
    }

    /// <summary>Pure slot-offset math for formations, relative to the formation centre.</summary>
    public static class FormationLayout
    {
        public static void Build(FormationShape shape, int rows, int columns, float spacing, List<Vector2> offsets)
        {
            offsets.Clear();
            rows = Mathf.Max(1, rows);
            columns = Mathf.Max(1, columns);
            float halfWidth = (columns - 1) * spacing * 0.5f;

            switch (shape)
            {
                case FormationShape.Grid:
                    for (int r = 0; r < rows; r++)
                    for (int c = 0; c < columns; c++)
                        offsets.Add(new Vector2(c * spacing - halfWidth, -r * spacing));
                    break;

                case FormationShape.Line:
                    for (int c = 0; c < columns; c++)
                        offsets.Add(new Vector2(c * spacing - halfWidth, 0f));
                    break;

                case FormationShape.V:
                    // Point faces the player; the wings rise outward.
                    for (int c = 0; c < columns; c++)
                    {
                        float x = c * spacing - halfWidth;
                        offsets.Add(new Vector2(x, Mathf.Abs(x) * 0.6f));
                    }
                    break;

                case FormationShape.Arc:
                    // Semicircle opening upward, radius fits the column count.
                    float radius = Mathf.Max(spacing, halfWidth);
                    for (int c = 0; c < columns; c++)
                    {
                        float t = columns == 1 ? 0.5f : c / (columns - 1f);
                        float angle = Mathf.Lerp(Mathf.PI, 2f * Mathf.PI, t);
                        offsets.Add(new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * 0.5f));
                    }
                    break;
            }
        }
    }
}
