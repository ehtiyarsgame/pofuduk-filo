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
            // Saves from before mastery/pilot levels existed deserialize these as null.
            _data.weaponMastery ??= new List<UpgradeLevelEntry>();
            _data.pilotLevels ??= new List<UpgradeLevelEntry>();
        }

        public long Gold => _data.gold;
        public int Stardust => _data.stardust;
        public int HighestChapterCleared => _data.highestChapterCleared;
        public string SelectedCharacterId => _data.selectedCharacter;

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

        // ---------------------------------------------------------------- Weapon mastery & pilot levels

        public int GetMastery(string weaponId) => FindIn(_data.weaponMastery, weaponId)?.level ?? 0;

        public bool CanUpgradeMastery(WeaponDefinition w)
        {
            int level = GetMastery(w.id);
            return IsUnlocked(w) && level < Core.Formulas.MaxWeaponMastery && _data.gold >= Core.Formulas.MasteryCost(level);
        }

        public bool TryUpgradeMastery(WeaponDefinition w)
        {
            if (!CanUpgradeMastery(w)) return false;
            int level = GetMastery(w.id);
            _data.gold -= Core.Formulas.MasteryCost(level);
            SetIn(_data.weaponMastery, w.id, level + 1);
            _persist(_data);
            WalletChanged?.Invoke();
            return true;
        }

        public int GetPilotLevel(string characterId) => Math.Max(1, FindIn(_data.pilotLevels, characterId)?.level ?? 1);

        public bool CanLevelPilot(CharacterDefinition c)
        {
            int level = GetPilotLevel(c.id);
            return IsUnlocked(c) && level < Core.Formulas.MaxPilotLevel && _data.gold >= Core.Formulas.PilotLevelCost(level);
        }

        // ---------------------------------------------------------------- Forge (power-match.md §3.4)

        public int GetForgeLevel(ForgeTrack track) => track == ForgeTrack.Power ? _data.forgePower : _data.forgeSpeed;

        public bool IsForgeMaxed(ForgeTrack track) =>
            track == ForgeTrack.Speed && _data.forgeSpeed >= Core.Formulas.MaxForgeSpeedLevel;

        public int ForgeCost(ForgeTrack track) => Core.Formulas.ForgeCost(GetForgeLevel(track));

        public bool CanUpgradeForge(ForgeTrack track) => !IsForgeMaxed(track) && _data.gold >= ForgeCost(track);

        public bool TryUpgradeForge(ForgeTrack track)
        {
            if (!CanUpgradeForge(track) || !TrySpend(ForgeCost(track), 0)) return false;
            if (track == ForgeTrack.Power) _data.forgePower++;
            else _data.forgeSpeed++;
            Commit();
            return true;
        }

        // ---------------------------------------------------------------- Endless record

        public float BestEndlessSeconds => _data.bestEndlessSeconds;
        public int BestEndlessKills => _data.bestEndlessKills;

        /// <summary>Stores an Endless result; true if it beat the time record.</summary>
        public bool RecordEndless(float seconds, int kills)
        {
            bool best = seconds > _data.bestEndlessSeconds;
            if (best) _data.bestEndlessSeconds = seconds;
            if (kills > _data.bestEndlessKills) _data.bestEndlessKills = kills;
            _persist(_data);
            return best;
        }

        public bool TryLevelPilot(CharacterDefinition c)
        {
            if (!CanLevelPilot(c)) return false;
            int level = GetPilotLevel(c.id);
            _data.gold -= Core.Formulas.PilotLevelCost(level);
            SetIn(_data.pilotLevels, c.id, level + 1);
            _persist(_data);
            WalletChanged?.Invoke();
            return true;
        }

        private static UpgradeLevelEntry FindIn(List<UpgradeLevelEntry> list, string id)
        {
            if (list == null) return null;
            foreach (UpgradeLevelEntry e in list)
                if (e.id == id) return e;
            return null;
        }

        private static void SetIn(List<UpgradeLevelEntry> list, string id, int level)
        {
            UpgradeLevelEntry e = FindIn(list, id);
            if (e == null) list.Add(new UpgradeLevelEntry { id = id, level = level });
            else e.level = level;
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

        // ---------------------------------------------------------------- Spending

        private bool TrySpend(int gold, int stardust)
        {
            if (_data.gold < gold || _data.stardust < stardust) return false;
            _data.gold -= gold;
            _data.stardust -= stardust;
            return true;
        }

        private void Commit()
        {
            _persist(_data);
            WalletChanged?.Invoke();
        }

        // ---------------------------------------------------------------- Hangar (meta-economy.md §3.3 B)

        public bool IsUnlocked(CharacterDefinition c) =>
            c.IsFree
            || _data.unlockedCharacters.Contains(c.id)
            || (c.requiresChapterCleared >= 0 && _data.highestChapterCleared >= c.requiresChapterCleared
                && c.goldCost == 0 && c.stardustCost == 0);

        public bool CanUnlock(CharacterDefinition c) =>
            !IsUnlocked(c) && c.requiresChapterCleared < 0 && _data.gold >= c.goldCost && _data.stardust >= c.stardustCost;

        public bool TryUnlock(CharacterDefinition c)
        {
            if (!CanUnlock(c) || !TrySpend(c.goldCost, c.stardustCost)) return false;
            _data.unlockedCharacters.Add(c.id);
            Commit();
            return true;
        }

        public bool SelectCharacter(CharacterDefinition c)
        {
            if (!IsUnlocked(c)) return false;
            _data.selectedCharacter = c.id;
            Commit();
            return true;
        }

        // ---------------------------------------------------------------- Weapon Lab (meta-economy.md §3.3 C)

        public bool IsUnlocked(WeaponDefinition w) => w.labCost <= 0 || _data.unlockedWeapons.Contains(w.id);

        public bool IsUnlocked(PassiveDefinition p) => p.labCost <= 0 || _data.unlockedPassives.Contains(p.id);

        public bool TryUnlock(WeaponDefinition w)
        {
            if (IsUnlocked(w) || !TrySpend(w.labCost, 0)) return false;
            _data.unlockedWeapons.Add(w.id);
            Commit();
            return true;
        }

        public bool TryUnlock(PassiveDefinition p)
        {
            if (IsUnlocked(p) || !TrySpend(p.labCost, 0)) return false;
            _data.unlockedPassives.Add(p.id);
            Commit();
            return true;
        }

        // ---------------------------------------------------------------- Constellation (meta-economy.md §3.3 D)

        public bool HasNode(ConstellationNode node) => _data.constellationNodes.Contains(node.id);

        public bool CanBuy(ConstellationNode node) =>
            ConstellationRules.CanBuy(node.id, node.stardustCost, node.requires, _data.constellationNodes, _data.stardust);

        public bool TryBuy(ConstellationNode node)
        {
            if (!CanBuy(node) || !TrySpend(0, node.stardustCost)) return false;
            _data.constellationNodes.Add(node.id);
            Commit();
            return true;
        }

        public bool CanRespec(long nowUtcTicks) =>
            _data.constellationNodes.Count > 0 && ConstellationRules.CanRespec(_data.lastRespecUtcTicks, nowUtcTicks);

        /// <summary>Refunds every owned node in full (free once per 24 h).</summary>
        public bool Respec(ConstellationDefinition board, long nowUtcTicks)
        {
            if (!CanRespec(nowUtcTicks)) return false;
            foreach (string id in _data.constellationNodes)
            {
                ConstellationNode node = board.Find(id);
                if (node != null) _data.stardust += node.stardustCost;
            }
            _data.constellationNodes.Clear();
            _data.lastRespecUtcTicks = nowUtcTicks;
            Commit();
            return true;
        }

        /// <summary>Adds owned constellation nodes to the run's stats (after <see cref="ApplyTo"/>).</summary>
        public void ApplyConstellation(PlayerStats stats, ConstellationDefinition board)
        {
            if (board == null) return;
            foreach (ConstellationNode node in board.nodes)
            {
                if (!HasNode(node)) continue;
                foreach (StatModifier m in node.modifiers) stats.AddRunBonus(m.stat, m.value);
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
