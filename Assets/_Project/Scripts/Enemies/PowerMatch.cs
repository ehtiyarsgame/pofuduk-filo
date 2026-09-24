using PofudukFilo.Core;

namespace PofudukFilo.Enemies
{
    /// <summary>
    /// Güç Eşleme (Power Match, design/gdd/power-match.md) — Ball Blast's lesson: the targets grow with
    /// the player, so a strong build never turns the late run into a walk. The first
    /// <see cref="CalibrationSeconds"/> of a run measure how long a normal enemy survives on screen;
    /// after that, new spawns get an HP scale that keeps the time-to-kill near that baseline
    /// × <see cref="TargetRatio"/> (slightly faster, so growth is still felt). Pure C#, unit-testable.
    /// </summary>
    public sealed class PowerMatch
    {
        public float CalibrationSeconds = 75f;
        /// <summary>Target TTK = baseline × this. Below 1 lets the player feel stronger than at the start.</summary>
        public float TargetRatio = 0.8f;
        /// <summary>Max log-change of the scale per second (at full error).</summary>
        public float Rate = 0.05f;
        public float MaxScale = 10f; // QA run 28: pinned at 6 from 340 s with TTK still 2.3 s vs a 2.95 s target
        /// <summary>Per-kill weight of the running average (≈ memory of 1/weight kills).</summary>
        public float AverageWeight = 0.04f;
        /// <summary>A floor so one-frame kills (bombs, overlaps) cannot drive the average to zero.</summary>
        public float MinTtk = 0.15f;

        public float Scale { get; private set; } = 1f;
        public float AverageTtk { get; private set; } = -1f;
        public float BaselineTtk { get; private set; } = -1f;

        private float _elapsed;

        public float Elapsed => _elapsed;

        /// <summary>Resume a saved run (run-resume.md).</summary>
        public void Restore(float scale, float average, float baseline, float elapsed)
        {
            Scale = scale < 1f ? 1f : scale > MaxScale ? MaxScale : scale;
            AverageTtk = average;
            BaselineTtk = baseline;
            _elapsed = elapsed;
        }

        public void Reset()
        {
            Scale = 1f;
            AverageTtk = -1f;
            BaselineTtk = -1f;
            _elapsed = 0f;
        }

        /// <summary>Seconds a normal enemy spent on screen before dying.</summary>
        public void RecordKill(float secondsOnScreen)
        {
            float ttk = secondsOnScreen < MinTtk ? MinTtk : secondsOnScreen;
            AverageTtk = AverageTtk < 0f ? ttk : AverageTtk + (ttk - AverageTtk) * AverageWeight;
        }

        public void Tick(float dt)
        {
            _elapsed += dt;
            if (_elapsed < CalibrationSeconds || AverageTtk < 0f) return;
            if (BaselineTtk < 0f) BaselineTtk = AverageTtk;
            Scale = Formulas.PowerMatchStep(Scale, AverageTtk, BaselineTtk * TargetRatio, Rate, dt, MaxScale);
        }
    }
}
