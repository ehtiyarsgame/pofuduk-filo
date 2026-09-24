using System.IO;
using UnityEditor;
using UnityEngine;

namespace PofudukFilo.EditorTools
{
    /// <summary>
    /// Tiny anti-aliased painter for placeholder art that already follows the art bible: round
    /// shapes, a 3–4 px dark plum outline (#3B2A4F), one highlight, big chibi eyes, blush.
    /// Replace the generated PNGs with real art later — every reference goes through the sprite.
    /// </summary>
    public sealed class Painter
    {
        public static readonly Color Outline = new(0.231f, 0.165f, 0.310f, 1f);

        private readonly int _w;
        private readonly int _h;
        private readonly Color[] _px;

        public Painter(int width, int height)
        {
            _w = width;
            _h = height;
            _px = new Color[width * height];
        }

        public int Width => _w;
        public int Height => _h;

        // ---------------------------------------------------------------- Primitives

        public delegate float Coverage(float x, float y);

        /// <summary>Paints every pixel by a signed-distance-like function (≤0 inside) with AA.</summary>
        public void Fill(Coverage sdf, Color color, float softness = 1.2f)
        {
            for (int y = 0; y < _h; y++)
            for (int x = 0; x < _w; x++)
            {
                float d = sdf(x + 0.5f, y + 0.5f);
                float a = Mathf.Clamp01(0.5f - d / softness);
                if (a <= 0f) continue;
                Blend(x, y, color, a);
            }
        }

        public void Circle(float cx, float cy, float r, Color c) =>
            Fill((x, y) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r, c);

        public void Ellipse(float cx, float cy, float rx, float ry, Color c) =>
            Fill((x, y) =>
            {
                float nx = (x - cx) / rx, ny = (y - cy) / ry;
                return (Mathf.Sqrt(nx * nx + ny * ny) - 1f) * Mathf.Min(rx, ry);
            }, c);

        public void RoundedRect(float x0, float y0, float x1, float y1, float radius, Color c) =>
            Fill((x, y) =>
            {
                float cx = (x0 + x1) * 0.5f, cy = (y0 + y1) * 0.5f;
                float hx = (x1 - x0) * 0.5f - radius, hy = (y1 - y0) * 0.5f - radius;
                float qx = Mathf.Abs(x - cx) - hx, qy = Mathf.Abs(y - cy) - hy;
                float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
                return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
            }, c);

        public void Diamond(float cx, float cy, float r, Color c) =>
            Fill((x, y) => (Mathf.Abs(x - cx) + Mathf.Abs(y - cy) - r) * 0.707f, c);

        public void Star(float cx, float cy, float outer, float inner, Color c, int points = 5) =>
            Fill((x, y) =>
            {
                float dx = x - cx, dy = y - cy;
                float ang = Mathf.Atan2(dy, dx) + Mathf.PI / 2f;
                float seg = Mathf.PI * 2f / points;
                float t = Mathf.Abs(Mathf.Repeat(ang, seg) - seg * 0.5f) / (seg * 0.5f); // 0 at tip, 1 between
                float radius = Mathf.Lerp(outer, inner, t);
                return Mathf.Sqrt(dx * dx + dy * dy) - radius;
            }, c);

        public void Heart(float cx, float cy, float size, Color c) =>
            Fill((x, y) =>
            {
                float nx = (x - cx) / size, ny = (y - cy) / size + 0.15f;
                float v = Mathf.Pow(nx * nx + ny * ny - 1f, 3f) - nx * nx * ny * ny * ny;
                return v <= 0f ? -1f : 1f;
            }, c, 1f);

        /// <summary>Convex triangle (signed edge distances), optionally grown by <paramref name="grow"/> px.</summary>
        public void Triangle(Vector2 a, Vector2 b, Vector2 c, Color color, float grow = 0f)
        {
            Vector2[] pts = { a, b, c };
            float area = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
            float sign = area > 0f ? 1f : -1f;
            Fill((x, y) =>
            {
                float d = float.MinValue;
                for (int i = 0; i < 3; i++)
                {
                    Vector2 p0 = pts[i], p1 = pts[(i + 1) % 3];
                    Vector2 e = p1 - p0;
                    float edge = sign * ((x - p0.x) * e.y - (y - p0.y) * e.x) / e.magnitude;
                    d = Mathf.Max(d, edge);
                }
                return d - grow;
            }, color);
        }

        // ---------------------------------------------------------------- Chibi helpers

        /// <summary>Pointed outlined ear (cat, fox) with an optional inner colour.</summary>
        public void Ear(float cx, float baseY, float halfWidth, float height, float tilt, Color fill, Color? inner = null)
        {
            var a = new Vector2(cx - halfWidth, baseY);
            var b = new Vector2(cx + halfWidth, baseY);
            var tip = new Vector2(cx + tilt, baseY + height);
            Triangle(a, b, tip, Outline, 7f);
            Triangle(a, b, tip, fill);
            if (inner.HasValue)
                Triangle(new Vector2(cx - halfWidth * 0.5f, baseY + 4f), new Vector2(cx + halfWidth * 0.5f, baseY + 4f),
                    new Vector2(cx + tilt * 0.6f, baseY + height * 0.6f), inner.Value);
        }

        /// <summary>Outlined round body with a soft highlight.</summary>
        public void Blob(float cx, float cy, float r, Color fill, float outline = 7f)
        {
            Circle(cx, cy, r + outline, Outline);
            Circle(cx, cy, r, fill);
            Ellipse(cx - r * 0.35f, cy + r * 0.42f, r * 0.28f, r * 0.18f, new Color(1f, 1f, 1f, 0.55f));
        }

        public void OutlinedEllipse(float cx, float cy, float rx, float ry, Color fill, float outline = 6f)
        {
            Ellipse(cx, cy, rx + outline, ry + outline, Outline);
            Ellipse(cx, cy, rx, ry, fill);
        }

        /// <summary>Big glossy chibi eyes (≈35–40 % of the face, two highlights) plus blush.</summary>
        public void Face(float cx, float cy, float faceRadius, bool determined = false)
        {
            float spacing = faceRadius * 0.4f;
            float ey = cy + faceRadius * 0.05f;
            float size = faceRadius * 0.26f;
            for (int s = -1; s <= 1; s += 2)
            {
                float ex = cx + s * spacing;
                Ellipse(ex, ey, size * 0.78f, size, Outline);
                Circle(ex - size * 0.28f, ey + size * 0.38f, size * 0.32f, Color.white);
                Circle(ex + size * 0.22f, ey - size * 0.3f, size * 0.15f, Color.white);
                Ellipse(ex + s * size * 0.4f, ey - size * 1.5f, size * 0.6f, size * 0.35f, new Color(1f, 0.45f, 0.6f, 0.45f));
                if (determined)
                    Fill((x, y) =>
                    {
                        // Little slanted brow: a thin rotated capsule above each eye.
                        float bx = ex, by = ey + size * 1.45f;
                        float dx = x - bx, dy = y - by;
                        float rot = s * 0.35f;
                        float rx = dx * Mathf.Cos(rot) + dy * Mathf.Sin(rot);
                        float ry = -dx * Mathf.Sin(rot) + dy * Mathf.Cos(rot);
                        return Mathf.Max(Mathf.Abs(rx) - size * 0.7f, Mathf.Abs(ry) - size * 0.12f);
                    }, Outline);
            }
            // Tiny "w" mouth.
            Ellipse(cx - size * 0.25f, cy - faceRadius * 0.22f, size * 0.28f, size * 0.18f, Outline);
            Ellipse(cx + size * 0.25f, cy - faceRadius * 0.22f, size * 0.28f, size * 0.18f, Outline);
        }

        // ---------------------------------------------------------------- Output

        private void Blend(int x, int y, Color c, float coverage)
        {
            int i = y * _w + x;
            Color dst = _px[i];
            float a = c.a * coverage;
            float outA = a + dst.a * (1f - a);
            if (outA <= 0f) return;
            Color outC = (c * a + dst * dst.a * (1f - a)) / outA;
            outC.a = outA;
            _px[i] = outC;
        }

        /// <summary>Writes the PNG and imports it as a sprite. Returns the sprite asset.</summary>
        public Sprite SaveSprite(string assetPath, float pixelsPerUnit, Vector4 border = default)
        {
            var tex = new Texture2D(_w, _h, TextureFormat.RGBA32, false);
            tex.SetPixels(_px);
            tex.Apply();
            SetupUtil.EnsureFolder(Path.GetDirectoryName(assetPath)!.Replace('\\', '/'));
            File.WriteAllBytes(assetPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.spriteBorder = border;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }
    }

    /// <summary>Recipes for every placeholder sprite (colours from art-bible §2.1).</summary>
    public static class ArtRecipes
    {
        public static readonly Color Pink = Hex(0xF7A6C1);
        public static readonly Color HotPink = Hex(0xFF4F9A);
        public static readonly Color Mint = Hex(0x7FE0C4);
        public static readonly Color Cream = Hex(0xFFE8A3);
        public static readonly Color Honey = Hex(0xFFC84A);
        public static readonly Color Coral = Hex(0xFF6B5E);
        public static readonly Color Sky = Hex(0x6EC6FF);
        public static readonly Color Leaf = Hex(0x8BE28B);
        public static readonly Color Gem = Hex(0xFF9EDB);
        public static readonly Color Lilac = Hex(0xC8B6FF);
        public static readonly Color Chick = Hex(0xFFE066);
        public static readonly Color Cookie = Hex(0xE0B07A);
        public static readonly Color Choc = Hex(0x7A4A36);

        public static Color Hex(int rgb) =>
            new(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);

        public static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }

        public static Painter Bunny()
        {
            var p = new Painter(256, 256);
            // Ears first so the head overlaps them.
            p.OutlinedEllipse(96, 196, 24, 52, Color.white);
            p.OutlinedEllipse(160, 196, 24, 52, Color.white);
            p.Ellipse(96, 196, 11, 36, Pink);
            p.Ellipse(160, 196, 11, 36, Pink);
            p.Blob(128, 110, 78, Hex(0xFFF7FB));
            p.Face(128, 108, 78);
            // Mint jet puff under the ship.
            p.Circle(128, 22, 16, WithAlpha(Mint, 0.9f));
            return p;
        }

        // Hangar pilots (friendly faces — only enemies get the determined brows).

        public static Painter ChickPilot()
        {
            var p = new Painter(256, 256);
            p.Circle(128, 222, 14, Painter.Outline);
            p.Circle(128, 222, 9, Honey);
            p.Blob(128, 118, 88, Chick);
            p.RoundedRect(64, 150, 192, 178, 12, Painter.Outline); // pilot goggles strap
            p.Circle(100, 164, 20, Painter.Outline);
            p.Circle(156, 164, 20, Painter.Outline);
            p.Circle(100, 164, 14, WithAlpha(Sky, 0.8f));
            p.Circle(156, 164, 14, WithAlpha(Sky, 0.8f));
            p.Face(128, 112, 80);
            p.OutlinedEllipse(128, 78, 16, 10, Hex(0xFF9F43), 4f);
            return p;
        }

        public static Painter CatPilot()
        {
            var p = new Painter(256, 256);
            p.Ear(78, 160, 34, 78, -14, Hex(0xFFD8A8), Pink);
            p.Ear(178, 160, 34, 78, 14, Hex(0xFFD8A8), Pink);
            p.Blob(128, 112, 86, Hex(0xFFD8A8));
            p.Face(128, 112, 86);
            return p;
        }

        public static Painter Hamster()
        {
            var p = new Painter(256, 256);
            p.Blob(64, 196, 28, Hex(0xF2C38B));
            p.Blob(192, 196, 28, Hex(0xF2C38B));
            p.Circle(64, 196, 14, Pink);
            p.Circle(192, 196, 14, Pink);
            p.Blob(128, 114, 92, Hex(0xF2C38B));
            p.Ellipse(128, 82, 62, 44, Hex(0xFFF3E0)); // cheek pouch
            p.Face(128, 116, 90);
            return p;
        }

        public static Painter Fox()
        {
            var p = new Painter(256, 256);
            p.Ear(76, 160, 36, 88, -18, Hex(0xFF9F5A), Hex(0xFFF6EC));
            p.Ear(180, 160, 36, 88, 18, Hex(0xFF9F5A), Hex(0xFFF6EC));
            p.Blob(128, 112, 88, Hex(0xFF9F5A));
            p.Ellipse(128, 80, 58, 40, Hex(0xFFF6EC));
            p.Face(128, 114, 88);
            p.Star(128, 30, 18, 8, Honey);
            return p;
        }

        public static Painter MysteryBunny()
        {
            Painter p = Bunny();
            p.Star(200, 214, 26, 11, Painter.Outline);
            p.Star(200, 214, 20, 8, Honey);
            return p;
        }

        public static Painter ChickEnemy(bool crown)
        {
            var p = new Painter(256, 256);
            p.Circle(128, 222, 14, Painter.Outline);
            p.Circle(128, 222, 9, Honey); // tuft
            p.Blob(128, 120, 90, Chick);
            p.OutlinedEllipse(48, 110, 22, 32, Hex(0xFFD23F), 5f); // wings
            p.OutlinedEllipse(208, 110, 22, 32, Hex(0xFFD23F), 5f);
            p.Face(128, 128, 90, determined: true);
            p.OutlinedEllipse(128, 88, 18, 11, Hex(0xFF9F43), 4f); // beak
            if (crown)
            {
                p.RoundedRect(88, 200, 168, 226, 6, Painter.Outline);
                p.RoundedRect(92, 204, 164, 222, 5, Honey);
                for (int i = -1; i <= 1; i++)
                {
                    p.Circle(128 + i * 30, 238, 12, Painter.Outline);
                    p.Circle(128 + i * 30, 238, 8, Honey);
                }
            }
            return p;
        }

        public static Painter JellyBear(bool crown)
        {
            var p = new Painter(256, 256);
            Color jelly = WithAlpha(Lilac, 0.92f);
            p.Blob(70, 200, 26, jelly);
            p.Blob(186, 200, 26, jelly);
            p.Blob(128, 118, 92, jelly);
            p.Heart(128, 66, 22, WithAlpha(HotPink, 0.8f)); // sugar heart inside
            p.Face(128, 132, 92, determined: true);
            if (crown)
            {
                p.Star(128, 226, 26, 12, Painter.Outline);
                p.Star(128, 226, 20, 9, Honey);
            }
            return p;
        }

        public static Painter CookieRobot(bool big)
        {
            var p = new Painter(256, 256);
            p.RoundedRect(124, 196, 132, 236, 3, Painter.Outline); // antenna
            p.Circle(128, 240, 12, Painter.Outline);
            p.Circle(128, 240, 8, Coral);
            p.Blob(128, 118, 90, Cookie);
            var rng = new System.Random(7);
            for (int i = 0; i < (big ? 9 : 6); i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float d = 40f + (float)rng.NextDouble() * 38f;
                float x = 128 + Mathf.Cos(a) * d, y = 118 + Mathf.Sin(a) * d;
                if (Mathf.Abs(y - 124) < 26 && Mathf.Abs(x - 128) < 60) continue; // keep the face clear
                p.Circle(x, y, 9, Choc);
            }
            p.Face(128, 124, 90, determined: true);
            return p;
        }

        public static Painter IceCreamTower()
        {
            var p = new Painter(256, 256);
            p.Blob(128, 60, 54, Mint);
            p.Blob(128, 130, 58, Cream);
            p.Blob(128, 198, 50, Pink);
            p.Face(128, 132, 58, determined: true);
            return p;
        }

        public static Painter GumBalloon()
        {
            var p = new Painter(256, 256);
            p.Circle(128, 22, 12, Painter.Outline);
            p.Circle(128, 22, 7, HotPink);
            p.Blob(128, 130, 94, Hex(0xFF7AC2));
            p.Face(128, 130, 94);
            return p;
        }

        public static Painter QueenHen()
        {
            var p = new Painter(256, 256);
            for (int i = -1; i <= 1; i++)
            {
                p.Circle(128 + i * 22, 228, 18, Painter.Outline);
                p.Circle(128 + i * 22, 228, 13, Coral); // comb
            }
            p.Blob(128, 118, 96, Color.white);
            p.OutlinedEllipse(40, 104, 20, 40, Hex(0xF1E9FF), 5f);
            p.OutlinedEllipse(216, 104, 20, 40, Hex(0xF1E9FF), 5f);
            p.Face(128, 128, 96, determined: true);
            p.OutlinedEllipse(128, 86, 20, 12, Hex(0xFF9F43), 4f);
            p.Star(128, 200, 22, 10, Painter.Outline);
            p.Star(128, 200, 17, 8, Honey);
            return p;
        }

        public static Painter Cat()
        {
            var p = new Painter(128, 128);
            p.Ear(38, 80, 17, 40, -7, Hex(0xFFD8A8), Pink);
            p.Ear(90, 80, 17, 40, 7, Hex(0xFFD8A8), Pink);
            p.Blob(64, 60, 42, Hex(0xFFD8A8), 5f);
            p.Face(64, 62, 42);
            return p;
        }

        // Bullets, pickups and small props (64 px, 1 unit).

        public static Painter EnemyBullet(Color body)
        {
            var p = new Painter(64, 64);
            p.Circle(32, 32, 29, Painter.Outline);
            p.Circle(32, 32, 24, body);
            p.Circle(32, 32, 12, Color.white); // white core = top of the value hierarchy
            return p;
        }

        public static Painter Feather(bool giant)
        {
            var p = new Painter(64, 64);
            p.Ellipse(32, 32, giant ? 16 : 11, 28, WithAlpha(Hex(0xDFFFF4), 0.75f)); // soft, no outline (art-bible §2.2)
            p.Ellipse(32, 36, giant ? 7 : 4, 18, WithAlpha(Color.white, 0.8f));
            return p;
        }

        public static Painter MiniStar(Color c)
        {
            var p = new Painter(64, 64);
            p.Star(32, 32, 28, 13, WithAlpha(c, 0.85f));
            return p;
        }

        public static Painter SoftCircle(Color c, int size = 64)
        {
            var p = new Painter(size, size);
            p.Circle(size * 0.5f, size * 0.5f, size * 0.46f, c);
            return p;
        }

        public static Painter GemSprite(Color c)
        {
            var p = new Painter(64, 64);
            p.Diamond(32, 32, 28, Painter.Outline);
            p.Diamond(32, 32, 22, c);
            p.Diamond(26, 38, 7, WithAlpha(Color.white, 0.7f));
            return p;
        }

        public static Painter Coin()
        {
            var p = new Painter(64, 64);
            p.Circle(32, 32, 28, Painter.Outline);
            p.Circle(32, 32, 23, Honey);
            p.Circle(32, 32, 14, Hex(0xFFDB7A));
            return p;
        }

        public static Painter HeartPickup()
        {
            var p = new Painter(64, 64);
            p.Heart(32, 30, 27, Painter.Outline);
            p.Heart(32, 31, 21, HotPink);
            return p;
        }

        public static Painter MagnetPickup()
        {
            var p = new Painter(64, 64);
            p.Circle(32, 32, 28, Painter.Outline);
            p.Circle(32, 32, 23, Coral);
            p.Circle(32, 32, 11, Color.white);
            p.RoundedRect(26, 4, 38, 30, 4, Coral);
            return p;
        }

        public static Painter BombPickup()
        {
            var p = new Painter(64, 64);
            p.RoundedRect(29, 44, 35, 60, 3, Painter.Outline);
            p.Circle(32, 60, 5, Honey);
            p.Circle(32, 28, 26, Painter.Outline);
            p.Circle(32, 28, 21, Lilac);
            p.Ellipse(24, 36, 7, 5, WithAlpha(Color.white, 0.7f));
            return p;
        }

        public static Painter Egg()
        {
            var p = new Painter(64, 64);
            p.Ellipse(32, 30, 22, 28, Painter.Outline);
            p.Ellipse(32, 30, 17, 23, Hex(0xFFF6E0));
            p.Ellipse(26, 40, 5, 7, WithAlpha(Color.white, 0.8f));
            return p;
        }

        public static Painter Bubble()
        {
            var p = new Painter(64, 64);
            p.Circle(32, 32, 29, WithAlpha(Painter.Outline, 0.8f));
            p.Circle(32, 32, 25, WithAlpha(Hex(0xFF9ED6), 0.75f));
            p.Ellipse(24, 42, 7, 5, WithAlpha(Color.white, 0.85f));
            return p;
        }

        /// <summary>9-slice base for every UI panel and button.</summary>
        public static Painter RoundedPanel()
        {
            var p = new Painter(96, 96);
            p.RoundedRect(0, 0, 96, 96, 30, Color.white);
            return p;
        }
    }
}
