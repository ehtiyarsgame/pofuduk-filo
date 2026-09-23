using System;
using System.Collections.Generic;
using PofudukFilo.Weapons;

namespace PofudukFilo.Meta
{
    /// <summary>
    /// Main-menu economy: wallet, Workshop purchases and applying meta bonuses at run start
    /// (design/gdd/meta-economy.md).
    /// </summary>
    public sealed class MetaProgressionService
    {
        private readonly SaveData _data;
        private readonly Action<SaveData> _persist;

        public event Action WalletChanged;

        public MetaProgressionService(SaveData data, Action<SaveData> persist = null)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _persist = persist ?? SaveService.Save;
        }

        public long Gold => _data.gold;
        public int Stardust => _data.stardust;

        public int GetLevel(MetaUpgradeDefinition upgrade)
        {
            UpgradeLevelEntry entry = Find(upgrade.id);
            return entry?.level ?? 0;
        }

        public bool IsMaxed(MetaUpgradeDefinition upgrade) => GetLevel(upgrade) >= upgrade.maxLevel;

        public bool CanAfford(MetaUpgradeDefinition upgrade) =>
            !IsMaxed(upgrade) && _data.gold >= upgrade.CostForNext(GetLevel(upgrade));

        /// <summary>True if anything in <paramref name="upgrades"/> is affordable — drives the "!" badge on the menu button.</summary>
        public bool AnyAffordable(IReadOnlyList<MetaUpgradeDefinition> upgrades)
        {
            for (int i = 0; i < upgrades.Count; i++)
                if (CanAfford(upgrades[i])) return true;
            return false;
        }

        public bool TryPurchase(MetaUpgradeDefinition upgrade)
        {
            if (!CanAfford(upgrade)) return false;

            int level = GetLevel(upgrade);
            _data.gold -= upgrade.CostForNext(level);

            UpgradeLevelEntry entry = Find(upgrade.id);
            if (entry == null) _data.upgrades.Add(new UpgradeLevelEntry { id = upgrade.id, level = 1 });
            else entry.level = level + 1;

            _persist(_data);
            WalletChanged?.Invoke();
            return true;
        }

        /// <summary>End of run. Gold is always kept, even on death (game-concept.md §3.5).</summary>
        public void GrantRunRewards(long gold, int stardust, int clearedChapter = -1)
        {
            _data.gold += Math.Max(0, gold);
            _data.stardust += Math.Max(0, stardust);
            if (clearedChapter > _data.highestChapterCleared) _data.highestChapterCleared = clearedChapter;
            _persist(_data);
            WalletChanged?.Invoke();
        }

        /// <summary>Writes Workshop bonuses into the run's PlayerStats.</summary>
        public void ApplyTo(PlayerStats stats, IReadOnlyList<MetaUpgradeDefinition> upgrades)
        {
            stats.ClearMetaBonuses();
            for (int i = 0; i < upgrades.Count; i++)
            {
                MetaUpgradeDefinition u = upgrades[i];
                stats.AddMetaBonus(u.stat, u.effectPerLevel * GetLevel(u));
            }
        }

        private UpgradeLevelEntry Find(string id)
        {
            for (int i = 0; i < _data.upgrades.Count; i++)
                if (_data.upgrades[i].id == id) return _data.upgrades[i];
            return null;
        }
    }
}
