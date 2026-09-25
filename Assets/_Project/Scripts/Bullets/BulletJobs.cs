using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace PofudukFilo.Bullets
{
    /// <summary>One bullet as plain data. Bullets are never GameObjects (architecture.md §4).</summary>
    public struct BulletData
    {
        public float2 Position;
        public float2 Velocity;
        public float Radius;
        public float Damage;
        public float Lifetime;
        /// <summary>Extra enemies this bullet may pass through. 0 = dies on first hit.</summary>
        public int Pierce;
        public int TypeIndex;
        /// <summary>Stable id of the last enemy hit, so a piercing bullet does not hit it again next frame.</summary>
        public int LastHitEnemyId;
        public bool Grazed;
        public bool Alive;
        /// <summary>False for boss special attacks, which bubbles and omelettes must not eat.</summary>
        public bool Absorbable;
        /// <summary>On-hit traits of a player shot (BulletEffect flags, hero-guns.md §3.2).</summary>
        public byte Effects;
        /// <summary>Enemy shot that bursts into three when its fuse (Lifetime) runs out: 1 + the child bullet type, 0 = never.</summary>
        public int SplitInto;
        /// <summary>Explosion radius of a player shot with the Explode trait; 0 = BulletSystem.ExplodeRadius (hero-guns.md §3.3).</summary>
        public float Area;
        /// <summary>Balonbaş (hero-guns.md §3.4): walls and enemies it may still bounce off; each bounce ×1.15 damage.</summary>
        public int Bounces;
        /// <summary>Yıldızpati (hero-guns.md §3.4): turn rate toward the nearest enemy in degrees per second; 0 = straight.</summary>
        public float Homing;
    }

    /// <summary>Collision snapshot of one enemy, written by EnemyManager each frame.</summary>
    public struct EnemyProxy
    {
        public float2 Position;
        public float Radius;
        public int Id;
    }

    public enum BulletHitKind : byte
    {
        Enemy,
        Player,
        Graze
    }

    public struct BulletHit
    {
        public BulletHitKind Kind;
        /// <summary>Index into this frame's enemy proxy array (Kind == Enemy only).</summary>
        public int EnemyIndex;
        public float Damage;
        public float2 Position;
        public byte Effects;
        public int TypeIndex;
        public float Area;
    }

    public static class BulletGrid
    {
        /// <summary>Packs a cell coordinate into a collision-free key (play area is far below ±32k cells).</summary>
        public static int CellKey(int2 cell) => ((cell.x + 32768) << 16) | ((cell.y + 32768) & 0xFFFF);

        public static int2 Cell(float2 position, float invCellSize) => (int2)math.floor(position * invCellSize);
    }

    [BurstCompile]
    public struct MoveBulletsJob : IJobParallelFor
    {
        public NativeArray<BulletData> Bullets;
        public float DeltaTime;
        /// <summary>Despawn bounds: xy = min, zw = max.</summary>
        public float4 Bounds;
        /// <summary>The visible playfield walls bouncing shots reflect off: xy = min, zw = max (w = the HUD panel's edge).</summary>
        public float4 Walls;

        public const float BounceDamageGrowth = 1.15f;

        public void Execute(int index)
        {
            BulletData b = Bullets[index];
            if (!b.Alive) return;

            b.Position += b.Velocity * DeltaTime;
            b.Lifetime -= DeltaTime;

            if (b.Bounces > 0)
            {
                bool hitSide = (b.Position.x < Walls.x && b.Velocity.x < 0f) || (b.Position.x > Walls.z && b.Velocity.x > 0f);
                bool hitTop = b.Position.y > Walls.w && b.Velocity.y > 0f;
                if (hitSide || hitTop)
                {
                    if (hitSide) b.Velocity.x = -b.Velocity.x;
                    if (hitTop) b.Velocity.y = -b.Velocity.y;
                    b.Bounces--;
                    b.Damage *= BounceDamageGrowth;
                    b.Lifetime = math.max(b.Lifetime, 1.2f);
                    b.LastHitEnemyId = -1;
                }
            }

            bool outside = b.Position.x < Bounds.x || b.Position.y < Bounds.y ||
                           b.Position.x > Bounds.z || b.Position.y > Bounds.w;
            if (b.Lifetime <= 0f || outside) b.Alive = false;

            Bullets[index] = b;
        }
    }

    /// <summary>
    /// Inserts each enemy into every grid cell its (radius + max bullet radius) box overlaps,
    /// so a bullet only has to look at its own cell and never sees the same enemy twice.
    /// </summary>
    [BurstCompile]
    public struct BuildEnemyGridJob : IJob
    {
        [ReadOnly] public NativeArray<EnemyProxy> Enemies;
        public int EnemyCount;
        public float InvCellSize;
        public float MaxBulletRadius;
        public NativeParallelMultiHashMap<int, int> Grid;

        public void Execute()
        {
            Grid.Clear();
            for (int i = 0; i < EnemyCount; i++)
            {
                EnemyProxy e = Enemies[i];
                float reach = e.Radius + MaxBulletRadius;
                int2 min = BulletGrid.Cell(e.Position - reach, InvCellSize);
                int2 max = BulletGrid.Cell(e.Position + reach, InvCellSize);
                for (int x = min.x; x <= max.x; x++)
                for (int y = min.y; y <= max.y; y++)
                    Grid.Add(BulletGrid.CellKey(new int2(x, y)), i);
            }
        }
    }

    /// <summary>Homing shots turn toward the nearest visible enemy (Yıldızpati, hero-guns.md §3.4).</summary>
    [BurstCompile]
    public struct HomingJob : IJobParallelFor
    {
        public NativeArray<BulletData> Bullets;
        [ReadOnly] public NativeArray<EnemyProxy> Enemies;
        public int EnemyCount;
        public float DeltaTime;
        public float SearchRadius;

        public void Execute(int index)
        {
            BulletData b = Bullets[index];
            if (!b.Alive || b.Homing <= 0f) return;

            float bestSq = SearchRadius * SearchRadius;
            int best = -1;
            for (int i = 0; i < EnemyCount; i++)
            {
                float d = math.distancesq(Enemies[i].Position, b.Position);
                if (d < bestSq && Enemies[i].Radius > 0f)
                {
                    bestSq = d;
                    best = i;
                }
            }
            if (best < 0) return;

            float speed = math.length(b.Velocity);
            if (speed <= 0f) return;
            float current = math.atan2(b.Velocity.y, b.Velocity.x);
            float2 to = Enemies[best].Position - b.Position;
            float wanted = math.atan2(to.y, to.x);
            float delta = wanted - current;
            delta = math.atan2(math.sin(delta), math.cos(delta)); // wrap to [-π, π]
            float maxTurn = math.radians(b.Homing) * DeltaTime;
            float turned = current + math.clamp(delta, -maxTurn, maxTurn);
            b.Velocity = new float2(math.cos(turned), math.sin(turned)) * speed;
            Bullets[index] = b;
        }
    }

    [BurstCompile]
    public struct PlayerBulletCollisionJob : IJobParallelFor
    {
        public NativeArray<BulletData> Bullets;
        [ReadOnly] public NativeArray<EnemyProxy> Enemies;
        [ReadOnly] public NativeParallelMultiHashMap<int, int> Grid;
        public float InvCellSize;
        public NativeQueue<BulletHit>.ParallelWriter Hits;

        public void Execute(int index)
        {
            BulletData b = Bullets[index];
            if (!b.Alive) return;

            int key = BulletGrid.CellKey(BulletGrid.Cell(b.Position, InvCellSize));
            if (!Grid.TryGetFirstValue(key, out int enemyIndex, out NativeParallelMultiHashMapIterator<int> it))
                return;

            do
            {
                EnemyProxy e = Enemies[enemyIndex];
                if (e.Id == b.LastHitEnemyId) continue;

                float r = b.Radius + e.Radius;
                if (math.distancesq(b.Position, e.Position) > r * r) continue;

                Hits.Enqueue(new BulletHit
                {
                    Kind = BulletHitKind.Enemy,
                    EnemyIndex = enemyIndex,
                    Damage = b.Damage,
                    Position = b.Position,
                    Effects = b.Effects,
                    TypeIndex = b.TypeIndex,
                    Area = b.Area
                });
                b.LastHitEnemyId = e.Id;
                b.Pierce--;
                if (b.Pierce < 0)
                {
                    // A bouncing bubble springs back off the enemy instead of popping (hero-guns.md §3.4).
                    if (b.Bounces > 0)
                    {
                        b.Bounces--;
                        b.Pierce = 0;
                        b.Velocity.y = -b.Velocity.y;
                        b.Damage *= MoveBulletsJob.BounceDamageGrowth;
                        break;
                    }
                    b.Alive = false;
                    break;
                }
            } while (Grid.TryGetNextValue(out enemyIndex, ref it));

            Bullets[index] = b;
        }
    }

    [BurstCompile]
    public struct EnemyBulletCollisionJob : IJobParallelFor
    {
        public NativeArray<BulletData> Bullets;
        public float2 PlayerPosition;
        public float PlayerHitRadius;
        public float PlayerGrazeRadius;
        public NativeQueue<BulletHit>.ParallelWriter Hits;

        public void Execute(int index)
        {
            BulletData b = Bullets[index];
            if (!b.Alive) return;

            float distSq = math.distancesq(b.Position, PlayerPosition);
            float hitR = b.Radius + PlayerHitRadius;
            if (distSq <= hitR * hitR)
            {
                Hits.Enqueue(new BulletHit { Kind = BulletHitKind.Player, EnemyIndex = -1, Damage = b.Damage, Position = b.Position });
                b.Alive = false;
                Bullets[index] = b;
                return;
            }

            float grazeR = b.Radius + PlayerGrazeRadius;
            if (!b.Grazed && distSq <= grazeR * grazeR)
            {
                Hits.Enqueue(new BulletHit { Kind = BulletHitKind.Graze, EnemyIndex = -1, Position = b.Position });
                b.Grazed = true;
                Bullets[index] = b;
            }
        }
    }
}
