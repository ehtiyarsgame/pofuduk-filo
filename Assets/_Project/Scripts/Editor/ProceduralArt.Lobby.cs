using UnityEngine;

namespace PofudukFilo.EditorTools
{
    /// <summary>
    /// Menu v4 lobby art (design/ux/main-menu.md §3), after the market-standard lobby of Archero / Survivor.io /
    /// Capybara Go: an illustrated stage card, record chests for the chest track, a round frame for the side event
    /// icons and the raised home bubble of the bottom bar.
    /// </summary>
    public static partial class ArtRecipes
    {
        /// <summary>Stage card: a framed window onto a candy nebula with a ringed planet, a moon and stars.</summary>
        public static Painter StageCard()
        {
            const int w = 512, h = 360;
            var p = new Painter(w, h);
            Painter.Sdf frame = (x, y) => Painter.RoundRectSdf(x, y, 4, 4, w - 4, h - 4, 44);
            p.Fill((x, y) => frame(x, y) - 6f, Painter.Outline);
            // Nebula sky.
            p.Fill((x, y) => frame(x, y), (x, y) =>
            {
                float t = y / h;
                Color c = Color.Lerp(Hex(0x2A1B5E), Hex(0x5B2E8A), t);
                float n = Painter.TileFbm(x, y, w, h, 4, 3, 11);
                c = Color.Lerp(c, Hex(0xFF7AC2), Mathf.Clamp01((n - 0.52f) * 2.2f) * 0.55f * (1f - t * 0.4f));
                c = Color.Lerp(c, Hex(0x6EC6FF), Mathf.Clamp01((0.42f - n) * 2.4f) * 0.35f);
                return c;
            });
            var rng = new System.Random(5);
            for (int i = 0; i < 60; i++)
            {
                float sx = 20 + (float)rng.NextDouble() * (w - 40), sy = 20 + (float)rng.NextDouble() * (h - 40);
                float r = 0.8f + (float)rng.NextDouble() * 2.2f;
                p.Fill((x, y) => Mathf.Max(Painter.CircleSdf(x, y, sx, sy, r), frame(x, y)), new Color(1f, 0.97f, 0.9f, 0.9f), 1f);
            }
            for (int i = 0; i < 5; i++)
            {
                float sx = 40 + (float)rng.NextDouble() * (w - 80), sy = 60 + (float)rng.NextDouble() * (h - 100);
                p.Star(sx, sy, 7f, 2.4f, new Color(1f, 0.96f, 0.8f, 0.95f), 4);
            }
            // Big ringed candy planet (lower right) and a small moon (upper left), clipped to the card.
            Painter.Sdf planet = (x, y) => Painter.CircleSdf(x, y, 400, 90, 92);
            p.Fill((x, y) => Mathf.Max(planet(x, y), frame(x, y)), (x, y) =>
            {
                float stripe = Mathf.Sin((y - 90) * 0.11f + (x - 400) * 0.02f);
                Color c = Color.Lerp(Hex(0xFF9FCF), Hex(0xFFD0E6), stripe * 0.5f + 0.5f);
                float shade = Mathf.Clamp01(((x - 400) + (y - 90)) / 180f + 0.5f);
                return Color.Lerp(c, Painter.Deep(c, 0.5f), 1f - shade);
            });
            p.Fill((x, y) => Mathf.Max(Mathf.Max(Mathf.Abs(Painter.RotEllipseSdf(x, y, 400, 90, 150, 30, -0.25f)) - 5f, -(y - 70f + (x - 400) * 0.25f)), frame(x, y)),
                new Color(1f, 0.9f, 0.55f, 0.95f), 1.5f);
            p.Fill((x, y) => Mathf.Max(Painter.CircleSdf(x, y, 92, 290, 30), frame(x, y)), (x, y) =>
                Color.Lerp(Hex(0xB9A8F0), Hex(0xE8E0FF), Mathf.Clamp01((y - 270f) / 40f)));
            // Soft floor glow where the hero floats, and a glassy top highlight.
            p.Fill((x, y) => Mathf.Max(Painter.EllipseSdf(x, y, 256, 40, 190, 70), frame(x, y)), (x, y) =>
                new Color(0.78f, 0.62f, 1f, 0.35f * Mathf.Clamp01(1f - Mathf.Abs(y - 40f) / 70f)), 1f);
            p.Fill((x, y) => Mathf.Max(frame(x, y), (h - 60f) - y), new Color(1f, 1f, 1f, 0.08f), 1f);
            p.Fill((x, y) => Mathf.Abs(frame(x, y) + 5f) - 1.5f, new Color(1f, 0.85f, 0.95f, 0.45f), 1.2f);
            return p;
        }

        /// <summary>Record chest: a candy chest, glowing gold when open.</summary>
        public static Painter Chest(bool open)
        {
            var p = new Painter(128, 128);
            Color wood = open ? Honey : Hex(0x9C7BFF), trim = open ? Hex(0xFFF1B0) : Hex(0xFFC84A);
            if (open) p.Glow(64, 70, 62, WithAlpha(Honey, 0.6f), 1.6f);
            p.Volume((x, y) => Painter.RoundRectSdf(x, y, 16, 18, 112, 72, 12), 64, 45, 48, 27, wood, 6f, shadow: false);
            if (open)
                p.Volume((x, y) => Painter.RotEllipseSdf(x, y, 64, 96, 50, 20, 0.2f), 64, 96, 50, 20, Painter.Deep(wood, 0.2f), 6f, shadow: false);
            else
                p.Volume((x, y) => Mathf.Max(Painter.EllipseSdf(x, y, 64, 72, 50, 34), 72f - y), 64, 86, 50, 20, wood, 6f, shadow: false, gloss: 0.6f);
            p.Fill((x, y) => Painter.RoundRectSdf(x, y, 56, 18, 72, open ? 72 : 104, 4), trim);
            p.Fill((x, y) => Painter.RoundRectSdf(x, y, 16, 64, 112, 74, 3), trim);
            p.Circle(64, 70, 9, Painter.Outline);
            p.Circle(64, 70, 6, open ? Color.white : Honey);
            if (open) for (int i = 0; i < 3; i++) p.Star(40 + i * 24, 104 + (i % 2) * 8, 7, 2.6f, Color.white, 4);
            return p;
        }

        /// <summary>Raised home bubble of the bottom bar: a glossy pink disc with a rim, drawn behind the centre tab.</summary>
        public static Painter HomeBubble()
        {
            var p = new Painter(200, 200);
            p.Glow(100, 100, 98, WithAlpha(HotPink, 0.45f), 1.6f);
            p.Volume((x, y) => Painter.CircleSdf(x, y, 100, 100, 78), 100, 100, 78, 78, HotPink, 7f, shadow: false, gloss: 0.8f);
            p.Fill((x, y) => Mathf.Abs(Painter.CircleSdf(x, y, 100, 100, 68)) - 2f, new Color(1f, 1f, 1f, 0.35f), 1.2f);
            return p;
        }

        /// <summary>Home icon: a candy house with a heart door.</summary>
        public static Painter IconHome()
        {
            var p = new Painter(128, 128);
            p.Triangle(new Vector2(64, 116), new Vector2(10, 64), new Vector2(118, 64), Painter.Outline, 6f);
            p.Triangle(new Vector2(64, 110), new Vector2(18, 66), new Vector2(110, 66), Coral);
            p.Volume((x, y) => Painter.RoundRectSdf(x, y, 26, 14, 102, 70, 8), 64, 42, 38, 28, Hex(0xFFF1F7), 6f, shadow: false);
            p.Heart(64, 30, 14, HotPink);
            return p;
        }
    }
}
