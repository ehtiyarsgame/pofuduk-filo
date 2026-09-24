using UnityEngine;

namespace PofudukFilo.EditorTools
{
    /// <summary>
    /// Content added with the rewarded-ad unlocks (design/gdd/ad-rewards.md): the Fish Missile and Yarn Ball
    /// projectiles, the Pengu and Kuzu pilots, the "watch an ad" TV icon and the daily gift box.
    /// </summary>
    public static partial class ArtRecipes
    {
        /// <summary>Homing fish pointing up (the sprite is rotated along its velocity). Sharks are bigger and grey-blue.</summary>
        public static Painter FishSprite(bool shark)
        {
            var p = new Painter(128, 128);
            Color body = shark ? Hex(0x8FA8D8) : Hex(0x7FD8FF), fin = shark ? Hex(0x6A7FB8) : Sky;
            // Tail fin at the bottom, side fins, then the body.
            p.Glow(64, 20, 18, WithAlpha(Mint, 0.7f)); // bubbly exhaust
            for (int s = -1; s <= 1; s += 2) // forked tail
                p.Volume((x, y) => Painter.RotEllipseSdf(x, y, 64 + s * 12, 26, 8, 18, s * 0.55f), 64 + s * 12, 26, 8, 18, fin, 4f, shadow: false, gloss: 0f);
            for (int s = -1; s <= 1; s += 2)
                p.Volume((x, y) => Painter.RotEllipseSdf(x, y, 64 + s * 26, 62, 14, 8, s * 0.6f), 64 + s * 26, 62, 14, 8, fin, 4f, shadow: false, gloss: 0f);
            if (shark)
                p.Triangle(new Vector2(64, 78), new Vector2(50, 58), new Vector2(78, 58), Painter.Outline, 4f);
            p.Volume((x, y) => Painter.EllipseSdf(x, y, 64, 70, 24, 40), 64, 70, 24, 40, body, 5f, shadow: false, gloss: 0.8f);
            p.Fill((x, y) => Painter.EllipseSdf(x, y, 64, 62, 13, 26), WithAlpha(Color.white, 0.35f), 4f); // belly
            for (int s = -1; s <= 1; s += 2)
            {
                p.Circle(64 + s * 11, 90, 6.5f, Painter.Outline);
                p.Circle(64 + s * 11 - 1.5f, 92, 2.5f, Color.white);
            }
            if (shark)
                for (int i = -1; i <= 1; i++)
                    p.Triangle(new Vector2(64 + i * 7 - 3, 104), new Vector2(64 + i * 7 + 3, 104), new Vector2(64 + i * 7, 98), Color.white);
            return p;
        }

        /// <summary>Yarn ball: a pink sphere wound with curved strands and a loose end.</summary>
        public static Painter YarnSprite()
        {
            var p = new Painter(128, 128);
            Color yarn = Hex(0xFF8FC2);
            p.Volume((x, y) => Painter.CircleSdf(x, y, 64, 64, 54), 64, 64, 54, 54, yarn, 6f, shadow: false, gloss: 0.5f);
            for (int i = 0; i < 5; i++)
            {
                float a = i * 0.63f - 1.2f;
                float cx = 64 + Mathf.Cos(a) * 70, cy = 64 + Mathf.Sin(a) * 70;
                p.Fill((x, y) => Mathf.Max(Painter.CircleSdf(x, y, 64, 64, 50),
                    Mathf.Abs(Painter.CircleSdf(x, y, cx, cy, 74)) - 2.2f), Painter.Deep(yarn, 0.35f));
            }
            // Loose end curling off the lower right.
            p.Fill((x, y) => Mathf.Max(Mathf.Abs(Painter.CircleSdf(x, y, 100, 30, 16)) - 3f, -Painter.CircleSdf(x, y, 64, 64, 52)), Painter.Deep(yarn, 0.35f));
            return p;
        }

        /// <summary>Pengu: a round penguin with a white face mask and an orange beak.</summary>
        public static Painter PenguinPilot()
        {
            var p = new Painter(256, 256);
            Color back = Hex(0x3C4B7A);
            p.Blob(128, 112, 90, back);
            p.Fill((x, y) => Mathf.Min(Painter.CircleSdf(x, y, 98, 120, 50), Painter.CircleSdf(x, y, 158, 120, 50)), Hex(0xFFF8F0), 2f);
            p.Fill((x, y) => Painter.EllipseSdf(x, y, 128, 84, 64, 44), Hex(0xFFF8F0), 2f);
            p.Face(128, 116, 86);
            p.OutlinedEllipse(128, 96, 18, 9, Hex(0xFFA23A), 4f);
            // Little aviator scarf.
            p.RoundedRect(62, 26, 194, 44, 8, Painter.Outline);
            p.RoundedRect(66, 30, 190, 40, 5, Coral);
            return p;
        }

        /// <summary>Kuzu: a fluffy lamb — cloud wool around a cream face.</summary>
        public static Painter LambPilot()
        {
            var p = new Painter(256, 256);
            Color wool = Hex(0xFFFDF7);
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI * 2f / 10f;
                p.Blob(128 + Mathf.Cos(a) * 80, 118 + Mathf.Sin(a) * 76, 34, wool, 6f);
            }
            p.Ear(56, 124, 22, 44, -60, Hex(0xFFD8C2), Pink);
            p.Ear(200, 124, 22, 44, 60, Hex(0xFFD8C2), Pink);
            p.Blob(128, 110, 76, Hex(0xFFE9D6));
            p.Face(128, 108, 76);
            for (int i = -1; i <= 1; i++) p.Blob(128 + i * 30, 186, 22, wool, 5f); // fringe
            return p;
        }

        /// <summary>"Watch an ad" icon: a candy TV with a play triangle.</summary>
        public static Painter IconAd()
        {
            var p = new Painter(128, 128);
            p.RoundedRect(40, 96, 48, 122, 3, Painter.Outline); // antennae
            p.RoundedRect(80, 96, 88, 122, 3, Painter.Outline);
            p.Volume((x, y) => Painter.RoundRectSdf(x, y, 10, 18, 118, 100, 18), 64, 59, 54, 41, Mint, 6f, shadow: false, gloss: 0.5f);
            p.Fill((x, y) => Painter.RoundRectSdf(x, y, 22, 30, 106, 88, 10), Hex(0x2A1D45));
            p.Triangle(new Vector2(52, 44), new Vector2(52, 76), new Vector2(82, 60), Color.white);
            return p;
        }

        /// <summary>Daily gift: a pink box with a honey ribbon and bow.</summary>
        public static Painter GiftBox()
        {
            var p = new Painter(256, 256);
            p.Volume((x, y) => Painter.RoundRectSdf(x, y, 44, 24, 212, 150, 16), 128, 87, 84, 63, HotPink, 7f, shadow: false);
            p.Volume((x, y) => Painter.RoundRectSdf(x, y, 32, 140, 224, 184, 14), 128, 162, 96, 22, Pink, 7f, shadow: false, gloss: 0.6f);
            p.Fill((x, y) => Painter.RoundRectSdf(x, y, 112, 24, 144, 184, 4), Honey);
            for (int s = -1; s <= 1; s += 2)
                p.Volume((x, y) => Painter.RotEllipseSdf(x, y, 128 + s * 36, 206, 36, 20, s * -0.4f), 128 + s * 36, 206, 36, 20, Honey, 6f, shadow: false, gloss: 0.4f);
            p.Blob(128, 196, 16, Honey, 6f);
            p.Star(206, 222, 18, 7, Color.white, 4);
            return p;
        }
    }
}
