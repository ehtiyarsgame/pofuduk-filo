using UnityEngine;

namespace PofudukFilo.EditorTools
{
    /// <summary>
    /// Enemy shots with an identity (design/gdd/enemy-attacks.md): each enemy type throws something that says who
    /// threw it, so a crowded screen stays readable ("düşmanların hep bir ateş türü var… arada kaynıyor"). All keep
    /// the enemy-shot language — warm glow halo, ink outline, bright core — so they never read as player shots.
    /// </summary>
    public static partial class ArtRecipes
    {
        private static readonly Color ShotInk = Hex(0x2A1236);

        /// <summary>Chick: a yellow egg dropped straight down.</summary>
        public static Painter ShotEgg(bool golden)
        {
            var p = new Painter(64, 64);
            Color shell = golden ? Hex(0xFFC21A) : Hex(0xFFF0A8);
            Color glow = golden ? Hex(0xFFB000) : Hex(0xFF9A3C);
            p.Glow(32, 32, 31, WithAlpha(glow, 0.8f), 1.5f);
            p.Fill((x, y) => Painter.EllipseSdf(x, y, 32, 31, 14, 18) - 3.5f, ShotInk);
            p.Fill((x, y) => Painter.EllipseSdf(x, y, 32, 31, 14, 18), (x, y) =>
                Color.Lerp(Painter.Deep(shell, 0.25f), shell, Mathf.Clamp01((y - 14f) / 30f)));
            p.Fill((x, y) => Painter.EllipseSdf(x, y, 27, 38, 4, 6), new Color(1f, 1f, 1f, 0.8f));
            if (golden) p.Star(32, 29, 6f, 2.5f, Color.white, 4);
            return p;
        }

        /// <summary>Candy bee: a thin orange stinger, drawn pointing up (aligned to its velocity).</summary>
        public static Painter ShotStinger()
        {
            var p = new Painter(64, 64);
            p.Fill((x, y) => Painter.EllipseSdf(x, y, 32, 30, 10, 28), (x, y) =>
                new Color(1f, 0.55f, 0.1f, 0.55f * Mathf.Clamp01(1f - Mathf.Abs(x - 32f) / 10f)), 1f);
            p.Triangle(new Vector2(32, 62), new Vector2(24, 8), new Vector2(40, 8), ShotInk, 1.5f);
            p.Triangle(new Vector2(32, 58), new Vector2(27, 11), new Vector2(37, 11), Hex(0xFF8A1F));
            p.Triangle(new Vector2(32, 56), new Vector2(31, 14), new Vector2(34, 14), Hex(0xFFF1B0));
            return p;
        }

        /// <summary>Cookie robot: a chunky cookie crumb with chocolate chips.</summary>
        public static Painter ShotCrumb()
        {
            var p = new Painter(64, 64);
            p.Glow(32, 32, 30, WithAlpha(Hex(0xFF7A1A), 0.7f), 1.6f);
            Painter.Sdf crumb = (x, y) => Mathf.Min(Painter.CircleSdf(x, y, 30, 33, 12), Painter.CircleSdf(x, y, 38, 28, 9));
            p.Fill((x, y) => crumb(x, y) - 3.5f, ShotInk);
            p.Fill((x, y) => crumb(x, y), (x, y) => Color.Lerp(Hex(0xB86B2E), Hex(0xE8A860), Mathf.Clamp01((y - 20f) / 24f)));
            p.Circle(27, 36, 3f, Hex(0x4A2412));
            p.Circle(37, 27, 2.6f, Hex(0x4A2412));
            p.Circle(33, 38, 2.2f, Hex(0x4A2412));
            return p;
        }

        /// <summary>Gum balloon death burst: a pink gum drop.</summary>
        public static Painter ShotGum()
        {
            var p = new Painter(64, 64);
            p.Glow(32, 32, 30, WithAlpha(Hex(0xFF4FB4), 0.8f), 1.5f);
            p.Circle(32, 32, 15f, ShotInk);
            p.Fill((x, y) => Painter.CircleSdf(x, y, 32, 32, 11.5f), (x, y) => Color.Lerp(Hex(0xE0368C), Hex(0xFF9ED2), Mathf.Clamp01((y - 20f) / 24f)));
            p.Circle(28, 37, 3.5f, new Color(1f, 1f, 1f, 0.85f));
            return p;
        }

        /// <summary>Jelly bear: a wobbly green jelly blob (big splits into three small ones).</summary>
        public static Painter ShotJelly(bool big)
        {
            var p = new Painter(64, 64);
            float r = big ? 17f : 12f;
            p.Glow(32, 32, 31, WithAlpha(Hex(0x5CFF7A), 0.7f), 1.5f);
            Painter.Sdf blob = (x, y) => Painter.CircleSdf(x, y, 32, 32, r + Mathf.Sin(Mathf.Atan2(y - 32, x - 32) * 5f) * 1.6f);
            p.Fill((x, y) => blob(x, y) - 3.5f, ShotInk);
            p.Fill((x, y) => blob(x, y), (x, y) => new Color(0.25f, 0.85f, 0.35f, 0.92f) * Mathf.Lerp(0.8f, 1.1f, Mathf.Clamp01((y - 16f) / 32f)));
            p.Fill((x, y) => Painter.EllipseSdf(x, y, 27, 38, r * 0.35f, r * 0.25f), new Color(1f, 1f, 1f, 0.7f));
            return p;
        }

        /// <summary>Ice-cream tower: a strawberry scoop with a cone-coloured rim.</summary>
        public static Painter ShotScoop()
        {
            var p = new Painter(64, 64);
            p.Glow(32, 32, 30, WithAlpha(Hex(0xFF6FA8), 0.75f), 1.5f);
            Painter.Sdf scoop = (x, y) => Painter.CircleSdf(x, y, 32, 32, 14f + Mathf.Sin(Mathf.Atan2(y - 32, x - 32) * 8f) * 1.2f);
            p.Fill((x, y) => scoop(x, y) - 3.5f, ShotInk);
            p.Fill((x, y) => scoop(x, y), (x, y) => Color.Lerp(Hex(0xFF7FB0), Hex(0xFFD0E4), Mathf.Clamp01((y - 18f) / 28f)));
            p.Fill((x, y) => Mathf.Max(scoop(x, y), y - 26f), Hex(0xF2B65A));
            p.Circle(27, 38, 3f, new Color(1f, 1f, 1f, 0.8f));
            return p;
        }

        /// <summary>Marshmallow: a soft white puff with a pink halo.</summary>
        public static Painter ShotPuff()
        {
            var p = new Painter(64, 64);
            p.Glow(32, 32, 31, WithAlpha(Hex(0xFF7AC8), 0.75f), 1.4f);
            Painter.Sdf puff = (x, y) => Mathf.Min(Mathf.Min(Painter.CircleSdf(x, y, 26, 32, 10), Painter.CircleSdf(x, y, 38, 32, 10)),
                Painter.CircleSdf(x, y, 32, 38, 10));
            p.Fill((x, y) => puff(x, y) - 3.5f, ShotInk);
            p.Fill((x, y) => puff(x, y), (x, y) => Color.Lerp(Hex(0xE6DAF0), Color.white, Mathf.Clamp01((y - 20f) / 26f)));
            return p;
        }

        /// <summary>Donut UFO beam, 64×256 (tall; stretched to the beam's length): hot pink edges, white core.</summary>
        public static Painter LaserBeam()
        {
            var p = new Painter(64, 256);
            p.Fill((x, y) => -1000f, (x, y) =>
            {
                float d = Mathf.Abs(x - 32f) / 32f;
                Color edge = new Color(1f, 0.3f, 0.65f, Mathf.Clamp01(1f - d) * 0.9f);
                return d < 0.28f ? Color.Lerp(Color.white, new Color(1f, 0.6f, 0.85f, 1f), d / 0.28f) : edge;
            });
            return p;
        }

        /// <summary>Laser warning: a thin dashed line, 16×256, shown while the UFO charges.</summary>
        public static Painter LaserWarning()
        {
            var p = new Painter(16, 256);
            p.Fill((x, y) => Mathf.Abs(x - 8f) - 3f, (x, y) =>
                new Color(1f, 0.35f, 0.5f, Mathf.Repeat(y, 24f) < 14f ? 0.95f : 0.25f), 1f);
            return p;
        }
    }
}
