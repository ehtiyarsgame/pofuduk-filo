using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using PofudukFilo.EditorTools;
using UnityEngine;

// Usage: dotnet run -- <out.png> [scale] [recipe ...]
// Renders ArtRecipes (all public static Painter methods, or the named ones) onto one sheet.
internal static class Program
{
    private static int Main(string[] args)
    {
        string outPath = args.Length > 0 ? args[0] : "sheet.png";
        int cell = args.Length > 1 ? int.Parse(args[1]) : 160;
        var names = new HashSet<string>(args[2..]);

        var painters = new List<Painter>();
        foreach (MethodInfo m in typeof(ArtRecipes).GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (m.ReturnType != typeof(Painter)) continue;
            if (names.Count > 0 && !names.Contains(m.Name)) continue;
            foreach (object[] a in Variants(m))
            {
                try { painters.Add((Painter)m.Invoke(null, a)); }
                catch (Exception e) { Console.Error.WriteLine($"{m.Name}: {e.InnerException?.Message ?? e.Message}"); }
            }
        }

        int cols = Math.Min(6, Math.Max(1, painters.Count));
        int rows = (painters.Count + cols - 1) / cols;
        int W = cols * cell, H = rows * cell;
        var img = new float[W * H * 3];
        Color bg = ArtRecipes.Hex(0x2A2146);
        for (int i = 0; i < W * H; i++) { img[i * 3] = bg.r; img[i * 3 + 1] = bg.g; img[i * 3 + 2] = bg.b; }

        for (int k = 0; k < painters.Count; k++)
        {
            Painter p = painters[k];
            int ox = (k % cols) * cell, oy = (k / cols) * cell;
            float scale = Math.Min((cell - 8f) / p.Width, (cell - 8f) / p.Height);
            int dw = (int)(p.Width * scale), dh = (int)(p.Height * scale);
            int px0 = ox + (cell - dw) / 2, py0 = oy + (cell - dh) / 2;
            for (int y = 0; y < dh; y++)
            for (int x = 0; x < dw; x++)
            {
                // Box-filter downsample (what mipmapped sampling roughly does on device).
                float r = 0, g = 0, b = 0, a = 0; int n = 0;
                int sx0 = (int)(x / scale), sx1 = Math.Max(sx0 + 1, (int)((x + 1) / scale));
                int sy0 = (int)(y / scale), sy1 = Math.Max(sy0 + 1, (int)((y + 1) / scale));
                for (int sy = sy0; sy < sy1 && sy < p.Height; sy++)
                for (int sx = sx0; sx < sx1 && sx < p.Width; sx++)
                {
                    Color c = p.Pixels[(p.Height - 1 - sy) * p.Width + sx]; // flip: Texture2D rows are bottom-up
                    r += c.r * c.a; g += c.g * c.a; b += c.b * c.a; a += c.a; n++;
                }
                if (n == 0 || a <= 0) continue;
                float al = a / n;
                int i = ((py0 + y) * W + px0 + x) * 3;
                img[i] = img[i] * (1 - al) + r / n;
                img[i + 1] = img[i + 1] * (1 - al) + g / n;
                img[i + 2] = img[i + 2] * (1 - al) + b / n;
            }
        }
        WritePng(outPath, W, H, img);
        Console.WriteLine($"{painters.Count} sprites -> {outPath}");
        return 0;
    }

    private static IEnumerable<object[]> Variants(MethodInfo m)
    {
        ParameterInfo[] ps = m.GetParameters();
        if (ps.Length == 0) { yield return Array.Empty<object>(); yield break; }
        if (ps.Length == 1 && ps[0].ParameterType == typeof(bool)) { yield return new object[] { false }; yield return new object[] { true }; yield break; }
        if (ps.Length == 1 && ps[0].ParameterType == typeof(string))
        {
            foreach (string id in new[] { "feather_blaster", "egg_mortar", "star_boomerang", "bubble_orbit", "spark_cat", "prism_beam", "feather_storm",
                         "rainbow_storm", "galaxy_vortex", "crystal_glasses", "hot_pan", "moon_dust", "stretchy_gum",
                         "battery_collar", "carrot_shield", "lucky_clover", "sharp_claws", "double_barrel", "tiger_eye", "glass_cannon",
                         "last_stand", "sugar_heart", "golden_paw", "wise_owl", "turtle_shell", "fish_missile", "shark_swarm", "yarn_ball", "cosmic_yarn" })
                yield return new object[] { id };
            yield break;
        }
        if (ps[0].ParameterType == typeof(Color))
        {
            var rest = new object[ps.Length];
            rest[0] = ArtRecipes.HotPink;
            for (int i = 1; i < ps.Length; i++) rest[i] = ps[i].HasDefaultValue ? ps[i].DefaultValue : Type.Missing;
            yield return rest;
        }
    }

    private static void WritePng(string path, int w, int h, float[] rgb)
    {
        var raw = new byte[h * (w * 3 + 1)];
        for (int y = 0; y < h; y++)
        {
            raw[y * (w * 3 + 1)] = 0;
            for (int x = 0; x < w * 3; x++)
                raw[y * (w * 3 + 1) + 1 + x] = (byte)Math.Clamp((int)(Math.Pow(Math.Clamp(rgb[y * w * 3 + x], 0, 1), 1.0) * 255 + 0.5), 0, 255);
        }
        using var fs = File.Create(path);
        fs.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var ihdr = new byte[13];
        BE(ihdr, 0, w); BE(ihdr, 4, h); ihdr[8] = 8; ihdr[9] = 2;
        Chunk(fs, "IHDR", ihdr);
        using (var ms = new MemoryStream())
        {
            using (var z = new ZLibStream(ms, System.IO.Compression.CompressionLevel.Optimal, true)) z.Write(raw);
            Chunk(fs, "IDAT", ms.ToArray());
        }
        Chunk(fs, "IEND", Array.Empty<byte>());
    }

    private static void BE(byte[] b, int o, int v) { b[o] = (byte)(v >> 24); b[o + 1] = (byte)(v >> 16); b[o + 2] = (byte)(v >> 8); b[o + 3] = (byte)v; }

    private static void Chunk(Stream s, string type, byte[] data)
    {
        var len = new byte[4]; BE(len, 0, data.Length); s.Write(len);
        byte[] t = System.Text.Encoding.ASCII.GetBytes(type);
        s.Write(t); s.Write(data);
        uint crc = Crc(t, 0xFFFFFFFF); crc = Crc(data, crc) ^ 0xFFFFFFFF;
        var c = new byte[4]; BE(c, 0, (int)crc); s.Write(c);
    }

    private static uint Crc(byte[] d, uint c)
    {
        foreach (byte b in d)
        {
            c ^= b;
            for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
        }
        return c;
    }
}
