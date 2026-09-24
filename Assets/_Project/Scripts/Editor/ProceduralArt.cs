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
        /// <summary>Raw pixels, bottom row first (Texture2D order). Used by the offline art preview.</summary>
        public Color[] Pixels => _px;

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

        /// <summary>Exact heart distance (Inigo Quilez's sdHeart), ~2×<paramref name="size"/> tall, centred on (cx, cy).</summary>
        public static float HeartSdf(float x, float y, float cx, float cy, float size)
        {
            float k = size * 2f;
            float px = Mathf.Abs(x - cx) / k, py = (y - cy) / k + 0.5f;
            float d;
            if (py + px > 1f)
                d = Mathf.Sqrt((px - 0.25f) * (px - 0.25f) + (py - 0.75f) * (py - 0.75f)) - 0.35355f;
            else
            {
                float a = px * px + (py - 1f) * (py - 1f);
                float m = 0.5f * Mathf.Max(px + py, 0f);
                float b = (px - m) * (px - m) + (py - m) * (py - m);
                d = Mathf.Sqrt(Mathf.Min(a, b)) * Mathf.Sign(px - py);
            }
            return d * k;
        }

        public void Heart(float cx, float cy, float size, Color c) => Fill((x, y) => HeartSdf(x, y, cx, cy, size), c);

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

        // ---------------------------------------------------------------- Shaded volumes (art-bible §2.2)

        public delegate float Sdf(float x, float y);
        public delegate Color Shader(float x, float y);

        /// <summary>Per-pixel coloured fill (gradients) inside a signed-distance shape.</summary>
        public void Fill(Coverage sdf, Shader shade, float softness = 1.2f)
        {
            for (int y = 0; y < _h; y++)
            for (int x = 0; x < _w; x++)
            {
                float px = x + 0.5f, py = y + 0.5f;
                float d = sdf(px, py);
                float a = Mathf.Clamp01(0.5f - d / softness);
                if (a <= 0f) continue;
                Blend(x, y, shade(px, py), a);
            }
        }

        public static Color Light(Color c, float t = 0.28f) => Color.Lerp(c, new Color(1f, 0.98f, 0.94f, c.a), t);
        public static Color Deep(Color c, float t = 0.38f) => Color.Lerp(c, new Color(0.36f, 0.22f, 0.52f, c.a), t);

        public static float CircleSdf(float x, float y, float cx, float cy, float r) =>
            Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r;

        public static float EllipseSdf(float x, float y, float cx, float cy, float rx, float ry)
        {
            float nx = (x - cx) / rx, ny = (y - cy) / ry;
            return (Mathf.Sqrt(nx * nx + ny * ny) - 1f) * Mathf.Min(rx, ry);
        }

        public static float RotEllipseSdf(float x, float y, float cx, float cy, float rx, float ry, float angle)
        {
            float dx = x - cx, dy = y - cy;
            float c = Mathf.Cos(-angle), sn = Mathf.Sin(-angle);
            return EllipseSdf(cx + dx * c - dy * sn, cy + dx * sn + dy * c, cx, cy, rx, ry);
        }

        public static float RoundRectSdf(float x, float y, float x0, float y0, float x1, float y1, float radius)
        {
            float cx = (x0 + x1) * 0.5f, cy = (y0 + y1) * 0.5f;
            float hx = (x1 - x0) * 0.5f - radius, hy = (y1 - y0) * 0.5f - radius;
            float qx = Mathf.Abs(x - cx) - hx, qy = Mathf.Abs(y - cy) - hy;
            return new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
        }

        /// <summary>
        /// The house "toy vinyl" volume: soft contact shadow, dark plum outline, top-left key light
        /// gradient, a darker occluded belly, a cool rim light on the upper right and a glossy
        /// specular. <paramref name="cx"/>/<paramref name="cy"/>/<paramref name="rx"/>/<paramref name="ry"/>
        /// describe the shape's extent for the lighting; <paramref name="sdf"/> is its exact outline.
        /// </summary>
        public void Volume(Sdf sdf, float cx, float cy, float rx, float ry, Color baseColor,
            float outline = 7f, bool shadow = true, float gloss = 1f)
        {
            float r = Mathf.Min(rx, ry);
            if (shadow)
                Fill((x, y) => sdf(x, y + r * 0.10f) - outline, new Color(0.08f, 0.03f, 0.14f, 0.30f), r * 0.22f);
            if (outline > 0f) Fill((x, y) => sdf(x, y) - outline, Outline);

            Color lit = Light(baseColor), mid = baseColor, dark = Deep(baseColor);
            Fill((x, y) => sdf(x, y), (x, y) =>
            {
                float nx = (x - cx) / rx, ny = (y - cy) / ry;
                float t = Mathf.Clamp01(0.55f + 0.5f * (ny * 0.85f - nx * 0.25f));
                t = t * t * (3f - 2f * t);
                return t > 0.5f ? Color.Lerp(mid, lit, (t - 0.5f) * 2f) : Color.Lerp(dark, mid, t * 2f);
            });
            // Occluded belly: inside the shape, outside a copy nudged up-left.
            Fill((x, y) => Mathf.Max(sdf(x, y), -sdf(x + r * 0.05f, y - r * 0.16f)),
                Deep(baseColor, 0.55f) * new Color(1f, 1f, 1f, 0.55f), r * 0.12f);
            // Rim light: a thin crescent on the upper-right edge.
            Fill((x, y) => Mathf.Max(sdf(x, y), -sdf(x + r * 0.07f, y + r * 0.07f)),
                new Color(0.86f, 0.95f, 1f, 0.55f), 2.5f);
            if (gloss > 0f)
            {
                float gx = cx - rx * 0.38f, gy = cy + ry * 0.46f;
                Fill((x, y) => EllipseSdf(x, y, gx, gy, rx * 0.30f, ry * 0.16f),
                    (x, y) => new Color(1f, 1f, 1f, gloss * Mathf.Lerp(0.15f, 0.8f, Mathf.Clamp01((y - gy) / (ry * 0.16f) * 0.5f + 0.5f))), 1.5f);
                Circle(cx - rx * 0.08f, cy + ry * 0.62f, r * 0.05f, new Color(1f, 1f, 1f, 0.85f * gloss));
            }
        }

        /// <summary>Shaded round body — every character is built from these.</summary>
        public void Blob(float cx, float cy, float r, Color fill, float outline = 7f) =>
            Volume((x, y) => CircleSdf(x, y, cx, cy, r), cx, cy, r, r, fill, outline);

        public void OutlinedEllipse(float cx, float cy, float rx, float ry, Color fill, float outline = 6f) =>
            Volume((x, y) => EllipseSdf(x, y, cx, cy, rx, ry), cx, cy, rx, ry, fill, outline, shadow: false,
                gloss: Mathf.Min(rx, ry) > 14f ? 0.7f : 0f);

        /// <summary>Radial glow halo (bullets, cores): bright centre fading to transparent at <paramref name="r"/>.</summary>
        public void Glow(float cx, float cy, float r, Color c, float power = 2f) =>
            Fill((x, y) => CircleSdf(x, y, cx, cy, r), (x, y) =>
            {
                float t = 1f - Mathf.Clamp01(Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / r);
                return new Color(c.r, c.g, c.b, c.a * Mathf.Pow(t, power));
            }, 1f);

        /// <summary>
        /// Big glossy chibi eyes with a coloured iris gradient and two catchlights, blush, and a
        /// mouth. Enemies (<paramref name="determined"/>) get slanted brows and a fanged pout.
        /// </summary>
        public void Face(float cx, float cy, float faceRadius, bool determined = false, Color? iris = null)
        {
            Color irisColor = iris ?? (determined ? new Color(0.85f, 0.25f, 0.42f) : new Color(0.36f, 0.42f, 0.95f));
            float spacing = faceRadius * 0.40f;
            float ey = cy + faceRadius * 0.06f;
            float size = faceRadius * 0.27f;
            for (int s = -1; s <= 1; s += 2)
            {
                float ex = cx + s * spacing;
                // Blush first, under everything.
                Fill((x, y) => EllipseSdf(x, y, ex + s * size * 0.45f, ey - size * 1.35f, size * 0.62f, size * 0.34f),
                    new Color(1f, 0.42f, 0.58f, 0.42f), size * 0.35f);
                Ellipse(ex, ey, size * 0.80f + 3f, size + 3f, Outline);
                float top = ey + size, bottom = ey - size;
                Fill((x, y) => EllipseSdf(x, y, ex, ey, size * 0.80f, size), (x, y) =>
                {
                    float t = Mathf.Clamp01((y - bottom) / (top - bottom));
                    return Color.Lerp(Light(irisColor, 0.25f), Outline, Mathf.Pow(t, 0.8f));
                });
                Ellipse(ex, ey + size * 0.12f, size * 0.42f, size * 0.52f, new Color(0.10f, 0.05f, 0.16f, 0.9f)); // pupil
                Circle(ex - size * 0.30f, ey + size * 0.40f, size * 0.34f, Color.white);
                Circle(ex + size * 0.28f, ey - size * 0.34f, size * 0.15f, new Color(1f, 1f, 1f, 0.95f));
                if (determined)
                    Fill((x, y) =>
                    {
                        float bx = ex + s * size * 0.1f, by = ey + size * 1.35f;
                        float dx = x - bx, dy = y - by;
                        float rot = s * 0.42f;
                        float rx = dx * Mathf.Cos(rot) + dy * Mathf.Sin(rot);
                        float ry = -dx * Mathf.Sin(rot) + dy * Mathf.Cos(rot);
                        float w = size * 0.8f, h = size * 0.17f;
                        return new Vector2(Mathf.Max(Mathf.Abs(rx) - w, 0f), Mathf.Max(Mathf.Abs(ry) - h, 0f)).magnitude - h * 0.6f;
                    }, Outline);
            }

            float my = cy - faceRadius * 0.24f;
            if (determined)
            {
                // Pout: small dark arc plus one white fang.
                Fill((x, y) => Mathf.Max(EllipseSdf(x, y, cx, my - size * 0.15f, size * 0.42f, size * 0.30f), -(y - (my - size * 0.15f))), Outline);
                Triangle(new Vector2(cx + size * 0.05f, my - size * 0.14f), new Vector2(cx + size * 0.28f, my - size * 0.14f),
                    new Vector2(cx + size * 0.17f, my - size * 0.36f), Color.white);
            }
            else
            {
                // Open smile with a pink tongue.
                Fill((x, y) => Mathf.Max(EllipseSdf(x, y, cx, my, size * 0.38f, size * 0.34f), y - my), Outline);
                Fill((x, y) => Mathf.Max(EllipseSdf(x, y, cx, my - size * 0.20f, size * 0.22f, size * 0.12f),
                    EllipseSdf(x, y, cx, my, size * 0.30f, size * 0.27f)), new Color(1f, 0.45f, 0.55f));
            }
        }

        /// <summary>Composites another painter, scaled and centred on (cx, cy), with bilinear sampling.</summary>
        public void Blit(Painter src, float cx, float cy, float scale)
        {
            float halfW = src._w * scale * 0.5f, halfH = src._h * scale * 0.5f;
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - halfW)), x1 = Mathf.Min(_w - 1, Mathf.CeilToInt(cx + halfW));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - halfH)), y1 = Mathf.Min(_h - 1, Mathf.CeilToInt(cy + halfH));
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float sx = (x + 0.5f - (cx - halfW)) / scale - 0.5f, sy = (y + 0.5f - (cy - halfH)) / scale - 0.5f;
                int ix = Mathf.FloorToInt(sx), iy = Mathf.FloorToInt(sy);
                float fx = sx - ix, fy = sy - iy;
                float r = 0f, g = 0f, b = 0f, a = 0f;
                for (int k = 0; k < 4; k++)
                {
                    int tx = ix + (k & 1), ty = iy + (k >> 1);
                    if (tx < 0 || ty < 0 || tx >= src._w || ty >= src._h) continue;
                    float w = ((k & 1) == 1 ? fx : 1f - fx) * ((k >> 1) == 1 ? fy : 1f - fy);
                    Color c = src._px[ty * src._w + tx];
                    r += c.r * c.a * w; g += c.g * c.a * w; b += c.b * c.a * w; a += c.a * w;
                }
                if (a <= 0.001f) continue;
                Blend(x, y, new Color(r / a, g / a, b / a, 1f), a);
            }
        }

        // ---------------------------------------------------------------- Fast dots and tileable noise

        /// <summary>Bounded-box soft dot with optional halo; wraps across edges so tiles stay seamless.</summary>
        public void Dot(float cx, float cy, float r, Color c, float glow = 0f)
        {
            for (int wy = -1; wy <= 1; wy++)
            for (int wx = -1; wx <= 1; wx++)
            {
                float ox = cx + wx * _w, oy = cy + wy * _h;
                float reach = r + glow + 2f;
                if (ox + reach < 0 || ox - reach > _w || oy + reach < 0 || oy - reach > _h) continue;
                int x0 = Mathf.Max(0, Mathf.FloorToInt(ox - reach)), x1 = Mathf.Min(_w - 1, Mathf.CeilToInt(ox + reach));
                int y0 = Mathf.Max(0, Mathf.FloorToInt(oy - reach)), y1 = Mathf.Min(_h - 1, Mathf.CeilToInt(oy + reach));
                for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - ox) * (x + 0.5f - ox) + (y + 0.5f - oy) * (y + 0.5f - oy));
                    float a = Mathf.Clamp01(r + 0.5f - d);
                    if (glow > 0f) a = Mathf.Max(a, 0.55f * Mathf.Pow(1f - Mathf.Clamp01(d / (r + glow)), 2.2f));
                    if (a > 0f) Blend(x, y, c, a);
                }
            }
        }

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 144269504;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xFFFFFF) / 16777215f;
            }
        }

        /// <summary>Value noise that repeats every <paramref name="px"/>×<paramref name="py"/> cells.</summary>
        public static float TileNoise(float x, float y, int px, int py, int seed)
        {
            int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
            float fx = x - ix, fy = y - iy;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            int x0 = ((ix % px) + px) % px, x1 = (x0 + 1) % px;
            int y0 = ((iy % py) + py) % py, y1 = (y0 + 1) % py;
            float a = Mathf.Lerp(Hash(x0, y0, seed), Hash(x1, y0, seed), fx);
            float b = Mathf.Lerp(Hash(x0, y1, seed), Hash(x1, y1, seed), fx);
            return Mathf.Lerp(a, b, fy);
        }

        /// <summary>Tileable fractal noise in [0,1] over a canvas of <paramref name="w"/>×<paramref name="h"/> px.</summary>
        public static float TileFbm(float x, float y, int w, int h, int baseCells, int octaves, int seed)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            int cx = baseCells, cy = Mathf.Max(1, baseCells * h / w);
            for (int o = 0; o < octaves; o++)
            {
                sum += amp * TileNoise(x / w * cx, y / h * cy, cx, cy, seed + o * 31);
                norm += amp;
                amp *= 0.5f;
                cx *= 2;
                cy *= 2;
            }
            return sum / norm;
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
            // Characters are drawn at ~2× their on-screen size; mips keep the downscale smooth.
            importer.mipmapEnabled = _w >= 128 && _h >= 128 && _w <= 256;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }
    }

    /// <summary>Recipes for every placeholder sprite (colours from art-bible §2.1).</summary>
    public static partial class ArtRecipes
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

        /// <summary>The player: Pıtır the bunny pilot riding the candy starfighter (reads as "ship" at a glance).</summary>
        public static Painter Bunny() => Ship(BunnyPilot());

        // ---------------------------------------------------------------- Brand (studio intro, app icon)

        /// <summary>
        /// Ehtiyars Game studio emblem: a round badge with a cheerful chibi "ehtiyar" (old man) —
        /// flat cap, round glasses, a big white moustache and beard. Shown by the studio intro.
        /// </summary>
        public static Painter StudioEmblem()
        {
            var p = new Painter(512, 512);
            Color skin = Hex(0xFFD7B5), cap = Hex(0x6B4FA8), beard = Hex(0xFBF7FF);
            // Badge: honey rim, deep plum field, soft inner glow.
            p.Glow(256, 256, 250, WithAlpha(Honey, 0.35f), 2.5f);
            p.Circle(256, 256, 232, Painter.Outline);
            p.Circle(256, 256, 222, Honey);
            p.Circle(256, 256, 204, Painter.Outline);
            p.Circle(256, 256, 198, Hex(0x3B2A5E));
            p.Glow(256, 300, 190, WithAlpha(Lilac, 0.35f), 1.6f);
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2f / 12f;
                p.Star(256 + Mathf.Cos(a) * 213, 256 + Mathf.Sin(a) * 213, 7f, 3f, Hex(0xFFF1C2));
            }
            // Ears and head.
            p.Blob(150, 250, 26, skin, 6f);
            p.Blob(362, 250, 26, skin, 6f);
            p.Blob(256, 250, 112, skin, 7f);
            // Beard: fluffy cloud under the chin.
            p.Volume((x, y) => Mathf.Min(Mathf.Min(Painter.CircleSdf(x, y, 256, 150, 70), Painter.CircleSdf(x, y, 200, 172, 46)),
                    Mathf.Min(Painter.CircleSdf(x, y, 312, 172, 46), Painter.CircleSdf(x, y, 256, 118, 48))),
                256, 150, 110, 80, beard, 6f, shadow: false, gloss: 0.3f);
            // Flat cap (kasket) with a brim.
            p.Volume((x, y) => Mathf.Max(Painter.EllipseSdf(x, y, 250, 322, 108, 58), 292 - y), 250, 330, 108, 50, cap, 7f, shadow: false, gloss: 0.6f);
            p.Volume((x, y) => Painter.EllipseSdf(x, y, 272, 294, 84, 15), 272, 294, 84, 15, Painter.Deep(cap, 0.2f), 6f, shadow: false, gloss: 0f);
            p.Circle(250, 376, 11, Painter.Outline);
            p.Circle(250, 376, 7, Honey);
            // Eyes behind round glasses.
            for (int s = -1; s <= 1; s += 2)
            {
                float ex = 256 + s * 46;
                p.Fill((x, y) => Mathf.Abs(Painter.CircleSdf(x, y, ex, 232, 30)) - 5f, Painter.Outline);
                p.Circle(ex, 232, 25, WithAlpha(Sky, 0.35f));
                p.Fill((x, y) => Mathf.Max(Painter.EllipseSdf(x, y, ex, 226, 14, 10), 226 - y), Painter.Outline); // happy closed eye
                p.Fill((x, y) => Painter.EllipseSdf(x, y, ex + s * 30, 196, 18, 10), new Color(1f, 0.45f, 0.55f, 0.45f), 6f);
                p.Circle(ex - 10, 244, 5f, new Color(1f, 1f, 1f, 0.8f));
            }
            p.RoundedRect(236, 228, 276, 236, 4, Painter.Outline); // glasses bridge
            // Nose and the big moustache.
            p.Blob(256, 196, 20, Hex(0xFFB897), 5f);
            for (int s = -1; s <= 1; s += 2)
                p.Volume((x, y) => Painter.RotEllipseSdf(x, y, 256 + s * 40, 170, 46, 20, s * 0.32f), 256 + s * 40, 170, 46, 20, beard, 6f, shadow: false, gloss: 0.2f);
            return p;
        }

        /// <summary>Launcher icon: the bunny starfighter on a candy-sky tile with sparkles.</summary>
        public static Painter AppIcon()
        {
            var p = new Painter(512, 512);
            p.Fill((x, y) => Painter.RoundRectSdf(x, y, 0, 0, 512, 512, 96), (x, y) =>
            {
                float t = y / 512f;
                return Color.Lerp(Hex(0x5B3C99), Hex(0xFF8FC2), t * t * 0.9f + 0.1f * t);
            }, 1.5f);
            p.Glow(256, 230, 230, WithAlpha(Hex(0xFFF1C2), 0.55f), 1.8f);
            var rng = new System.Random(7);
            for (int i = 0; i < 18; i++)
            {
                float x = 40 + (float)rng.NextDouble() * 432, y = 40 + (float)rng.NextDouble() * 432;
                if (Mathf.Abs(x - 256) < 150 && Mathf.Abs(y - 240) < 150) continue;
                p.Star(x, y, 6f + (float)rng.NextDouble() * 8f, 2.5f, new Color(1f, 0.96f, 0.85f, 0.9f), 4);
            }
            p.Blit(Ship(BunnyPilot()), 256, 246, 1.38f);
            return p;
        }

        /// <summary>
        /// The shared candy starfighter with a pilot portrait composited into the cockpit. Every
        /// Hangar character flies the same hull, so the silhouette stays learnable. The heart emblem
        /// sits on the hitbox (the root pivot is moved onto it in the scene).
        /// </summary>
        public static Painter Ship(Painter pilot, Color? wingColor = null)
        {
            var p = new Painter(256, 256);
            Color hull = Hex(0xFFF1F7), wing = wingColor ?? Mint, trim = HotPink;
            p.Fill((x, y) => Painter.EllipseSdf(x, y, 128, 84, 112, 60), new Color(0.08f, 0.03f, 0.14f, 0.25f), 20f);
            // Wings, swept back.
            p.Volume((x, y) => Painter.RotEllipseSdf(x, y, 62, 74, 58, 26, 0.38f), 62, 74, 58, 26, wing, 6f, shadow: false);
            p.Volume((x, y) => Painter.RotEllipseSdf(x, y, 194, 74, 58, 26, -0.38f), 194, 74, 58, 26, wing, 6f, shadow: false);
            p.Volume((x, y) => Painter.CircleSdf(x, y, 22, 52, 12), 22, 52, 12, 12, trim, 5f, shadow: false, gloss: 0f); // wingtip lights
            p.Volume((x, y) => Painter.CircleSdf(x, y, 234, 52, 12), 234, 52, 12, 12, trim, 5f, shadow: false, gloss: 0f);
            // Twin engines.
            for (int s = -1; s <= 1; s += 2)
            {
                float ex = 128 + s * 30;
                p.Volume((x, y) => Painter.RoundRectSdf(x, y, ex - 15, 14, ex + 15, 58, 10), ex, 36, 15, 22, Hex(0x9C8FD6), 6f, shadow: false, gloss: 0f);
                p.Ellipse(ex, 18, 11, 6, Hex(0x3FE0D0));
            }
            // Hull and emblem.
            p.Volume((x, y) => Painter.EllipseSdf(x, y, 128, 84, 50, 62), 128, 84, 50, 62, hull, 7f, shadow: false);
            p.Heart(128, 66, 16, Painter.Outline);
            p.Heart(128, 67, 12, trim);
            p.Blit(pilot, 128, 152, 0.64f);
            return p;
        }

        /// <summary>Pıtır's portrait: bunny with flight goggles (Hangar icon and cockpit).</summary>
        public static Painter BunnyPilot()
        {
            var p = new Painter(256, 256);
            p.Volume((x, y) => Painter.RotEllipseSdf(x, y, 92, 186, 26, 56, 0.18f), 92, 186, 26, 56, Color.white, 7f, shadow: false, gloss: 0.4f);
            p.Volume((x, y) => Painter.RotEllipseSdf(x, y, 164, 186, 26, 56, -0.18f), 164, 186, 26, 56, Color.white, 7f, shadow: false, gloss: 0.4f);
            p.Fill((x, y) => Painter.RotEllipseSdf(x, y, 93, 186, 12, 40, 0.18f), Pink);
            p.Fill((x, y) => Painter.RotEllipseSdf(x, y, 163, 186, 12, 40, -0.18f), Pink);
            p.Blob(128, 104, 80, Hex(0xFFF8FC));
            p.RoundedRect(52, 140, 204, 158, 9, Painter.Outline);
            p.Circle(96, 150, 20, Painter.Outline);
            p.Circle(160, 150, 20, Painter.Outline);
            p.Circle(96, 150, 14, WithAlpha(Sky, 0.9f));
            p.Circle(160, 150, 14, WithAlpha(Sky, 0.9f));
            p.Circle(91, 155, 4.5f, Color.white);
            p.Circle(155, 155, 4.5f, Color.white);
            p.Face(128, 94, 74);
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
            Painter pilot = BunnyPilot();
            pilot.Star(206, 222, 30, 13, Painter.Outline);
            pilot.Star(206, 222, 23, 10, Honey);
            return pilot;
        }

        public static Painter ChickEnemy(bool crown)
        {
            var p = new Painter(256, 256);
            Color body = Chick, wing = Hex(0xFFC93C), beak = Hex(0xFF9F43);
            // Feet and flapping wings behind the body.
            for (int s = -1; s <= 1; s += 2)
            {
                float fx = 128 + s * 34;
                p.Volume((x, y) => Painter.EllipseSdf(x, y, fx, 30, 20, 11), fx, 30, 20, 11, beak, 5f, shadow: false, gloss: 0f);
                float wx = 128 + s * 86;
                p.Volume((x, y) => Painter.RotEllipseSdf(x, y, wx, 124, 22, 40, s * -0.55f), wx, 124, 22, 40, wing, 6f, shadow: false, gloss: 0.5f);
            }
            p.Volume((x, y) => Painter.RotEllipseSdf(x, y, 118, 216, 10, 22, 0.35f), 118, 216, 10, 22, wing, 5f, shadow: false, gloss: 0f); // tuft
            p.Volume((x, y) => Painter.RotEllipseSdf(x, y, 140, 212, 9, 18, -0.45f), 140, 212, 9, 18, wing, 5f, shadow: false, gloss: 0f);
            p.Blob(128, 118, 86, body);
            p.Face(128, 128, 86, determined: true);
            p.Volume((x, y) => Painter.EllipseSdf(x, y, 128, 88, 19, 11), 128, 88, 19, 11, beak, 4f, shadow: false, gloss: 0.5f);
            if (crown)
            {
                p.Fill((x, y) => Crown(x, y, 128, 206, 44), Painter.Outline);
                p.Volume((x, y) => Crown(x, y, 128, 206, 38), 128, 216, 38, 22, Honey, 0f, shadow: false, gloss: 0.6f);
                p.Circle(128, 214, 6, Coral);
                p.Circle(104, 212, 4, Sky);
                p.Circle(152, 212, 4, Sky);
            }
            return p;
        }

        /// <summary>Three-point crown, base centred at (cx, baseY), half width w.</summary>
        private static float Crown(float x, float y, float cx, float baseY, float w)
        {
            float h = w * 0.9f;
            float band = Painter.RoundRectSdf(x, y, cx - w, baseY, cx + w, baseY + h * 0.45f, 4f);
            float spikes = float.MaxValue;
            for (int i = -1; i <= 1; i++)
            {
                float tx = cx + i * w * 0.72f, ty = baseY + h * (i == 0 ? 1.05f : 0.9f);
                spikes = Mathf.Min(spikes, Painter.CircleSdf(x, y, tx, ty, w * 0.17f));
                // Spike body: a narrow triangle-ish ellipse from band to tip.
                spikes = Mathf.Min(spikes, Painter.EllipseSdf(x, y, tx, baseY + h * 0.45f, w * 0.30f, (ty - baseY - h * 0.45f)));
            }
            return Mathf.Min(band, spikes);
        }

        public static Painter JellyBear(bool crown)
        {
            var p = new Painter(256, 256);
            Color jelly = WithAlpha(Lilac, 0.94f);
            p.Blob(66, 196, 28, jelly);
            p.Blob(190, 196, 28, jelly);
            p.Circle(66, 196, 13, WithAlpha(Pink, 0.6f));
            p.Circle(190, 196, 13, WithAlpha(Pink, 0.6f));
            p.Blob(128, 116, 92, jelly);
            p.Glow(128, 70, 40, WithAlpha(HotPink, 0.55f)); // sugar heart glowing inside the gummy
            p.Heart(128, 66, 21, WithAlpha(HotPink, 0.9f));
            p.Face(128, 132, 92, determined: true, iris: Hex(0x7B3FE4));
            // Belly paws.
            p.Volume((x, y) => Painter.EllipseSdf(x, y, 72, 44, 22, 16), 72, 44, 22, 16, jelly, 5f, shadow: false, gloss: 0f);
            p.Volume((x, y) => Painter.EllipseSdf(x, y, 184, 44, 22, 16), 184, 44, 22, 16, jelly, 5f, shadow: false, gloss: 0f);
            if (crown)
            {
                p.Fill((x, y) => Crown(x, y, 128, 206, 42), Painter.Outline);
                p.Volume((x, y) => Crown(x, y, 128, 206, 36), 128, 216, 36, 22, Honey, 0f, shadow: false, gloss: 0.6f);
                p.Circle(128, 214, 6, HotPink);
            }
            return p;
        }

        public static Painter CookieRobot(bool big)
        {
            var p = new Painter(256, 256);
            Color metal = Hex(0xB8C4E0);
            p.RoundedRect(123, 196, 133, 236, 4, Painter.Outline); // antenna
            p.Volume((x, y) => Painter.CircleSdf(x, y, 128, 238, 12), 128, 238, 12, 12, Coral, 5f, shadow: false);
            p.Glow(128, 238, 26, WithAlpha(Coral, 0.5f));
            // Side bolts / ear speakers.
            p.Volume((x, y) => Painter.RoundRectSdf(x, y, 26, 96, 50, 146, 8), 38, 121, 12, 25, metal, 6f, shadow: false, gloss: 0.5f);
            p.Volume((x, y) => Painter.RoundRectSdf(x, y, 206, 96, 230, 146, 8), 218, 121, 12, 25, metal, 6f, shadow: false, gloss: 0.5f);
            p.Blob(128, 118, 90, Cookie);
            var rng = new System.Random(7);
            for (int i = 0; i < (big ? 11 : 7); i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float d = 38f + (float)rng.NextDouble() * 40f;
                float x = 128 + Mathf.Cos(a) * d, y = 118 + Mathf.Sin(a) * d;
                if (Mathf.Abs(y - 124) < 30 && Mathf.Abs(x - 128) < 64) continue; // keep the face clear
                float r = 7f + (float)rng.NextDouble() * 4f;
                p.Volume((px, py) => Painter.CircleSdf(px, py, x, y, r), x, y, r, r, Choc, 0f, shadow: false, gloss: 0.4f);
            }
            // Robot visor band over the eyes.
            p.Fill((x, y) => Painter.RoundRectSdf(x, y, 50, 104, 206, 160, 26), WithAlpha(Painter.Outline, 0.95f));
            p.Fill((x, y) => Painter.RoundRectSdf(x, y, 56, 110, 200, 154, 22), (x, y) =>
                Color.Lerp(Hex(0x2B1E4A), Hex(0x4A3A7A), (y - 110) / 44f));
            p.Face(128, 124, 88, determined: true, iris: Hex(0xFF5E7E));
            if (big)
                p.Star(128, 64, 16, 7, Honey);
            return p;
        }

        public static Painter IceCreamTower()
        {
            var p = new Painter(256, 256);
            // Waffle cone.
            p.Fill((x, y) => Mathf.Max(Mathf.Abs(x - 128) - (y - 4f) * 0.45f, Mathf.Max(y - 80f, 4f - y)) - 6f, Painter.Outline);
            p.Fill((x, y) => Mathf.Max(Mathf.Abs(x - 128) - (y - 4f) * 0.45f, Mathf.Max(y - 80f, 4f - y)), (x, y) =>
            {
                float gx = Mathf.Repeat(x + y, 18f), gy = Mathf.Repeat(x - y, 18f);
                bool line = gx < 2.5f || gy < 2.5f;
                return line ? Hex(0xB9783F) : Color.Lerp(Hex(0xD99A55), Hex(0xF2C27E), (y - 8f) / 64f);
            });
            p.Blob(128, 96, 46, Mint);
            p.Blob(128, 196, 42, Pink);
            p.Blob(128, 146, 54, Cream);
            p.Volume((x, y) => Painter.CircleSdf(x, y, 128, 240, 11), 128, 240, 11, 11, Coral, 4f, shadow: false); // cherry
            p.Face(128, 146, 54, determined: true);
            return p;
        }

        public static Painter GumBalloon()
        {
            var p = new Painter(256, 256);
            // String.
            p.Fill((x, y) => Mathf.Abs(x - (128 + Mathf.Sin(y * 0.12f) * 6f)) - 2.5f + Mathf.Max(0f, y - 44f) * 10f + Mathf.Max(0f, 6f - y) * 10f, Painter.Outline);
            p.Triangle(new Vector2(116, 30), new Vector2(140, 30), new Vector2(128, 46), Painter.Outline, 4f);
            p.Triangle(new Vector2(118, 32), new Vector2(138, 32), new Vector2(128, 44), Hex(0xFF7AC2));
            p.Blob(128, 136, 92, Hex(0xFF7AC2));
            p.Face(128, 136, 92, iris: Hex(0xB02E7A));
            return p;
        }

        public static Painter QueenHen()
        {
            var p = new Painter(256, 256);
            Color feather = Hex(0xF1E9FF);
            p.Volume((x, y) => Painter.RotEllipseSdf(x, y, 36, 104, 22, 46, 0.45f), 36, 104, 22, 46, feather, 6f, shadow: false);
            p.Volume((x, y) => Painter.RotEllipseSdf(x, y, 220, 104, 22, 46, -0.45f), 220, 104, 22, 46, feather, 6f, shadow: false);
            p.Blob(128, 114, 94, Color.white);
            p.Face(128, 124, 94, determined: true, iris: Hex(0xE0335E));
            p.Volume((x, y) => Painter.EllipseSdf(x, y, 128, 80, 22, 13), 128, 80, 22, 13, Hex(0xFF9F43), 4f, shadow: false);
            p.Fill((x, y) => Crown(x, y, 128, 196, 54), Painter.Outline);
            p.Volume((x, y) => Crown(x, y, 128, 196, 47), 128, 210, 47, 28, Honey, 0f, shadow: false, gloss: 0.7f);
            p.Diamond(128, 208, 9, HotPink);
            p.Circle(96, 205, 5, Sky);
            p.Circle(160, 205, 5, Sky);
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

        /// <summary>
        /// Enemy fire (art-bible §3 readability): warm glow halo, a thick dark ring that separates it
        /// from anything behind, a hot gradient body and a white core. Player shots never use this
        /// ring or warm colours, so "red with a dark ring" always means danger.
        /// </summary>
        public static Painter EnemyBullet(Color body)
        {
            var p = new Painter(64, 64);
            p.Glow(32, 32, 31, WithAlpha(body, 0.85f), 1.4f);
            p.Circle(32, 32, 19, Painter.Outline);
            p.Circle(32, 32, 16.5f, WithAlpha(Color.white, 0.9f)); // thin bright rim inside the dark ring
            p.Fill((x, y) => Painter.CircleSdf(x, y, 32, 32, 14.5f), (x, y) =>
                Color.Lerp(Color.Lerp(body, new Color(1f, 0.9f, 0.3f), 0.35f), body, Mathf.Clamp01(Mathf.Sqrt((x - 32) * (x - 32) + (y - 32) * (y - 32)) / 14.5f)));
            p.Circle(32, 32, 6.5f, Color.white);
            return p;
        }

        /// <summary>Player orb shot: soft cool glow, no dark ring, slightly translucent.</summary>
        public static Painter PlayerOrb(Color c)
        {
            var p = new Painter(64, 64);
            p.Glow(32, 32, 30, WithAlpha(c, 0.5f), 2f);
            p.Fill((x, y) => Painter.CircleSdf(x, y, 32, 32, 13f), (x, y) =>
                new Color(Mathf.Lerp(1f, c.r, 0.6f), Mathf.Lerp(1f, c.g, 0.6f), Mathf.Lerp(1f, c.b, 0.6f), 0.85f));
            return p;
        }

        public static Painter Feather(bool giant)
        {
            var p = new Painter(64, 64);
            float w = giant ? 15f : 10f;
            p.Glow(32, 32, 31, WithAlpha(Mint, giant ? 0.7f : 0.55f), 1.8f);
            // Energy feather: pointed ellipse, white core, soft mint edge (no outline: player shots stay light).
            p.Fill((x, y) => Painter.EllipseSdf(x, y, 32, 32, w, 28f) + Mathf.Max(0f, y - 44f) * 0.25f,
                (x, y) => Color.Lerp(Hex(0x7FFFE0), Color.white, Mathf.Clamp01(1f - Mathf.Abs(x - 32f) / w)));
            p.Fill((x, y) => Mathf.Abs(x - 32f) - 1.4f + Mathf.Max(0f, Mathf.Abs(y - 30f) - 20f), WithAlpha(Hex(0x3FC9B0), 0.8f));
            return p;
        }

        public static Painter MiniStar(Color c)
        {
            var p = new Painter(64, 64);
            p.Glow(32, 32, 31, WithAlpha(c, 0.55f), 1.8f);
            p.Star(32, 32, 26, 12, Painter.Outline);
            p.Fill((x, y) =>
            {
                float dx = x - 32, dy = y - 32, ang = Mathf.Atan2(dy, dx) + Mathf.PI / 2f, seg = Mathf.PI * 2f / 5f;
                float t = Mathf.Abs(Mathf.Repeat(ang, seg) - seg * 0.5f) / (seg * 0.5f);
                return Mathf.Sqrt(dx * dx + dy * dy) - Mathf.Lerp(21f, 9.5f, t);
            }, (x, y) => Color.Lerp(c, Painter.Light(c, 0.6f), Mathf.Clamp01((y - 14f) / 36f)));
            p.Circle(27, 37, 3.5f, new Color(1f, 1f, 1f, 0.9f));
            return p;
        }

        public static Painter SoftCircle(Color c, int size = 64)
        {
            var p = new Painter(size, size);
            p.Circle(size * 0.5f, size * 0.5f, size * 0.46f, c);
            return p;
        }

        /// <summary>
        /// XP gem: chunky faceted crystal with a dark outline AND a white rim — no background star
        /// has either, so gems read as "pick me up" at a glance.
        /// </summary>
        public static Painter GemSprite(Color c)
        {
            var p = new Painter(64, 64);
            p.Glow(32, 32, 31, WithAlpha(c, 0.55f), 1.8f);
            p.Diamond(32, 32, 27, Painter.Outline);
            p.Diamond(32, 32, 23, Color.white);
            p.Fill((x, y) => (Mathf.Abs(x - 32) + Mathf.Abs(y - 32) - 20f) * 0.707f, (x, y) =>
            {
                bool up = y >= 32, left = x <= 32;
                float k = up && left ? 0.55f : up ? 0.25f : left ? 0f : -0.35f;
                return k >= 0f ? Painter.Light(c, k) : Painter.Deep(c, -k);
            });
            p.Diamond(26, 38, 5, new Color(1f, 1f, 1f, 0.9f));
            return p;
        }

        public static Painter Coin()
        {
            var p = new Painter(64, 64);
            p.Glow(32, 32, 31, WithAlpha(Honey, 0.45f), 2f);
            p.Circle(32, 32, 26, Painter.Outline);
            p.Fill((x, y) => Painter.CircleSdf(x, y, 32, 32, 22), (x, y) =>
                Color.Lerp(Hex(0xF0A020), Hex(0xFFE27A), Mathf.Clamp01((y - 10f) / 44f)));
            p.Circle(32, 32, 15, Hex(0xE8961A));
            p.Star(32, 31, 11, 5, Hex(0xFFF1B0));
            p.Ellipse(24, 42, 6, 3.5f, new Color(1f, 1f, 1f, 0.8f));
            return p;
        }

        public static Painter HeartPickup()
        {
            var p = new Painter(64, 64);
            p.Glow(32, 32, 31, WithAlpha(HotPink, 0.45f), 2f);
            p.Heart(32, 30, 25, Painter.Outline);
            p.Heart(32, 31, 20, HotPink);
            p.Heart(29, 35, 11, Painter.Light(HotPink, 0.35f));
            p.Ellipse(22, 40, 5, 3.5f, new Color(1f, 1f, 1f, 0.85f));
            return p;
        }

        public static Painter MagnetPickup()
        {
            var p = new Painter(64, 64);
            p.Glow(32, 32, 31, WithAlpha(Coral, 0.4f), 2f);
            // Horseshoe opening downward, white pole tips.
            Painter.Sdf shoe = (x, y) =>
            {
                const float cx = 32f, cy = 34f, R = 14f, t = 6.5f, legs = 16f;
                if (y >= cy) return Mathf.Abs(Painter.CircleSdf(x, y, cx, cy, R)) - t;
                return Mathf.Max(Mathf.Abs(Mathf.Abs(x - cx) - R) - t, (cy - legs) - y);
            };
            p.Fill((x, y) => shoe(x, y) - 4f, Painter.Outline);
            p.Fill((x, y) => shoe(x, y), (x, y) =>
                y < 24f ? Color.white : Color.Lerp(Coral, Painter.Light(Coral, 0.45f), Mathf.Clamp01((y - 20f) / 30f)));
            return p;
        }

        public static Painter BombPickup()
        {
            var p = new Painter(64, 64);
            p.RoundedRect(28, 44, 36, 58, 3, Painter.Outline);
            p.Glow(33, 58, 9, Honey, 1.2f);
            p.Star(33, 58, 7, 3, Color.white);
            p.Volume((x, y) => Painter.CircleSdf(x, y, 32, 28, 20), 32, 28, 20, 20, Hex(0x6B5B95), 5f, shadow: false);
            p.Heart(32, 26, 7, WithAlpha(HotPink, 0.9f));
            return p;
        }

        public static Painter Egg()
        {
            var p = new Painter(64, 64);
            p.Volume((x, y) => Painter.EllipseSdf(x, y, 32, 30, 18, 24), 32, 30, 18, 24, Hex(0xFFF6E0), 4f, shadow: false);
            p.Fill((x, y) => Mathf.Max(Painter.EllipseSdf(x, y, 32, 30, 18, 24), Mathf.Abs(y - 24f - Mathf.Abs(Mathf.Repeat(x, 8f) - 4f) * 1.2f) - 2f), WithAlpha(Coral, 0.8f));
            return p;
        }

        public static Painter Bubble()
        {
            var p = new Painter(64, 64);
            p.Glow(32, 32, 31, WithAlpha(Hex(0x9FE8FF), 0.4f), 2f);
            p.Fill((x, y) => Painter.CircleSdf(x, y, 32, 32, 24), (x, y) =>
            {
                float d = Mathf.Sqrt((x - 32) * (x - 32) + (y - 32) * (y - 32)) / 24f;
                return new Color(0.6f, 0.9f, 1f, Mathf.Lerp(0.2f, 0.85f, d * d)); // transparent middle, iridescent rim
            });
            p.Fill((x, y) => Mathf.Abs(Painter.CircleSdf(x, y, 32, 32, 24)) - 1.5f, WithAlpha(Painter.Outline, 0.8f));
            p.Ellipse(24, 42, 7, 4, new Color(1f, 1f, 1f, 0.9f));
            p.Circle(41, 23, 2.5f, new Color(1f, 1f, 1f, 0.8f));
            return p;
        }

        // Background (art-bible §6): deep candy-space gradient, soft nebula, two star layers, props.

        public static Painter SkyGradient()
        {
            var p = new Painter(8, 512);
            Color top = Hex(0x120C26), mid = Hex(0x2A1B4E), low = Hex(0x47285F);
            p.Fill((x, y) => -1f, (x, y) =>
            {
                float t = y / 512f;
                return t > 0.5f ? Color.Lerp(mid, top, (t - 0.5f) * 2f) : Color.Lerp(low, mid, t * 2f);
            });
            return p;
        }

        /// <summary>Tileable (both axes) pink/violet/teal cloud layer with transparent gaps.</summary>
        public static Painter Nebula()
        {
            const int w = 256, h = 512;
            var p = new Painter(w, h);
            Color pink = Hex(0xE0569E), violet = Hex(0x7A4BD6), teal = Hex(0x2FB5B0);
            p.Fill((x, y) => -1f, (x, y) =>
            {
                float n = Painter.TileFbm(x, y, w, h, 3, 5, 11);
                float hue = Painter.TileFbm(x, y, w, h, 2, 3, 77);
                float a = Mathf.Clamp01((n - 0.34f) * 2.3f);
                a = Mathf.Pow(a, 1.4f) * 0.85f;
                Color c = hue < 0.5f ? Color.Lerp(teal, violet, hue * 2f) : Color.Lerp(violet, pink, (hue - 0.5f) * 2f);
                return new Color(c.r, c.g, c.b, a);
            });
            return p;
        }

        public static Painter StarField(bool near)
        {
            const int w = 512, h = 1024;
            var p = new Painter(w, h);
            var rng = new System.Random(near ? 91 : 17);
            int count = near ? 45 : 380;
            Color[] tints = { Color.white, Hex(0xFFF3D6), Hex(0xE6DDFF) }; // neutral: coloured stars read as pickups
            for (int i = 0; i < count; i++)
            {
                float x = (float)rng.NextDouble() * w, y = (float)rng.NextDouble() * h;
                float big = (float)rng.NextDouble();
                Color c = tints[rng.Next(tints.Length)];
                if (near)
                {
                    float r = 1.2f + big * 1.6f;
                    c.a = 0.7f;
                    p.Dot(x, y, r, c, r * 3f);
                    if (big > 0.93f) // rare, faint four-point sparkles
                    {
                        for (int k = 0; k < 2; k++)
                        {
                            float len = r * 7f;
                            for (int t = -1; t <= 1; t += 2)
                            for (float s = 1f; s < len; s += 1f)
                                p.Dot(k == 0 ? x + t * s : x, k == 0 ? y : y + t * s, 0.6f * (1f - s / len) + 0.2f,
                                    new Color(c.r, c.g, c.b, 0.8f * (1f - s / len)));
                        }
                    }
                }
                else
                {
                    c.a = 0.2f + big * 0.4f;
                    p.Dot(x, y, 0.6f + big * 1.1f, c, big > 0.85f ? 3f : 0f);
                }
            }
            return p;
        }

        public static Painter RingPlanet()
        {
            var p = new Painter(256, 256);
            Painter.Sdf ring = (x, y) => Mathf.Abs(Painter.RotEllipseSdf(x, y, 128, 128, 104, 26, 0.35f)) - 6f;
            // Back half of the ring, planet, then the front half over it.
            p.Fill((x, y) => Mathf.Max(ring(x, y), -(y - 128f - (x - 128f) * 0.36f)), WithAlpha(Hex(0xFFC6E0), 0.85f));
            p.Volume((x, y) => Painter.CircleSdf(x, y, 128, 128, 62), 128, 128, 62, 62, Hex(0x8C7CF0), 5f, shadow: false);
            p.Fill((x, y) => Mathf.Max(Painter.CircleSdf(x, y, 128, 128, 62), Mathf.Abs(y - 128f - (x - 128f) * 0.2f + 22f) - 7f), WithAlpha(Hex(0xB4A8FF), 0.8f));
            p.Fill((x, y) => Mathf.Max(ring(x, y), y - 128f - (x - 128f) * 0.36f), WithAlpha(Hex(0xFFC6E0), 0.9f));
            return p;
        }

        public static Painter DonutPlanet()
        {
            var p = new Painter(256, 256);
            Painter.Sdf donut = (x, y) => Mathf.Abs(Painter.CircleSdf(x, y, 128, 128, 72)) - 44f;
            p.Volume(donut, 128, 128, 116, 116, Hex(0xE8B07A), 6f, shadow: false);
            p.Fill((x, y) =>
            {
                float r = Mathf.Sqrt((x - 128f) * (x - 128f) + (y - 128f) * (y - 128f));
                float edge = 100f + 6f * Mathf.Sin(Mathf.Atan2(y - 128f, x - 128f) * 9f);
                return Mathf.Max(r - edge, 40f - r);
            }, (x, y) => Color.Lerp(Hex(0xFF7ABD), Hex(0xFFB0D8), Mathf.Clamp01((y - 40f) / 180f))); // wavy pink icing
            var rng = new System.Random(3);
            for (int i = 0; i < 26; i++)
            {
                float a = (float)rng.NextDouble() * 6.283f, d = 50f + (float)rng.NextDouble() * 44f;
                float x = 128 + Mathf.Cos(a) * d, y = 128 + Mathf.Sin(a) * d;
                Color c = new[] { Mint, Cream, Sky, Color.white }[rng.Next(4)];
                p.Fill((px, py) => Painter.RotEllipseSdf(px, py, x, y, 7f, 2.5f, a * 3f), c);
            }
            return p;
        }

        public static Painter CandyMoon()
        {
            var p = new Painter(256, 256);
            p.Volume((x, y) => Painter.CircleSdf(x, y, 128, 128, 96), 128, 128, 96, 96, Hex(0x7FE0C4), 6f, shadow: false);
            // Swirl stripes like a hard candy.
            p.Fill((x, y) => Mathf.Max(Painter.CircleSdf(x, y, 128, 128, 96),
                Mathf.Sin(Mathf.Atan2(y - 128f, x - 128f) * 3f + Mathf.Sqrt((x - 128f) * (x - 128f) + (y - 128f) * (y - 128f)) * 0.06f) * 20f - 6f),
                WithAlpha(Color.white, 0.55f));
            p.Ellipse(92, 170, 28, 14, WithAlpha(Color.white, 0.6f));
            return p;
        }

        /// <summary>Weapon/passive icon: shaded badge plus a symbol. Evolutions and fusions get gold.</summary>
        public static Painter Icon(string id)
        {
            bool legendary = id is "supernova_omelette" or "galaxy_vortex" or "gum_rings" or "storm_cat" or "prism_beam" or "feather_storm"
                or "rainbow_storm" or "cosmic_breakfast" or "candy_shield_galaxy";
            (Color badge, Painter symbol, float scale) = id switch
            {
                "feather_blaster" => (Sky, Feather(false), 1.45f),
                "rainbow_storm" => (Honey, Feather(true), 1.45f),
                "egg_mortar" => (Coral, Egg(), 1.35f),
                "supernova_omelette" or "cosmic_breakfast" => (Honey, Egg(), 1.35f),
                "star_boomerang" => (Lilac, MiniStar(Honey), 1.45f),
                "galaxy_vortex" => (Honey, MiniStar(Lilac), 1.45f),
                "bubble_orbit" => (Pink, Bubble(), 1.35f),
                "gum_rings" or "candy_shield_galaxy" => (Honey, Bubble(), 1.35f),
                "spark_cat" => (Sky, Cat(), 0.72f),
                "storm_cat" => (Honey, Cat(), 0.72f),
                "prism_beam" => (Honey, GemSprite(Sky), 1.4f),
                "feather_storm" => (Honey, Feather(true), 1.2f),
                "crystal_glasses" => (Lilac, GemSprite(Sky), 1.3f),
                "hot_pan" => (Coral, Coin(), 1.3f),
                "moon_dust" => (Hex(0x6B5B95), MiniStar(Lilac), 1.4f),
                "stretchy_gum" => (Mint, Bubble(), 1.3f),
                "battery_collar" => (Mint, MiniStar(Honey), 1.4f),
                "carrot_shield" => (Mint, HeartPickup(), 1.3f),
                "magnet_ears" => (Sky, MagnetPickup(), 1.3f),
                "lucky_clover" => (Leaf, GemSprite(Leaf), 1.3f),
                _ => (Lilac, MiniStar(Color.white), 1.4f)
            };

            var p = new Painter(128, 128);
            p.Volume((x, y) => Painter.RoundRectSdf(x, y, 10, 10, 118, 118, 28), 64, 64, 54, 54, badge, 6f, shadow: false, gloss: 0.6f);
            // Soft dark inset behind the symbol so it pops on any badge colour.
            p.Fill((x, y) => Painter.CircleSdf(x, y, 64, 62, 40), new Color(0.23f, 0.16f, 0.31f, 0.35f), 16f);
            p.Blit(symbol, 64, 62, scale);
            if (legendary)
            {
                p.Star(104, 104, 17, 7, Painter.Outline);
                p.Star(104, 104, 13, 5.5f, Color.white);
            }
            return p;
        }

        /// <summary>9-slice screen-edge glow (Şeker Hücumu): clear centre, soft bright rim.</summary>
        public static Painter EdgeGlow()
        {
            var p = new Painter(128, 128);
            p.Fill((x, y) => -1f, (x, y) =>
            {
                float d = Mathf.Min(Mathf.Min(x, 128f - x), Mathf.Min(y, 128f - y)); // px from the nearest edge
                float a = Mathf.Pow(1f - Mathf.Clamp01(d / 56f), 2.2f);
                return new Color(1f, 1f, 1f, a);
            });
            return p;
        }

        /// <summary>
        /// 9-slice button face: grey-scale so Image.color tints it — top-lit vertical gradient, a
        /// glossy upper band, a soft inner rim and a darker bottom lip. Reads as a pressable candy.
        /// </summary>
        public static Painter ButtonFace()
        {
            var p = new Painter(128, 128);
            Painter.Sdf body = (x, y) => Painter.RoundRectSdf(x, y, 2, 2, 126, 126, 40);
            p.Fill((x, y) => body(x, y), (x, y) =>
            {
                float t = y / 128f;
                float v = Mathf.Lerp(0.8f, 1f, Mathf.SmoothStep(0f, 1f, t));
                return new Color(v, v, v, 1f);
            });
            // Glossy band across the top half.
            p.Fill((x, y) => Mathf.Max(Painter.RoundRectSdf(x, y, 12, 70, 116, 118, 30), -body(x, y) - 8f),
                (x, y) => new Color(1f, 1f, 1f, Mathf.Lerp(0.05f, 0.35f, (y - 70f) / 48f)), 3f);
            // Inner rim: thin light line just inside the edge.
            p.Fill((x, y) => Mathf.Abs(body(x, y) + 5f) - 1.5f, new Color(1f, 1f, 1f, 0.35f), 1.2f);
            // Darker bottom lip.
            p.Fill((x, y) => Mathf.Max(body(x, y), y - 22f), new Color(0.55f, 0.5f, 0.62f, 0.35f), 6f);
            return p;
        }

        /// <summary>9-slice base for every UI panel and button.</summary>
        /// <summary>Ar-Ge menu tile icon: a honey gear with a mint "up" arrow — permanent upgrades.</summary>
        public static Painter ResearchIcon()
        {
            var p = new Painter(256, 256);
            Color gear = Honey;
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                float cx = 118 + Mathf.Cos(a) * 88, cy = 118 + Mathf.Sin(a) * 88;
                p.Fill((x, y) => Painter.RotEllipseSdf(x, y, cx, cy, 26, 20, a) - 5f, Painter.Outline);
                p.Fill((x, y) => Painter.RotEllipseSdf(x, y, cx, cy, 26, 20, a), Painter.Deep(gear, 0.15f));
            }
            p.Blob(118, 118, 72, gear, 7f);
            p.Circle(118, 118, 30, Painter.Outline);
            p.Circle(118, 118, 22, Hex(0x3B2A5E));
            // Up arrow badge, bottom-right.
            p.Circle(192, 70, 50, Painter.Outline);
            p.Circle(192, 70, 43, Mint);
            p.Triangle(new Vector2(192, 104), new Vector2(166, 74), new Vector2(218, 74), Color.white);
            p.RoundedRect(182, 40, 202, 78, 4, Color.white);
            return p;
        }

        /// <summary>HUD bar track: a dark plum pill with an outline and an inner shadow (sliced 26 px ends).</summary>
        public static Painter BarTrack()
        {
            var p = new Painter(128, 56);
            Painter.Sdf body = (x, y) => Painter.RoundRectSdf(x, y, 2, 2, 126, 54, 26);
            p.Fill((x, y) => body(x, y), Painter.Outline);
            p.Fill((x, y) => body(x, y) + 5f, (x, y) =>
            {
                float t = y / 56f;
                return Color.Lerp(new Color(0.12f, 0.07f, 0.2f, 1f), new Color(0.22f, 0.15f, 0.33f, 1f), t);
            });
            // Inner shadow along the top edge so the fill reads as sitting inside a groove.
            p.Fill((x, y) => Mathf.Max(body(x, y) + 5f, 40f - y), new Color(0f, 0f, 0f, 0.35f), 6f);
            return p;
        }

        /// <summary>HUD bar fill: a white pill with a soft vertical gradient and a glossy top band; tinted by the Image colour.</summary>
        public static Painter BarFill()
        {
            var p = new Painter(128, 44);
            Painter.Sdf body = (x, y) => Painter.RoundRectSdf(x, y, 1, 1, 127, 43, 21);
            p.Fill((x, y) => body(x, y), (x, y) =>
            {
                float t = y / 44f;
                float v = Mathf.Lerp(0.72f, 1f, Mathf.SmoothStep(0f, 1f, t));
                return new Color(v, v, v, 1f);
            });
            p.Fill((x, y) => Mathf.Max(Painter.RoundRectSdf(x, y, 10, 25, 118, 38, 7), -body(x, y) - 4f),
                new Color(1f, 1f, 1f, 0.55f), 2f);
            p.Fill((x, y) => Mathf.Max(body(x, y), -(body(x, y) + 3f)), new Color(0f, 0f, 0f, 0.18f), 1.5f); // crisp edge
            return p;
        }

        public static Painter RoundedPanel()
        {
            var p = new Painter(96, 96);
            p.RoundedRect(0, 0, 96, 96, 30, Color.white);
            return p;
        }
    }
}
