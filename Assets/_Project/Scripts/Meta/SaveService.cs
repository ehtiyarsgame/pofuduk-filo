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
