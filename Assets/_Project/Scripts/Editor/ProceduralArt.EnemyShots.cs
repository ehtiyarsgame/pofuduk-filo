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
        private static readonly Color ShotInk = Hex(0x1A0A24);

        /// <summary>
        /// Shared backing of every enemy shot, sized to fill the quad (device feedback 2026-09-25: "düşman ateşleri
        /// gerçekten hiç görünmüyor"): a strong warm halo to the edge and a dark disc behind the shape, so the shot
        /// reads against any background, star field or crowd. Player shots are cool and translucent; these are hot.
        /// </summary>
        private static void ShotBacking(Painter p, Color halo, float disc)
        {
            p.Glow(32, 32, 32, WithAlpha(halo, 1f), 1.05f);
            p.Circle(32, 32, disc, ShotInk);
        }

        /// <summary>Chick: a bright egg dropped straight down.</summary>
        public static Painter ShotEgg(bool golden)
        {
            var p = new Painter(64, 64);
            ShotBacking(p, golden ? Hex(0xFFC400) : Hex(0xFF8A1F), 21f);
            Color shell = golden ? Hex(0xFFD23A) : Hex(0xFFF3B0);
            p.Fill((x, y) => Painter.EllipseSdf(x, y, 32, 31, 13, 17), (x, y) =>
                Color.Lerp(Painter.Deep(shell, 0.15f), shell, Mathf.Clamp01((y - 14f) / 30f)));
            p.Fill((x, y) => Painter.EllipseSdf(x, y, 32, 30, 6, 8), new Color(1f, 1f, 1f, 0.9f), 3f);
            if (golden) p.Star(32, 30, 7f, 3f, Color.white, 4);
            return p;
        }

        /// <summary>Candy bee: a long hot stinger, drawn pointing up (aligned to its velocity).</summary>
        public static Painter ShotStinger()
        {
            var p = new Painter(64, 64);
            p.Fill((x, y) => Painter.EllipseSdf(x, y, 32, 32, 16, 32), (x, y) =>
            {
                float d = Mathf.Sqrt(Mathf.Pow((x - 32f) / 16f, 2) + Mathf.Pow((y - 32f) / 32f, 2));
                return new Color(1f, 0.35f, 0.05f, Mathf.Clamp01(1f - d) * 1f);
            }, 1f);
            p.Triangle(new Vector2(32, 63), new Vector2(21, 4), new Vector2(43, 4), ShotInk, 2f);
            p.Triangle(new Vector2(32, 58), new Vector2(25, 8), new Vector2(39, 8), Hex(0xFF7A00));
            p.Triangle(new Vector2(32, 54), new Vector2(29, 12), new Vector2(35, 12), Hex(0xFFF6C8));
            return p;
        }

        /// <summary>Cookie robot: a big cookie crumb with chips.</summary>
        public static Painter ShotCrumb()
        {
            var p = new Painter(64, 64);
            ShotBacking(p, Hex(0xFF6A00), 21f);
            Painter.Sdf crumb = (x, y) => Mathf.Min(Painter.CircleSdf(x, y, 30, 34, 13), Painter.CircleSdf(x, y, 37, 28, 11));
            p.Fill((x, y) => crumb(x, y), (x, y) => Color.Lerp(Hex(0xD8894A), Hex(0xFFD08A), Mathf.Clamp01((y - 18f) / 28f)));
            p.Circle(26, 36, 3.2f, Hex(0x4A2412));
            p.Circle(38, 26, 2.8f, Hex(0x4A2412));
            p.Circle(34, 38, 2.4f, Hex(0x4A2412));
            p.Circle(29, 40, 2.4f, new Color(1f, 1f, 1f, 0.9f));
            return p;
        }

        /// <summary>Gum balloon death burst: a hot-pink gum drop.</summary>
        public static Painter ShotGum()
        {
            var p = new Painter(64, 64);
            ShotBacking(p, Hex(0xFF2E7A), 20f);
            p.Fill((x, y) => Painter.CircleSdf(x, y, 32, 32, 15f), (x, y) => Color.Lerp(Hex(0xFF3D8B), Hex(0xFFC2DE), Mathf.Clamp01((y - 18f) / 28f)));
            p.Circle(32, 32, 5.5f, Color.white);
            return p;
        }

        /// <summary>Jelly bear: a wobbly lime jelly blob (big splits into three small ones).</summary>
        public static Painter ShotJelly(bool big)
        {
            var p = new Painter(64, 64);
            float r = big ? 19f : 15f;
            ShotBacking(p, Hex(0x7CFF2E), r + 5f);
            Painter.Sdf blob = (x, y) => Painter.CircleSdf(x, y, 32, 32, r + Mathf.Sin(Mathf.Atan2(y - 32, x - 32) * 5f) * 1.6f);
            p.Fill((x, y) => blob(x, y), (x, y) => Color.Lerp(Hex(0x2FC43A), Hex(0xB8FF7A), Mathf.Clamp01((y - 14f) / 36f)));
            p.Fill((x, y) => Painter.EllipseSdf(x, y, 32, 33, r * 0.38f, r * 0.3f), new Color(1f, 1f, 1f, 0.85f), 2f);
            return p;
        }

        /// <summary>Ice-cream tower: a bright strawberry scoop with a cone rim.</summary>
        public static Painter ShotScoop()
        {
            var p = new Painter(64, 64);
            ShotBacking(p, Hex(0xFF3B4E), 21f);
            Painter.Sdf scoop = (x, y) => Painter.CircleSdf(x, y, 32, 32, 16f + Mathf.Sin(Mathf.Atan2(y - 32, x - 32) * 8f) * 1.2f);
            p.Fill((x, y) => scoop(x, y), (x, y) => Color.Lerp(Hex(0xFF5C8A), Hex(0xFFD8E6), Mathf.Clamp01((y - 18f) / 28f)));
            p.Fill((x, y) => Mathf.Max(scoop(x, y), y - 25f), Hex(0xFFB84A));
            p.Circle(32, 35, 4.5f, Color.white);
            return p;
        }

        /// <summary>Marshmallow: a white puff with a hot halo.</summary>
        public static Painter ShotPuff()
        {
            var p = new Painter(64, 64);
            ShotBacking(p, Hex(0xFF4A6A), 22f);
            Painter.Sdf puff = (x, y) => Mathf.Min(Mathf.Min(Painter.CircleSdf(x, y, 25, 31, 10.5f), Painter.CircleSdf(x, y, 39, 31, 10.5f)),
                Painter.CircleSdf(x, y, 32, 38, 10.5f));
            p.Fill((x, y) => puff(x, y), (x, y) => Color.Lerp(Hex(0xFFE0EA), Color.white, Mathf.Clamp01((y - 20f) / 26f)));
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
