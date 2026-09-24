using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace PofudukFilo.Meta
{
    [Serializable]
    public sealed class UpgradeLevelEntry
    {
        public string id;
        public int level;
    }

    /// <summary>JsonUtility-friendly save data (no dictionaries).</summary>
    [Serializable]
    public sealed class SaveData
    {
        public int version = 1;
        public long gold;
        public int stardust;
        public List<UpgradeLevelEntry> upgrades = new();
        // Purchased unlocks only; free items (cost 0) are always available.
        public List<string> unlockedCharacters = new();
        public List<string> unlockedWeapons = new();
        public List<string> unlockedPassives = new();
        public List<string> constellationNodes = new();
        public string selectedCharacter = "pitir";
        public long lastRespecUtcTicks;
        public int highestChapterCleared = -1;
        public List<UpgradeLevelEntry> weaponMastery = new();
        public List<UpgradeLevelEntry> pilotLevels = new();
        // Forge tracks (power-match.md §3.4) and the Endless record.
        public int forgePower;
        public int forgeSpeed;
        public float bestEndlessSeconds;
        public int bestEndlessKills;
        // Rewarded ads (ad-rewards.md): per-item unlock progress and the daily counters.
        public List<UpgradeLevelEntry> adProgress = new();
        public long adDay = -1;
        public int adViewsToday;
        public int giftsToday;
        public long lastGiftUtcTicks;
    }

    /// <summary>
    /// A run in flight (run-resume.md): enough to continue exactly where the player left — stage, clock, loadout,
    /// level, HP, gold, fleet and the Power Match state. Enemies and bullets are not stored; a resume starts with
    /// a short breather instead.
    /// </summary>
    [Serializable]
    public sealed class RunSnapshot
    {
        public int version = 1;
        public string characterId;
        // Timeline
        public int stage;
        public float elapsed;
        public float stageStartedAt;
        public int phaseIndex;
        public bool isEndless;
        public int endlessBossCycle;
        public bool bossWasAlive;
        // Power Match
        public float powerScale = 1f;
        public float powerAverage = -1f;
        public float powerBaseline = -1f;
        public float powerElapsed;
        // Player & build
        public float hp;
        public int level = 1;
        public int xp;
        public int pendingLevelUps;
        public List<UpgradeLevelEntry> weapons = new();
        public List<UpgradeLevelEntry> passives = new();
        public float extraRunDamage;
        // Economy & counters
        public int runGold;
        public int fallbackGold;
        public int kills;
        public int revivesLeft;
        public bool freeReviveUsed;
        public int rerollsLeft;
        public int banishesLeft;
        public int stageGold;
        public int stageStardust;
        public int highestStageCleared = -1;
        // Fleet
        public int wingmen;
        public float fleetPower = 1f;
        public int fleetKills;
        public int fleetMilestone;
        public int fleetNextPilot;
    }

    [Serializable]
    internal sealed class SignedEnvelope
    {
        public string payload;
        public string signature;
    }

    /// <summary>
    /// Local save with atomic write (tmp → replace) and an HMAC signature.
    /// The HMAC only deters casual file editing; premium currency must be validated
    /// server-side (architecture.md §9).
    /// </summary>
    public static class SaveService
    {
        private const string FileName = "save.json";
        // Obfuscation only — anything shipped in the client can be extracted.
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("pofuduk-filo-local-save-v1");

        private static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        public static SaveData Load()
        {
            try
            {
                if (!File.Exists(SavePath)) return new SaveData();

                var envelope = JsonUtility.FromJson<SignedEnvelope>(File.ReadAllText(SavePath));
                if (envelope == null || envelope.signature != Sign(envelope.payload))
                {
                    Debug.LogWarning("[SaveService] Signature mismatch — starting a fresh save.");
                    return new SaveData();
                }

                return JsonUtility.FromJson<SaveData>(envelope.payload) ?? new SaveData();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] Load failed: {e}");
                return new SaveData();
            }
        }

        // ---------------------------------------------------------------- Run in flight (run-resume.md)

        private static string RunPath => Path.Combine(Application.persistentDataPath, "run.json");

        public static bool HasRun => File.Exists(RunPath);

        public static RunSnapshot LoadRun()
        {
            try
            {
                if (!File.Exists(RunPath)) return null;
                var envelope = JsonUtility.FromJson<SignedEnvelope>(File.ReadAllText(RunPath));
                if (envelope == null || envelope.signature != Sign(envelope.payload)) return null;
                return JsonUtility.FromJson<RunSnapshot>(envelope.payload);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveService] Run snapshot unreadable: {e.Message}");
                return null;
            }
        }

        public static void SaveRun(RunSnapshot run)
        {
            try
            {
                string payload = JsonUtility.ToJson(run);
                string json = JsonUtility.ToJson(new SignedEnvelope { payload = payload, signature = Sign(payload) });
                string tmp = RunPath + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(RunPath)) File.Replace(tmp, RunPath, null);
                else File.Move(tmp, RunPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveService] Run snapshot not saved: {e.Message}");
            }
        }

        public static void DeleteRun()
        {
            try
            {
                if (File.Exists(RunPath)) File.Delete(RunPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveService] Run snapshot not deleted: {e.Message}");
            }
        }

        public static void Save(SaveData data)
        {
            string payload = JsonUtility.ToJson(data);
            string json = JsonUtility.ToJson(new SignedEnvelope { payload = payload, signature = Sign(payload) });

            string tmp = SavePath + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(SavePath)) File.Replace(tmp, SavePath, null);
            else File.Move(tmp, SavePath);
        }

        private static string Sign(string payload)
        {
            using var hmac = new HMACSHA256(Key);
            return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload ?? string.Empty)));
        }
    }
}
