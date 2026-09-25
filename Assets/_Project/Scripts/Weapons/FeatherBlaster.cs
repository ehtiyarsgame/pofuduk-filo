using PofudukFilo.Bullets;
using UnityEngine;

namespace PofudukFilo.Weapons
{
    /// <summary>
    /// Starting weapon: a fan of feathers straight up. Lv5 fires a giant feather every Nth shot
    /// (weapon-system.md §3.2 — 1) Tüy Blaster). Parallel lanes for Lv2, fan from Lv3.
    /// </summary>
    public sealed class FeatherBlaster : WeaponBehaviour
    {
        [SerializeField] private float laneSpacing = 0.25f;
        [SerializeField] private int giantBulletTypeIndex = -1;
        [SerializeField] private Vector2 muzzleOffset = new(0f, 0.5f);
        [Tooltip("Angle of the first pair of wing feathers from straight up (sideShots).")]
        [SerializeField] private float sideShotAngle = 22f;

        /// <summary>Explosion radius of Explode shots: the level's area × area bonuses; 0 = the default radius.</summary>
        private float ExplodeRadiusOf(in WeaponLevelStats s) => s.area > 0f ? AreaOf(s) : 0f;

        protected override void Fire(in WeaponLevelStats s)
        {
            BulletSystem bullets = BulletSystem.Instance;
            if (bullets == null) return;

            Vector2 origin = (Vector2)transform.position + muzzleOffset;
            float speed = s.projectileSpeed * Stats.SpeedMultiplier;
            float lifetime = s.lifetime * Stats.DurationMultiplier;

            bool special = s.specialEveryN > 0 && ShotCounter % s.specialEveryN == 0;
            int typeIndex = special && giantBulletTypeIndex >= 0 ? giantBulletTypeIndex : Definition.bulletTypeIndex;
            float damageScale = special ? s.specialDamageMultiplier : 1f;

            int count = Mathf.Max(1, s.projectileCount);
            bool fan = s.spreadDegrees > 0f && count > 1;

            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0f : i / (count - 1f) - 0.5f; // -0.5 … 0.5
                Vector2 position = origin;
                Vector2 direction = Vector2.up;

                if (fan)
                {
                    float angle = (90f + t * s.spreadDegrees) * Mathf.Deg2Rad;
                    direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                }
                else
                {
                    position.x += t * laneSpacing * (count - 1);
                }

                bullets.SpawnPlayerBullet(typeIndex, position, direction * speed,
                    RollDamage(s.damage * damageScale), s.pierce, lifetime, s.effects, ExplodeRadiusOf(s));
            }

            // Wing feathers (hero-guns.md §3.3): pairs fanning out from the outer lanes.
            float edge = fan ? 0f : 0.5f * laneSpacing * (count - 1);
            for (int k = 0; k < s.sideShots; k++)
            {
                int side = k % 2 == 0 ? -1 : 1;
                float angle = (90f + side * (sideShotAngle + 10f * (k / 2))) * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                bullets.SpawnPlayerBullet(typeIndex, origin + new Vector2(side * edge, 0f), dir * speed,
                    RollDamage(s.damage * damageScale * 0.8f), s.pierce, lifetime, s.effects, ExplodeRadiusOf(s));
            }
        }
    }
}
