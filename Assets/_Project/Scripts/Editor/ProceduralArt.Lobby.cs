using UnityEngine;

namespace PofudukFilo.EditorTools
{
    /// <summary>
    /// Lobby art for menu v5 (design/ux/main-menu.md §4, mockup design/ux/mockups/menu-v5-mockup.png): the "casual
    /// kit" every top mobile lobby shares — bevelled candy buttons with a thick ink outline, a bottom lip and a top
    /// gloss; dark ink pills and panels; red notification dots; a glowing hero platform and a calm radial backdrop.
    /// </summary>
    public static partial class ArtRecipes
    {
        /// <summary>Ink colour of every lobby outline (#1D1440).</summary>
        public static readonly Color Ink = Hex(0x1D1440);

        /// <summary>
        /// Candy button face, 160×160, 9-sliced at 56: ink outline (8 px) and drop (14 px), a vertical
        /// <paramref name="top"/>→<paramref name="bottom"/> gradient, a darker <paramref name="lip"/> along the bottom and a
        /// glossy band plus a highlight line along the top.
        /// </summary>
        public static Painter CandyButton(Color top, Color bottom, Color lip)
        {
            const int s = 160;
            var p = new Painter(s, s);
            p.Fill((x, y) => Painter.RoundRectSdf(x, y, 1, 1, s - 1, s - 1, 46), Ink, 1.2f);
            Painter.Sdf face = (x, y) => Painter.RoundRectSdf(x, y, 9, 15, s - 9, s - 9, 38);
            p.Fill((x, y) => face(x, y), (x, y) => Color.Lerp(bottom, top, Mathf.Clamp01((y - 30f) / (s - 50f))));
            p.Fill((x, y) => Mathf.Max(face(x, y), y - 37f), lip, 1.5f);
            p.Fill((x, y) => Mathf.Max(face(x, y), (s - 16f) - y), new Color(1f, 1f, 1f, 0.45f), 1.5f);
            p.Fill((x, y) => Painter.RoundRectSdf(x, y, 22, 110, s - 22, 138, 14), new Color(1f, 1f, 1f, 0.32f), 1.5f);
            return p;
        }

        public static Painter ButtonYellow() => CandyButton(Hex(0xFFE86B), Hex(0xFFA60D), Hex(0xE07A00));
        public static Painter ButtonPink() => CandyButton(Hex(0xFF9ED2), Hex(0xF0529C), Hex(0xC8367E));
        public static Painter ButtonBlue() => CandyButton(Hex(0x7FD1FF), Hex(0x3B7BEA), Hex(0x2B5DC0));
        public static Painter ButtonPurple() => CandyButton(Hex(0xC3A6FF), Hex(0x7B55E6), Hex(0x5E3CC4));
        public static Painter ButtonOrange() => CandyButton(Hex(0xFFCF6B), Hex(0xFF8A1F), Hex(0xD96A0A));
        public static Painter ButtonGreen() => CandyButton(Hex(0xB8F36B), Hex(0x43B02A), Hex(0x2F8C1C));
        public static Painter ButtonLavender() => CandyButton(Hex(0x9D8CF0), Hex(0x6A55D6), Hex(0x5140B4));
        public static Painter ButtonViolet() => CandyButton(Hex(0x9A86FF), Hex(0x5A3FD6), Hex(0x4630B0));

        /// <summary>Dark ink capsule for currencies and tags, 160×160 sliced at 78 (a full half-circle each end).</summary>
        public static Painter InkPill()
        {
            const int s = 160;
            var p = new Painter(s, s);
            p.Fill((x, y) => Painter.RoundRectSdf(x, y, 1, 1, s - 1, s - 1, 78), Ink, 1.2f);
            p.Fill((x, y) => Painter.RoundRectSdf(x, y, 7, 7, s - 7, s - 7, 72), WithAlpha(Hex(0x2A1F63), 0.6f), 1.2f);
            return p;
        }

        /// <summary>Dark ink panel with a soft inner fill, 160×160 sliced at 56 (record road, info boxes).</summary>
        public static Painter InkPanel()
        {
            const int s = 160;
            var p = new Painter(s, s);
            p.Fill((x, y) => Painter.RoundRectSdf(x, y, 1, 1, s - 1, s - 1, 46), Ink, 1.2f);
            p.Fill((x, y) => Painter.RoundRectSdf(x, y, 7, 7, s - 7, s - 7, 40), WithAlpha(Hex(0x241A55), 0.55f), 1.2f);
            return p;
        }

        /// <summary>Notification dot / level badge: a glossy disc with an ink rim, 64×64.</summary>
        public static Painter Dot(Color top, Color bottom)
        {
            var p = new Painter(64, 64);
            p.Circle(32, 32, 31, Ink);
            p.Fill((x, y) => Painter.CircleSdf(x, y, 32, 32, 25), (x, y) => Color.Lerp(bottom, top, Mathf.Clamp01((y - 8f) / 48f)));
            p.Fill((x, y) => Painter.EllipseSdf(x, y, 32, 46, 15, 6), new Color(1f, 1f, 1f, 0.4f), 1.5f);
            return p;
        }

        public static Painter DotRed() => Dot(Hex(0xFF6A6A), Hex(0xE0202E));
        public static Painter DotGold() => Dot(Hex(0xFFE45C), Hex(0xFFB300));

        /// <summary>Thin track and fill for bars (record road, level bar), 64×28 sliced at 13.</summary>
        public static Painter BarInk()
        {
            var p = new Painter(64, 28);
            p.Fill((x, y) => Painter.RoundRectSdf(x, y, 0, 0, 64, 28, 14), Hex(0x0D0826), 1.2f);
            return p;
        }

        public static Painter BarGold() => BarGradient(Hex(0xFFE45C), Hex(0xFFA800));
        public static Painter BarGreen() => BarGradient(Hex(0xB8F36B), Hex(0x5FCF2D));

        private static Painter BarGradient(Color top, Color bottom)
        {
            var p = new Painter(64, 28);
            p.Fill((x, y) => Painter.RoundRectSdf(x, y, 0, 0, 64, 28, 14), (x, y) => Color.Lerp(bottom, top, y / 28f), 1.2f);
            p.Fill((x, y) => Painter.RoundRectSdf(x, y, 8, 17, 56, 24, 4), new Color(1f, 1f, 1f, 0.35f), 1.2f);
            return p;
        }

        /// <summary>Glowing hero platform: a cyan disc seen from the side with a bright inner ring, 512×150.</summary>
        public static Painter HeroPlatform()
        {
            const int w = 512, h = 150;
            var p = new Painter(w, h);
            p.Fill((x, y) => Painter.EllipseSdf(x, y, w / 2f, h / 2f, 250, 72), (x, y) =>
            {
                float d = Mathf.Sqrt(Mathf.Pow((x - w / 2f) / 250f, 2) + Mathf.Pow((y - h / 2f) / 72f, 2));
                Color c = d < 0.45f ? Color.Lerp(Hex(0x8FF0FF), Hex(0x46B6FF), d / 0.45f)
                    : d < 0.8f ? Color.Lerp(Hex(0x46B6FF), Hex(0x2A4FC0), (d - 0.45f) / 0.35f)
                    : Color.Lerp(Hex(0x2A4FC0), WithAlpha(Hex(0x2A4FC0), 0f), (d - 0.8f) / 0.2f);
                return c;
            }, 1f);
            p.Fill((x, y) => Mathf.Abs(Painter.EllipseSdf(x, y, w / 2f, h / 2f, 170, 42)) - 3.5f, new Color(0.85f, 0.98f, 1f, 0.85f), 1.5f);
            return p;
        }

        /// <summary>Soft lavender spotlight behind the hero, 256×256.</summary>
        public static Painter Spotlight()
        {
            var p = new Painter(256, 256);
            p.Glow(128, 128, 127, new Color(0.67f, 0.59f, 1f, 0.6f), 1.6f);
            return p;
        }

        /// <summary>Lobby backdrop: a calm radial violet night with sparse stars, 432×936 (stretched full screen).</summary>
        public static Painter LobbyBackdrop()
        {
            const int w = 432, h = 936;
            var p = new Painter(w, h);
            p.Fill((x, y) => -1000f, (x, y) =>
            {
                float dx = (x - w * 0.5f) / (w * 1.2f), dy = (y - h * 0.62f) / (h * 0.7f);
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                return d < 0.45f ? Color.Lerp(Hex(0x4B3AA8), Hex(0x2A1F6E), d / 0.45f)
                    : Color.Lerp(Hex(0x2A1F6E), Hex(0x140F3A), Mathf.Clamp01((d - 0.45f) / 0.55f));
            });
            var rng = new System.Random(21);
            for (int i = 0; i < 70; i++)
            {
                float sx = (float)rng.NextDouble() * w, sy = (float)rng.NextDouble() * h;
                float r = 0.6f + (float)rng.NextDouble() * 1.3f;
                p.Circle(sx, sy, r, new Color(1f, 0.98f, 0.92f, 0.35f + 0.5f * (float)rng.NextDouble()));
            }
            return p;
        }

        /// <summary>Bottom tab bar: dark violet gradient with an ink top edge and a faint highlight, 128×128 sliced 24.</summary>
        public static Painter TabBar()
        {
            var p = new Painter(128, 128);
            p.Fill((x, y) => -1000f, (x, y) => Color.Lerp(Hex(0x1B1447), Hex(0x2C2168), y / 128f));
            p.Fill((x, y) => 120f - y, Hex(0x0D0826), 1f);
            p.Fill((x, y) => Mathf.Max(114f - y, y - 120f), new Color(1f, 1f, 1f, 0.12f), 1f);
            return p;
        }

        /// <summary>Power icon: a red candy tile with a golden lightning bolt, 64×64.</summary>
        public static Painter IconPower()
        {
            var p = new Painter(64, 64);
            p.Fill((x, y) => Painter.RoundRectSdf(x, y, 1, 1, 63, 63, 14), Ink, 1.2f);
            p.Fill((x, y) => Painter.RoundRectSdf(x, y, 6, 6, 58, 58, 10), (x, y) => Color.Lerp(Hex(0xE0202E), Hex(0xFF6A6A), y / 64f));
            Color gold = Hex(0xFFE45C);
            p.Triangle(new Vector2(38, 56), new Vector2(18, 28), new Vector2(34, 30), Ink, 3f);
            p.Triangle(new Vector2(28, 8), new Vector2(48, 36), new Vector2(30, 34), Ink, 3f);
            p.Triangle(new Vector2(38, 56), new Vector2(18, 28), new Vector2(34, 30), gold);
            p.Triangle(new Vector2(28, 8), new Vector2(48, 36), new Vector2(30, 34), gold);
            p.Fill((x, y) => Painter.RoundRectSdf(x, y, 22, 28, 40, 36, 2), gold);
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
