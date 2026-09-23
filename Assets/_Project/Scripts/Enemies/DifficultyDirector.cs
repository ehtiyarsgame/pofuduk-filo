using System;

namespace PofudukFilo.Enemies
{
    /// <summary>
    /// Invisible dynamic difficulty (game-concept.md §4.4), pure C# so it can be unit-tested.
    ///   • 2+ player hits in the last 60 s  → spawn budget −10 % (floor −25 %).
    ///   • 90 s without a hit              → spawn budget +5 %  (cap  +15 %).
    /// Adjustments are rate-limited so one bad moment does not swing the whole run.
    /// </summary>
    public sealed class DifficultyDirector
    {
        public const float StepDown = 0.10f;
        public const float StepUp = 0.05f;
        public const float Min = 0.75f;
        public const float Max = 1.15f;
        public const float HitWindowSeconds = 60f;
        public const int HitsToEase = 2;
        public const float CleanSecondsToHarden = 90f;
        public const float AdjustCooldownSeconds = 20f;

        private readonly float[] _hitTimes = new float[8];
        private int _hitCount;
        private int _hitHead;
        private float _lastHitTime;
        private float _lastAdjustTime = float.NegativeInfinity;

        public float Multiplier { get; private set; } = 1f;

        public void Reset(float now)
        {
            Multiplier = 1f;
            _hitCount = 0;
            _hitHead = 0;
            _lastHitTime = now;
            _lastAdjustTime = float.NegativeInfinity;
        }

        public void RegisterPlayerHit(float now)
        {
            _hitTimes[_hitHead] = now;
            _hitHead = (_hitHead + 1) % _hitTimes.Length;
            _hitCount = Math.Min(_hitCount + 1, _hitTimes.Length);
            _lastHitTime = now;
        }

        /// <summary>Call periodically (e.g. once per second) with the run clock.</summary>
        public void Evaluate(float now)
        {
            if (now - _lastAdjustTime < AdjustCooldownSeconds) return;

            if (HitsWithin(now, HitWindowSeconds) >= HitsToEase && Multiplier > Min)
            {
                Multiplier = Math.Max(Min, Multiplier - StepDown);
                _lastAdjustTime = now;
            }
            else if (now - _lastHitTime >= CleanSecondsToHarden && Multiplier < Max)
            {
                Multiplier = Math.Min(Max, Multiplier + StepUp);
                _lastAdjustTime = now;
            }
        }

        private int HitsWithin(float now, float window)
        {
            int count = 0;
            for (int i = 0; i < _hitCount; i++)
                if (now - _hitTimes[i] <= window) count++;
            return count;
        }
    }
}
