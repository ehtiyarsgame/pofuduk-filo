using System;
using UnityEngine;

namespace PofudukFilo.Enemies
{
    /// <summary>A swarm enemy the director may buy with its threat budget.</summary>
    [Serializable]
    public struct SwarmEntry
    {
        public Enemy prefab;
        [Tooltip("Threat points this enemy costs from the per-second spawn budget.")]
        public float threatCost;
        [Tooltip("Relative pick chance among affordable entries.")]
        public float weight;
    }

    [Serializable]
    public struct FormationEntry
    {
        public Enemy prefab;
        public FormationShape shape;
        public int rows;
        public int columns;
        public float spacing;
        public float weight;
    }

    public enum PhaseKind
    {
        Waves,
        MiniBoss,
        FinalBoss
    }

    /// <summary>One block of the run timeline (game-concept.md §3.2).</summary>
    [Serializable]
    public sealed class RunPhase
    {
        public string label = "Dalga 1";
        public PhaseKind kind = PhaseKind.Waves;
        [Tooltip("Run minute this phase starts at. Phases must be sorted ascending.")]
        public float startMinute;

        [Header("Swarm layer (Vampire Survivors)")]
        public SwarmEntry[] swarm = Array.Empty<SwarmEntry>();
        [Tooltip("Scales Formulas.SpawnBudget for this phase — e.g. 0.3 during a boss fight.")]
        public float budgetScale = 1f;

        [Header("Formation layer (Chicken Invaders)")]
        public FormationEntry[] formations = Array.Empty<FormationEntry>();
        [Tooltip("Seconds between formations; 0 disables the layer for this phase.")]
        public float formationInterval = 20f;

        [Header("Boss")]
        public Enemy bossPrefab;
    }

    /// <summary>
    /// A whole chapter run. Author one asset per chapter under Assets/_Project/Data/Runs/.
    /// Default timeline: 0:00 waves → 3:00 mini-boss → 6:00 mini-boss → 8:00 final boss.
    /// </summary>
    [CreateAssetMenu(menuName = "Pofuduk Filo/Run Definition", fileName = "Run")]
    public sealed class RunDefinition : ScriptableObject
    {
        public int chapterIndex;
        public RunPhase[] phases = Array.Empty<RunPhase>();

        [Tooltip("Spawn-free pause after a boss dies (game-concept.md §4.3: 8–10 s).")]
        public float breatherSeconds = 9f;

        [Tooltip("Cap on formations alive at once so the top of the screen stays readable.")]
        public int maxLiveFormations = 2;

        /// <summary>Index of the phase active at <paramref name="minutes"/>, or −1 before the first.</summary>
        public int PhaseIndexAt(float minutes)
        {
            int index = -1;
            for (int i = 0; i < phases.Length; i++)
            {
                if (phases[i].startMinute <= minutes) index = i;
                else break;
            }
            return index;
        }
    }
}
