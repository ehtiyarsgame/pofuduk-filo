namespace PofudukFilo.Enemies
{
    /// <summary>
    /// Chooses the next swarm enemy by weight alone — cost is paid afterwards by saving up
    /// (WaveDirector.TickSwarm). Pure so it can be unit-tested.
    /// </summary>
    public static class SwarmPicker
    {
        /// <summary>Index picked by <paramref name="roll01"/> in [0, 1) over the weights; −1 if all are ≤ 0.</summary>
        public static int PickWeighted(float[] weights, float roll01)
        {
            float total = 0f;
            for (int i = 0; i < weights.Length; i++) if (weights[i] > 0f) total += weights[i];
            if (total <= 0f) return -1;
            float roll = roll01 * total;
            int last = -1;
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] <= 0f) continue;
                last = i;
                roll -= weights[i];
                if (roll < 0f) return i;
            }
            return last;
        }
    }
}
