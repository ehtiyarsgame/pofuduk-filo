using System.Collections.Generic;
using PofudukFilo.Bullets;
using PofudukFilo.Enemies;
using UnityEngine;

namespace PofudukFilo.Weapons
{
    /// <summary>
    /// 2) Egg Mortar — lobs eggs at enemies; they burst in an area. Lv4+: each burst releases two
    /// mini chicks that dive at the nearest enemies. Evolved (Supernova Omelette): every Nth volley
    /// a golden egg cracks mid-screen, damages everything and turns enemy bullets into fluff
    /// (weapon-system.md §3.2).
    /// </summary>
    public sealed class EggMortar : WeaponBehaviour
    {
        [SerializeField] private Sprite eggSprite;
        [SerializeField] private float flightTime = 0.6f;
        [SerializeField] private float arcHeight = 1.5f;
        [SerializeField] private float targetSearchRadius = 12f;
        [SerializeField] private int chicksFromLevel = 4;
        [SerializeField] private int chickBulletType = -1;
        [SerializeField] private float chickSpeed = 9f;
        [SerializeField] private bool isEvolved;
        [SerializeField] private Color burstColor = new(1f, 0.91f, 0.64f, 0.8f);      // krem sarı
        [SerializeField] private Color supernovaColor = new(1f, 0.96f, 0.88f, 0.35f); // ≤35 % cream flash

        private struct Egg
        {
            public SpriteRenderer Renderer;
            public Vector2 From;
            public Vector2 To;
            public float Age;
            public float Damage;
            public float Radius;
        }

        private readonly List<Egg> _eggs = new(16);

        protected override void Fire(in WeaponLevelStats s)
        {
            int n = Enemies.QueryCircle(Origin, targetSearchRadius, Scratch);
            for (int i = 0; i < Mathf.Max(1, s.projectileCount); i++)
            {
                Vector2 target = n > 0
                    ? (Vector2)Scratch[Random.Range(0, n)].transform.position
                    : Origin + new Vector2(Random.Range(-2f, 2f), 5f);
                Launch(target, s.damage, AreaOf(s));
            }

            if (isEvolved && s.specialEveryN > 0 && ShotCounter % s.specialEveryN == 0)
                Supernova(s);
        }

        private void Launch(Vector2 target, float damage, float radius)
        {
            SpriteRenderer r = Vfx != null && eggSprite != null
                ? Vfx.Sprites.Get(eggSprite, Origin, isEvolved ? 0.55f : 0.4f, Color.white)
                : null;
            _eggs.Add(new Egg { Renderer = r, From = Origin, To = target, Damage = damage, Radius = radius });
        }

        protected override void Update()
        {
            base.Update();

            float dt = Time.deltaTime;
            for (int i = _eggs.Count - 1; i >= 0; i--)
            {
                Egg e = _eggs[i];
                e.Age += dt;
                float t = Mathf.Clamp01(e.Age / flightTime);

                if (e.Renderer != null)
                {
                    Vector2 p = Vector2.Lerp(e.From, e.To, t) + Vector2.up * (Mathf.Sin(t * Mathf.PI) * arcHeight);
                    e.Renderer.transform.position = p;
                    e.Renderer.transform.rotation = Quaternion.Euler(0f, 0f, t * 540f);
                }

                if (t >= 1f)
                {
                    Explode(e);
                    _eggs.RemoveAt(i);
                    continue;
                }
                _eggs[i] = e;
            }
        }

        private void Explode(in Egg e)
        {
            if (e.Renderer != null) Vfx.Sprites.Release(e.Renderer);

            DamageArea(e.To, e.Radius, e.Damage);
            if (Vfx != null)
            {
                Vfx.Pop(e.To, e.Radius, burstColor);
                Vfx.Confetti(e.To, burstColor, 6);
            }

            if (Level >= chicksFromLevel && chickBulletType >= 0) ReleaseChicks(e.To, e.Damage * 0.5f);
        }

        private void ReleaseChicks(Vector2 from, float damage)
        {
            Scratch.Clear();
            for (int i = 0; i < 2; i++)
            {
                Enemy target = Enemies.FindNearest(from, 8f, Scratch);
                Vector2 dir = target != null
                    ? ((Vector2)target.transform.position - from).normalized
                    : Random.insideUnitCircle.normalized;
                if (target != null) Scratch.Add(target);
                BulletSystem.Instance.SpawnPlayerBullet(chickBulletType, from, dir * chickSpeed, RollDamage(damage), 0, 3f);
            }
        }

        private void Supernova(in WeaponLevelStats s)
        {
            Camera cam = Camera.main;
            Vector2 center = cam != null ? (Vector2)cam.transform.position : Origin + Vector2.up * 6f;

            Enemies.DamageAll(RollDamage(s.damage * s.specialDamageMultiplier));
            BulletSystem.Instance.RequestClearEnemyBullets();

            if (Vfx == null) return;
            Vfx.Pop(center, 12f, supernovaColor, 0.6f);
            Vfx.Pop(center, 4f, burstColor, 0.4f);
            Vfx.Confetti(center, burstColor, 40);
        }

        private void OnDestroy()
        {
            // Evolution swaps the weapon object mid-flight; return any eggs to the pool.
            if (Vfx == null) return;
            for (int i = 0; i < _eggs.Count; i++)
                if (_eggs[i].Renderer != null) Vfx.Sprites.Release(_eggs[i].Renderer);
        }
    }
}
