using System;
using System.Collections.Generic;
using PofudukFilo.Weapons;
using UnityEngine;

namespace PofudukFilo.Meta
{
    [Serializable]
    public sealed class ConstellationNode
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        [Tooltip("0 Saldırı, 1 Koruma, 2 Şans")]
        public int branch;
        public int stardustCost = 2;
        [Tooltip("Node ids that must be owned first.")]
        public string[] requires = Array.Empty<string>();
        public StatModifier[] modifiers = Array.Empty<StatModifier>();
    }

    /// <summary>
    /// The Constellation board (meta-economy.md §3.3 D): Stardust nodes that bend the rules a
    /// little but noticeably. Branches let the player choose a path.
    /// </summary>
    [CreateAssetMenu(menuName = "Pofuduk Filo/Constellation", fileName = "Constellation")]
    public sealed class ConstellationDefinition : ScriptableObject
    {
        public string[] branchNames = { "Saldırı", "Koruma", "Şans" };
        public ConstellationNode[] nodes = Array.Empty<ConstellationNode>();

        public ConstellationNode Find(string id)
        {
            foreach (ConstellationNode n in nodes)
                if (n.id == id) return n;
            return null;
        }
    }
}
