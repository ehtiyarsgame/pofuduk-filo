using System;
using PofudukFilo.Core;
using UnityEngine;

namespace PofudukFilo.Progression
{
    /// <summary>
    /// In-run XP and level. Several level-ups in one frame are queued and shown one draft
    /// at a time (game-concept.md §6).
    /// </summary>
    public sealed class XpSystem : MonoBehaviour
    {
        [Tooltip("Meta 'Experience' upgrade, e.g. 0.08 = +8 % XP.")]
        [SerializeField] private float xpBonus;

        public event Action<int> LevelUpQueued; // new level
        public event Action<int, int> XpChanged; // current, required

        public int Level { get; private set; } = 1;
        public int CurrentXp { get; private set; }
        public int RequiredXp => Formulas.XpToNextLevel(Level);
        public int PendingLevelUps { get; private set; }

        public float XpBonus
        {
            get => xpBonus;
            set => xpBonus = value;
        }

        public void ResetRun()
        {
            Level = 1;
            CurrentXp = 0;
            PendingLevelUps = 0;
            XpChanged?.Invoke(CurrentXp, RequiredXp);
        }

        public void AddXp(int amount)
        {
            CurrentXp += Mathf.Max(1, Mathf.RoundToInt(amount * (1f + xpBonus)));

            while (CurrentXp >= RequiredXp)
            {
                CurrentXp -= RequiredXp;
                Level++;
                PendingLevelUps++;
                LevelUpQueued?.Invoke(Level);
            }

            XpChanged?.Invoke(CurrentXp, RequiredXp);
        }

        /// <summary>Called by the draft UI after the player picks a card.</summary>
        public bool ConsumePendingLevelUp()
        {
            if (PendingLevelUps == 0) return false;
            PendingLevelUps--;
            return true;
        }
    }
}
