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
    }

    /// <summary>Slot-local record of enemy types that have appeared in a World run.</summary>
    public static class BestiaryService
    {
        private const string SaveKey = "bestiary.encountered";
        private static BestiarySave save;
        private static int loadedSlot = -1;

        public static event Action<string> Encountered;

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

        private static void Persist()
        {
            SaveSlotPrefs.SetString(SaveKey, JsonUtility.ToJson(Data));
            SaveSlotPrefs.Save();
        }
    }
}
