using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace PofudukFilo.Progression
{
    public struct PickupData
    {
        public float2 Position;
        public float2 Velocity;
        public int Value;
        public PickupKind Kind;
        public bool Attracted;
        public bool Alive;
        public float Age;
    }

    public struct PickupCollected
    {
        public PickupKind Kind;
        public int Value;
        public float2 Position;
    }

    /// <summary>
    /// Pop-out, idle drift, magnet attraction and collection for every pickup in one Burst pass
    /// (game-concept.md §3.4). Pickups that drift off the bottom are lost.
    /// </summary>
    [BurstCompile]
    public struct UpdatePickupsJob : IJobParallelFor
    {
        public NativeArray<PickupData> Pickups;
        public float DeltaTime;
        public float2 PlayerPosition;
        public float MagnetRadius;
        public float CollectRadius;
        public bool GlobalMagnet;
        /// <summary>Seconds after which any pickup flies to the ship on its own (0 = never).</summary>
        public float AutoCollectAfter;
        public float DriftSpeed;
        public float AttractAcceleration;
        public float BottomY;
        public NativeQueue<PickupCollected>.ParallelWriter Collected;

        public void Execute(int index)
        {
            PickupData p = Pickups[index];
            if (!p.Alive) return;

            float2 toPlayer = PlayerPosition - p.Position;
            float distSq = math.lengthsq(toPlayer);

            p.Age += DeltaTime;
            if (!p.Attracted && (GlobalMagnet || distSq <= MagnetRadius * MagnetRadius
                                 || (AutoCollectAfter > 0f && p.Age >= AutoCollectAfter)))
                p.Attracted = true;

            if (p.Attracted)
            {
                float dist = math.sqrt(distSq);
                float2 dir = dist > 1e-4f ? toPlayer / dist : float2.zero;
                float speed = math.length(p.Velocity) + AttractAcceleration * DeltaTime;
                p.Velocity = dir * speed;
            }
            else
            {
                // Settle the pop-out velocity, then scroll down with the world.
                p.Velocity = math.lerp(p.Velocity, new float2(0f, -DriftSpeed), math.saturate(DeltaTime * 3f));
            }

            p.Position += p.Velocity * DeltaTime;

            if (math.distancesq(p.Position, PlayerPosition) <= CollectRadius * CollectRadius)
            {
                Collected.Enqueue(new PickupCollected { Kind = p.Kind, Value = p.Value, Position = p.Position });
                p.Alive = false;
            }
            else if (p.Position.y < BottomY)
            {
                p.Alive = false;
            }

            Pickups[index] = p;
        }
    }
}
