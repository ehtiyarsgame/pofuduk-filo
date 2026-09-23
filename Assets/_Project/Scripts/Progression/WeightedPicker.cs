using System;
using Random = Unity.Mathematics.Random;

namespace PofudukFilo.Progression
{
    /// <summary>
    /// Weighted sampling without replacement. Pure and seedable so draft odds can be unit-tested
    /// and runs can be replayed.
    /// </summary>
    public static class WeightedPicker
    {
        /// <summary>
        /// Fills <paramref name="results"/> with distinct indices into <paramref name="weights"/>.
        /// Entries with weight ≤ 0 are never picked.
        /// </summary>
        /// <returns>Number of indices written (fewer than requested if not enough candidates).</returns>
        public static int PickDistinct(ReadOnlySpan<float> weights, Span<int> results, ref Random random)
        {
            Span<bool> taken = weights.Length <= 256 ? stackalloc bool[weights.Length] : new bool[weights.Length];
            int written = 0;

            while (written < results.Length)
            {
                float total = 0f;
                for (int i = 0; i < weights.Length; i++)
                    if (!taken[i] && weights[i] > 0f) total += weights[i];
                if (total <= 0f) break;

                float roll = random.NextFloat(total);
                int chosen = -1;
                for (int i = 0; i < weights.Length; i++)
                {
                    if (taken[i] || weights[i] <= 0f) continue;
                    chosen = i;
                    roll -= weights[i];
                    if (roll < 0f) break;
                }

                taken[chosen] = true;
                results[written++] = chosen;
            }

            return written;
        }
    }
}
