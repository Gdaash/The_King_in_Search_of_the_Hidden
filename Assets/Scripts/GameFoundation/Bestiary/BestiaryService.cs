using System;
using System.Collections.Generic;
using System.Linq;
using GameFoundation.Saves;
using UnityEngine;

namespace GameFoundation.Bestiary
{
    [Serializable]
    internal sealed class BestiarySave
    {
        public List<string> encountered = new();
        public List<EnemyKillRecord> kills = new();
    }

    [Serializable]
    internal sealed class EnemyKillRecord { public string id; public int count; }

    /// <summary>Slot-local record of enemy types that have appeared in a World run.</summary>
    public static class BestiaryService
    {
        private const string SaveKey = "bestiary.encountered";
        private static BestiarySave save;
        private static int loadedSlot = -1;
        private static readonly Dictionary<string, EnemyKillRecord> killCounts = new(StringComparer.Ordinal);
        public const float StudiedDamageMultiplier = 1.2f;

        public static event Action<string> Encountered;
        public static event Action<string> ResearchChanged;

        public static int RequiredKills(int dangerRank) => 50 - Mathf.RoundToInt((Mathf.Clamp(dangerRank, 1, 12) - 1) * 40f / 11f);
        public static int Kills(string id)
        {
            _ = Data;
            return !string.IsNullOrEmpty(id) && killCounts.TryGetValue(id, out var record) ? record.count : 0;
        }
        public static bool IsStudied(EnemyIdentity identity) => identity != null && Kills(identity.Id) >= RequiredKills(identity.DangerRank);
        public static float DamageMultiplier(EnemyIdentity identity) => IsStudied(identity) ? StudiedDamageMultiplier : 1f;

        public static void RegisterKill(EnemyIdentity identity)
        {
            if (identity == null || string.IsNullOrEmpty(identity.Id)) return;
            var data = Data;
            if (!killCounts.TryGetValue(identity.Id, out var record))
            {
                record = new EnemyKillRecord { id = identity.Id };
                data.kills.Add(record); killCounts.Add(identity.Id, record);
            }
            if (record.count == int.MaxValue) return;
            record.count++;
            if (!data.encountered.Contains(identity.Id))
            { data.encountered.Add(identity.Id); Encountered?.Invoke(identity.Id); }
            // Ordinary kills update the slot's in-memory save. SaveSlotClock flushes
            // it periodically and on exit; only discovering a weakness flushes now.
            Persist(record.count == RequiredKills(identity.DangerRank));
            ResearchChanged?.Invoke(identity.Id);
        }

        internal static void ResetCache()
        { save = null; loadedSlot = -1; killCounts.Clear(); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntime()
        { ResetCache(); Encountered = null; ResearchChanged = null; }

        public static IReadOnlyList<string> EncounteredIds => Data.encountered;

        public static bool HasEncountered(GameObject enemyPrefab) =>
            enemyPrefab != null && enemyPrefab.TryGetComponent<EnemyIdentity>(out var identity) && Data.encountered.Contains(identity.Id);

        /// <summary>Records only combat enemies. Calling it more than once for a spawn is safe.</summary>
        public static void RegisterSpawn(GameObject enemyPrefab, GameObject instance)
        {
            if (enemyPrefab == null || instance == null || !instance.CompareTag("Enemy1") || instance.GetComponent<Health>() == null)
                return;

            if (!enemyPrefab.TryGetComponent<EnemyIdentity>(out var identity) || string.IsNullOrEmpty(identity.Id))
            {
                Debug.LogError("Enemy prefab requires a persistent EnemyIdentity.", enemyPrefab);
                return;
            }
            string id = identity.Id;
            if (Data.encountered.Contains(id))
                return;

            Data.encountered.Add(id);
            Persist();
            Encountered?.Invoke(id);
        }

        private static BestiarySave Data
        {
            get
            {
                if (save != null && loadedSlot == SaveSlotPrefs.SelectedSlot)
                    return save;

                loadedSlot = SaveSlotPrefs.SelectedSlot;
                save = SaveSlotPrefs.HasKey(SaveKey)
                    ? JsonUtility.FromJson<BestiarySave>(SaveSlotPrefs.GetString(SaveKey))
                    : null;
                save ??= new BestiarySave();
                save.encountered ??= new List<string>();
                save.kills ??= new List<EnemyKillRecord>();
                killCounts.Clear();
                foreach (var record in save.kills)
                    if (record != null && !string.IsNullOrEmpty(record.id))
                    { record.count = Mathf.Max(0, record.count); killCounts[record.id] = record; }
                if (MigrateLegacyIds(save))
                {
                    SaveSlotPrefs.SetString(SaveKey, JsonUtility.ToJson(save));
                    SaveSlotPrefs.Save();
                }
                return save;
            }
        }

        private static bool MigrateLegacyIds(BestiarySave data)
        {
            // Goblin became Octopus while retaining the same gameplay role.
            if (!data.encountered.Remove("Goblin")) return false;
            if (!data.encountered.Contains("Octopus")) data.encountered.Add("Octopus");
            return true;
        }

        private static void Persist(bool flush = true)
        {
            SaveSlotPrefs.SetString(SaveKey, JsonUtility.ToJson(Data));
            if (flush) SaveSlotPrefs.Save();
        }
    }
}
