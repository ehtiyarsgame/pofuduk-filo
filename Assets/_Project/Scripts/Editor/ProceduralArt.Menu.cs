using UnityEngine;

namespace PofudukFilo.EditorTools
{
    /// <summary>
    /// Menu dressing (design/ux/main-menu.md): pedestal, light rays, capsules, tab bar, ribbon, shine,
    /// and a matching icon set (gear, play, trophy, blaster, pause, stardust). Same outline and
    /// lighting rules as every other recipe, so the UI reads as one family.
    /// </summary>
    public static partial class ArtRecipes
    {
        private static readonly Color Plum = Hex(0x3B2A5E);
        private static readonly Color DeepPlum = Hex(0x241838);

        /// <summary>Glowing candy platform the hero ship floats above.</summary>
        public static Painter MenuPedestal()
        {
            var p = new Painter(512, 200);
            p.Fill((x, y) => Painter.EllipseSdf(x, y, 256, 92, 250, 80), (x, y) =>
            {
                float d = Mathf.Sqrt(Mathf.Pow((x - 256) / 250f, 2) + Mathf.Pow((y - 92) / 80f, 2));
                return new Color(0.78f, 0.62f, 1f, 0.45f * Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d));
            }, 1f);
            // Side wall (the platform's thickness), then the top face.
            p.Fill((x, y) => Painter.EllipseSdf(x, y, 256, 84, 200, 44) - 6f, Painter.Outline);
            p.Fill((x, y) => Painter.EllipseSdf(x, y, 256, 84, 200, 44), Hex(0x6B4FA8));
            p.Fill((x, y) => Painter.EllipseSdf(x, y, 256, 104, 200, 44) - 6f, Painter.Outline);
            p.Fill((x, y) => Painter.EllipseSdf(x, y, 256, 104, 200, 44), (x, y) =>
                Color.Lerp(Hex(0x8E73D6), Hex(0xC8B6FF), Mathf.Clamp01((y - 64f) / 80f)));
            // Mint energy ring and a soft gloss.
            p.Fill((x, y) => Mathf.Abs(Painter.EllipseSdf(x, y, 256, 106, 150, 30)) - 4f, WithAlpha(Mint, 0.9f));
            p.Fill((x, y) => Painter.EllipseSdf(x, y, 256, 106, 150, 30), WithAlpha(Mint, 0.18f));
            p.Fill((x, y) => Painter.EllipseSdf(x, y, 200, 124, 70, 10), new Color(1f, 1f, 1f, 0.35f), 4f);
            return p;
        }

        /// <summary>Soft radial light rays (rotated slowly behind the hero).</summary>
        public static Painter LightRays()
        {
            var p = new Painter(512, 512);
            p.Fill((x, y) => Painter.CircleSdf(x, y, 256, 256, 255), (x, y) =>
            {
                float dx = x - 256, dy = y - 256;
                float r = Mathf.Sqrt(dx * dx + dy * dy) / 256f;
                float a = Mathf.Atan2(dy, dx);
                float ray = Mathf.Pow(Mathf.Abs(Mathf.Cos(a * 6f)), 6f);
                float fade = Mathf.Clamp01(1f - r) * Mathf.Clamp01(r * 4f);
                return new Color(1f, 0.95f, 0.8f, 0.55f * ray * fade * fade);
            }, 1f);
            return p;
        }

        /// <summary>Currency / info capsule: dark plum pill with a light top edge (sliced 36 px).</summary>
        public static Painter Capsule()
        {
            var p = new Painter(160, 76);
            Painter.Sdf body = (x, y) => Painter.RoundRectSdf(x, y, 2, 2, 158, 74, 36);
            p.Fill((x, y) => body(x, y), Painter.Outline);
            p.Fill((x, y) => body(x, y) + 5f, (x, y) => Color.Lerp(DeepPlum, Plum, Mathf.Clamp01(y / 76f)));
            p.Fill((x, y) => Mathf.Max(Mathf.Abs(body(x, y) + 7f) - 1.2f, 52f - y), new Color(1f, 1f, 1f, 0.28f), 1.2f);
            return p;
        }

        /// <summary>Bottom tab bar: plum slab with a bright top rim (sliced 40 px).</summary>
        public static Painter NavBar()
        {
            var p = new Painter(160, 120);
            Painter.Sdf body = (x, y) => Painter.RoundRectSdf(x, y, 2, -60, 158, 118, 40);
            p.Fill((x, y) => body(x, y), Painter.Outline);
            p.Fill((x, y) => body(x, y) + 5f, (x, y) => Color.Lerp(DeepPlum, Hex(0x4A3570), Mathf.Clamp01(y / 120f)));
            p.Fill((x, y) => Mathf.Max(Mathf.Abs(body(x, y) + 8f) - 1.5f, 90f - y), new Color(1f, 0.85f, 0.95f, 0.35f), 1.2f);
            return p;
        }

        /// <summary>Pink ribbon banner with folded tails (under the logo's second line).</summary>
        public static Painter LogoRibbon()
        {
            var p = new Painter(640, 160);
            Color front = HotPink, back = Painter.Deep(HotPink, 0.35f);
            for (int s = -1; s <= 1; s += 2)
            {
                float cx = 320 + s * 250;
                p.Triangle(new Vector2(cx - s * 70, 30), new Vector2(cx + s * 70, 30), new Vector2(cx + s * 30, 70), Painter.Outline, 6f);
                p.Fill((x, y) => Painter.RoundRectSdf(x, y, cx - 70, 30, cx + 70, 110, 8) - 6f, Painter.Outline);
                p.Fill((x, y) => Painter.RoundRectSdf(x, y, cx - 70, 30, cx + 70, 110, 8), back);
            }
            p.Fill((x, y) => Painter.RoundRectSdf(x, y, 80, 44, 560, 134, 14) - 6f, Painter.Outline);
            p.Fill((x, y) => Painter.RoundRectSdf(x, y, 80, 44, 560, 134, 14), (x, y) =>
                Color.Lerp(Painter.Deep(front, 0.15f), Painter.Light(front, 0.25f), Mathf.Clamp01((y - 44f) / 90f)));
            p.Fill((x, y) => Painter.RoundRectSdf(x, y, 100, 108, 540, 124, 8), new Color(1f, 1f, 1f, 0.3f), 2f);
            return p;
        }

        /// <summary>Diagonal white band swept across the PLAY button.</summary>
        public static Painter ShineBand()
        {
            var p = new Painter(128, 256);
            p.Fill((x, y) => Mathf.Abs((x - 64) - (y - 128) * 0.35f) - 18f, (x, y) =>
            {
                float d = Mathf.Abs((x - 64) - (y - 128) * 0.35f) / 18f;
                return new Color(1f, 1f, 1f, 0.55f * (1f - d * d));
            }, 6f);
            return p;
        }

        /// <summary>Vertical fade (top opaque → bottom clear) framing the menu edges.</summary>
        public static Painter Vignette()
        {
            var p = new Painter(16, 256);
            p.Fill((x, y) => -1f, (x, y) => new Color(0.06f, 0.03f, 0.12f, 0.85f * Mathf.Pow(Mathf.Clamp01(y / 256f), 1.6f)), 1f);
            return p;
        }

        // ---------------------------------------------------------------- Icons

        public static Painter IconGear()
        {
            var p = new Painter(128, 128);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                float cx = 64 + Mathf.Cos(a) * 40, cy = 64 + Mathf.Sin(a) * 40;
                p.Fill((x, y) => Painter.RotEllipseSdf(x, y, cx, cy, 14, 11, a) - 4f, Painter.Outline);
            }
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                float cx = 64 + Mathf.Cos(a) * 40, cy = 64 + Mathf.Sin(a) * 40;
                p.Fill((x, y) => Painter.RotEllipseSdf(x, y, cx, cy, 14, 11, a), Hex(0xDCCFFF));
            }
            p.Blob(64, 64, 36, Hex(0xDCCFFF), 5f);
            p.Circle(64, 64, 15, Painter.Outline);
            p.Circle(64, 64, 10, Plum);
            return p;
        }

        public static Painter IconPlay()
        {
            var p = new Painter(128, 128);
            p.Triangle(new Vector2(40, 22), new Vector2(40, 106), new Vector2(108, 64), Painter.Outline, 8f);
            p.Triangle(new Vector2(40, 22), new Vector2(40, 106), new Vector2(108, 64), Color.white, 2f);
            return p;
        }

        public static Painter IconPause()
        {
            var p = new Painter(128, 128);
            for (int s = -1; s <= 1; s += 2)
            {
                float cx = 64 + s * 20;
                p.RoundedRect(cx - 16, 24, cx + 16, 104, 10, Painter.Outline);
                p.RoundedRect(cx - 10, 30, cx + 10, 98, 7, Color.white);
            }
            return p;
        }

        public static Painter IconTrophy()
        {
            var p = new Painter(128, 128);
            Color gold = Honey;
            // Handles.
            for (int s = -1; s <= 1; s += 2)
            {
                p.Fill((x, y) => Mathf.Abs(Painter.CircleSdf(x, y, 64 + s * 34, 84, 16)) - 7f, Painter.Outline);
                p.Fill((x, y) => Mathf.Abs(Painter.CircleSdf(x, y, 64 + s * 34, 84, 16)) - 3f, gold);
            }
            // Cup, stem, base.
            p.Volume((x, y) => Mathf.Max(Painter.EllipseSdf(x, y, 64, 92, 38, 46), y - 110f), 64, 84, 38, 30, gold, 5f, shadow: false);
            p.RoundedRect(54, 30, 74, 52, 4, Painter.Outline);
            p.RoundedRect(58, 30, 70, 52, 3, Painter.Deep(gold, 0.2f));
            p.RoundedRect(34, 12, 94, 34, 8, Painter.Outline);
            p.RoundedRect(38, 16, 90, 30, 6, Painter.Deep(gold, 0.1f));
            p.Star(64, 86, 13, 6, Color.white);
            return p;
        }

        /// <summary>Candy blaster for the Weapons tab.</summary>
        public static Painter IconWeapons()
        {
            var p = new Painter(256, 256);
            Color body = HotPink, barrel = Hex(0xDCCFFF), grip = Hex(0x6B4FA8);
            // Grip.
            p.Volume((x, y) => Painter.RotEllipseSdf(x, y, 92, 84, 24, 48, 0.3f), 92, 84, 24, 48, grip, 7f, shadow: false, gloss: 0.3f);
            // Barrel.
            p.Volume((x, y) => Painter.RoundRectSdf(x, y, 128, 130, 212, 166, 16), 170, 148, 42, 18, barrel, 7f, shadow: false);
            p.Blob(214, 148, 18, Mint, 6f);
            // Body.
            p.Volume((x, y) => Painter.EllipseSdf(x, y, 96, 148, 70, 48), 96, 148, 70, 48, body, 7f, shadow: false);
            p.Heart(90, 144, 18, Painter.Outline);
            p.Heart(90, 145, 13, Color.white);
            // Muzzle sparkle.
            p.Star(228, 196, 18, 6, Honey, 4);
            return p;
        }

        /// <summary>Stardust: a lilac four-point sparkle with two tiny companions.</summary>
        public static Painter StardustIcon()
        {
            var p = new Painter(96, 96);
            Color c = Lilac;
            p.Glow(48, 48, 46, WithAlpha(c, 0.5f), 1.8f);
            p.Star(46, 46, 36, 11, Painter.Outline, 4);
            p.Fill((x, y) =>
            {
                float dx = x - 46, dy = y - 46, ang = Mathf.Atan2(dy, dx) + Mathf.PI / 2f, seg = Mathf.PI / 2f;
                float t = Mathf.Abs(Mathf.Repeat(ang, seg) - seg * 0.5f) / (seg * 0.5f);
                return Mathf.Sqrt(dx * dx + dy * dy) - Mathf.Lerp(30f, 8f, t);
            }, (x, y) => Color.Lerp(c, Color.white, Mathf.Clamp01((y - 20f) / 60f) * 0.7f));
            p.Star(78, 76, 9, 3, Color.white, 4);
            p.Star(76, 20, 6, 2, Color.white, 4);
            return p;
        }
    }
}
