using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace GameFoundation.Saves
{
    /// <summary>Single versioned storage envelope for each gameplay save slot.</summary>
    internal static class GameSaveService
    {
        private const int CurrentVersion = 1;
        private static readonly Dictionary<int, SlotData> LoadedSlots = new();
        private static readonly HashSet<int> activeRuns = new();
        private static readonly HashSet<int> locallyMigrating = new();
        internal static bool WritesSuspended;
        internal static bool ClearSlot(int slot)
        {
            if (WritesSuspended || slot < 1 || slot > 3) return false;
            SaveFileStorage.Write(slot, JsonUtility.ToJson(new SlotData { deleted = true }));
            pendingSaves.Remove(slot); activeRuns.Remove(slot); LoadedSlots.Remove(slot);
            // Keep the tombstone so legacy keys cannot resurrect a deleted slot.
            EraseSlotStorage(slot, UnityEngine.PlayerPrefs.SetInt, UnityEngine.PlayerPrefs.DeleteKey, UnityEngine.PlayerPrefs.Save);
            if (SaveSlotPrefs.SelectedSlot == slot) GameFoundation.Bestiary.BestiaryService.ResetCache();
            return true;
        }
        internal static void EraseSlotStorage(int slot, Action<string,int> setInt, Action<string> deleteKey, Action save)
        {
            if (slot < 1 || slot > 3) throw new ArgumentOutOfRangeException(nameof(slot));
            setInt(Prefix(slot) + "reset", 1);
            deleteKey(Prefix(slot) + "created");deleteKey(EnvelopeKey(slot));save();
        }
        private static int batchDepth;
        private static bool LegacyAllowed(int slot) => (!SaveFileStorage.Exists(slot) || locallyMigrating.Contains(slot)) && !UnityEngine.PlayerPrefs.HasKey(Prefix(slot) + "reset");
        internal static void ResetSlot(int slot)
        {
            GameFoundation.Bestiary.BestiaryService.ResetCache();
            pendingSaves.Remove(slot);
            activeRuns.Remove(slot);
            LoadedSlots[slot] = new SlotData();
            UnityEngine.PlayerPrefs.SetInt(Prefix(slot) + "reset", 1);
            // Reset is called after unloading gameplay while ordinary writes are suspended.
            string json = JsonUtility.ToJson(LoadedSlots[slot]);
            SaveFileStorage.Write(slot, json);
            UnityEngine.PlayerPrefs.SetString(EnvelopeKey(slot), json);
            UnityEngine.PlayerPrefs.Save();
        }
        private static readonly HashSet<int> pendingSaves = new();
        internal static IDisposable Batch() { batchDepth++; return new SaveBatch(); }
        private sealed class SaveBatch : IDisposable
        {
            private bool disposed;
            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                if (--batchDepth != 0) return;
                var slots = new List<int>(pendingSaves);
                pendingSaves.Clear();
                foreach (int slot in slots) Save(slot);
            }
        }
        [Serializable] private sealed class Entry { public string key; public string type; public string value; }
        [Serializable] private sealed class SlotData
        {
            public int version = CurrentVersion;
            public bool deleted;
            public List<Entry> entries = new();
            public string unfinishedRunCheckpoint;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetRunSession()
        {
            activeRuns.Clear(); locallyMigrating.Clear(); LoadedSlots.Clear(); pendingSaves.Clear(); batchDepth = 0;
        }
        private static string Prefix(int slot) => $"foundation.slot.{slot}.";
        private static string EnvelopeKey(int slot) => Prefix(slot) + "saveData";
        private static SlotData Data(int slot)
        {
            if (LoadedSlots.TryGetValue(slot, out SlotData data)) return data;
            bool fileExists = SaveFileStorage.Exists(slot);
            if (!fileExists) locallyMigrating.Add(slot);
            string json = fileExists ? SaveFileStorage.Read(slot) : UnityEngine.PlayerPrefs.GetString(EnvelopeKey(slot), string.Empty);
            try { data = string.IsNullOrEmpty(json) ? null : Parse(json); }
            catch (Exception error) when (error is ArgumentException || error is System.IO.InvalidDataException)
            {
                if (!fileExists) throw;
                data = Parse(SaveFileStorage.ReadBackup(slot));
                Debug.LogWarning("Recovered gameplay save from its local backup: " + error.Message);
            }
            bool migrate = !fileExists && data != null;
            data ??= new SlotData(); data.entries ??= new List<Entry>();
            if (!activeRuns.Contains(slot) && !string.IsNullOrEmpty(data.unfinishedRunCheckpoint))
            {
                // One envelope contains both the baseline and its pending marker, so a crash
                // cannot leave a new inventory paired with an old completion marker.
                SlotData restored = null;
                try { restored = JsonUtility.FromJson<SlotData>(data.unfinishedRunCheckpoint); }
                catch (ArgumentException) { Debug.LogWarning("Unreadable run checkpoint; retaining the saved shelter inventory."); }
                if (restored != null && restored.entries != null)
                {
                    data = restored; data.unfinishedRunCheckpoint = null;
                    if (!WritesSuspended) { Persist(slot, data); UnityEngine.PlayerPrefs.Save(); }
                }
                else data.unfinishedRunCheckpoint = null;
            }
            LoadedSlots[slot] = data;
            if (migrate && !WritesSuspended) Persist(slot, data);
            return data;
        }
        private static Entry Find(int slot, string key) => Data(slot).entries.Find(item => item != null && item.key == key);
        private static Entry GetOrMigrate(int slot, string key, string type, string fallback)
        {
            Entry entry = Find(slot, key); if (entry != null) return entry.type == "deleted" ? new Entry { key = key, type = type, value = fallback } : entry;
            string prefixed = Prefix(slot) + key;
            string source = !LegacyAllowed(slot) ? null : UnityEngine.PlayerPrefs.HasKey(prefixed) ? prefixed : slot == 1 && UnityEngine.PlayerPrefs.HasKey(key) ? key : null;
            string value = fallback;
            if (source != null) value = type == "int" ? UnityEngine.PlayerPrefs.GetInt(source).ToString(CultureInfo.InvariantCulture) : type == "float" ? UnityEngine.PlayerPrefs.GetFloat(source).ToString(CultureInfo.InvariantCulture) : UnityEngine.PlayerPrefs.GetString(source, fallback);
            entry = new Entry { key = key, type = type, value = value }; Data(slot).entries.Add(entry); return entry;
        }
        internal static bool SlotExists(int slot)
        {
            if (slot < 1 || slot > 3) return false;
            if (SaveFileStorage.Exists(slot)) return !Data(slot).deleted;
            return UnityEngine.PlayerPrefs.HasKey(Prefix(slot) + "created") || UnityEngine.PlayerPrefs.HasKey(EnvelopeKey(slot)) ||
                (slot == 1 && LegacyAllowed(slot) && (UnityEngine.PlayerPrefs.HasKey("foundation.daycycle") || UnityEngine.PlayerPrefs.HasKey("Global_Resource_Human")));
        }

        internal static void MarkSelected(int slot) { if (WritesSuspended) return; Data(slot).deleted = false; UnityEngine.PlayerPrefs.SetInt("foundation.selectedSaveSlot", slot); UnityEngine.PlayerPrefs.SetInt(Prefix(slot) + "created", 1); Save(slot); }
        internal static bool HasKey(int slot, string key)
        {
            var entry = Find(slot, key);
            return entry != null ? entry.type != "deleted" : LegacyAllowed(slot) && (UnityEngine.PlayerPrefs.HasKey(Prefix(slot) + key) || (slot == 1 && UnityEngine.PlayerPrefs.HasKey(key)));
        }
        internal static int GetInt(int slot, string key, int fallback) => int.TryParse(GetOrMigrate(slot, key, "int", fallback.ToString(CultureInfo.InvariantCulture)).value, out int value) ? value : fallback;
        internal static float GetFloat(int slot, string key, float fallback) => float.TryParse(GetOrMigrate(slot, key, "float", fallback.ToString(CultureInfo.InvariantCulture)).value, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : fallback;
        internal static string GetString(int slot, string key, string fallback) => GetOrMigrate(slot, key, "string", fallback).value ?? fallback;
        internal static void Set(int slot, string key, string type, string value) { if (WritesSuspended) return; Entry entry = Find(slot, key); if (entry == null) { entry = new Entry { key = key }; Data(slot).entries.Add(entry); } entry.type = type; entry.value = value; }
        internal static void Delete(int slot, string key)
        {
            if (WritesSuspended) return;
            Set(slot, key, "deleted", "");
            // Raw legacy keys also belong to the baseline until the battle finishes.
            if (!activeRuns.Contains(slot)) { UnityEngine.PlayerPrefs.DeleteKey(Prefix(slot) + key); if (slot == 1) UnityEngine.PlayerPrefs.DeleteKey(key); }
        }
        internal static void Save(int slot) { if (WritesSuspended || activeRuns.Contains(slot)) return; if (batchDepth > 0) { pendingSaves.Add(slot); return; } Persist(slot, Data(slot)); UnityEngine.PlayerPrefs.Save(); }
        internal static bool BeginRunCheckpoint(int slot)
        {
            if (WritesSuspended || activeRuns.Contains(slot)) return false;
            var data = Data(slot);
            data.unfinishedRunCheckpoint = JsonUtility.ToJson(data);
            activeRuns.Add(slot); pendingSaves.Remove(slot);
            Persist(slot, data); UnityEngine.PlayerPrefs.Save();
            return true;
        }
        internal static void CompleteRunCheckpoint(int slot)
        {
            if (WritesSuspended || !activeRuns.Remove(slot)) return;
            Data(slot).unfinishedRunCheckpoint = null;
            Save(slot);
        }
        internal static bool HasActiveRunCheckpoint(int slot) => activeRuns.Contains(slot);
        private static SlotData Parse(string json)
        {
            var result = JsonUtility.FromJson<SlotData>(json);
            if (result == null || result.version != CurrentVersion || result.entries == null)
                throw new System.IO.InvalidDataException("Unsupported or invalid gameplay save.");
            return result;
        }
        private static void Persist(int slot, SlotData data)
        {
            if (WritesSuspended) return;
            string json = JsonUtility.ToJson(data);
            SaveFileStorage.Write(slot, json);
            // Retain the old local envelope for compatibility, but synced files take precedence.
            UnityEngine.PlayerPrefs.SetString(EnvelopeKey(slot), json);
        }
        internal static void ClearAllFiles()
        {
            if (WritesSuspended) return;
            for (int slot = 1; slot <= 3; slot++) SaveFileStorage.Write(slot, JsonUtility.ToJson(new SlotData { deleted = true }));
        }
        internal static void ResetCache()
        {
            foreach (int slot in new List<int>(LoadedSlots.Keys)) if (!activeRuns.Contains(slot)) LoadedSlots.Remove(slot);
            GameFoundation.Bestiary.BestiaryService.ResetCache();
        }
    }
}
