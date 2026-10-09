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
        internal static bool WritesSuspended;
        private static int batchDepth;
        private static bool LegacyAllowed(int slot) => !UnityEngine.PlayerPrefs.HasKey(Prefix(slot) + "reset");
        internal static void ResetSlot(int slot)
        {
            pendingSaves.Remove(slot);
            LoadedSlots[slot] = new SlotData();
            UnityEngine.PlayerPrefs.SetInt(Prefix(slot) + "reset", 1);
            UnityEngine.PlayerPrefs.SetString(EnvelopeKey(slot), JsonUtility.ToJson(LoadedSlots[slot]));
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
        [Serializable] private sealed class SlotData { public int version = CurrentVersion; public List<Entry> entries = new(); }
        private static string Prefix(int slot) => $"foundation.slot.{slot}.";
        private static string EnvelopeKey(int slot) => Prefix(slot) + "saveData";
        private static SlotData Data(int slot)
        {
            if (LoadedSlots.TryGetValue(slot, out SlotData data)) return data;
            string json = UnityEngine.PlayerPrefs.GetString(EnvelopeKey(slot), string.Empty);
            data = string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<SlotData>(json);
            data ??= new SlotData(); data.entries ??= new List<Entry>(); LoadedSlots[slot] = data;
            return data;
        }
        private static Entry Find(int slot, string key) => Data(slot).entries.Find(item => item != null && item.key == key);
        private static Entry GetOrMigrate(int slot, string key, string type, string fallback)
        {
            Entry entry = Find(slot, key); if (entry != null) return entry;
            string prefixed = Prefix(slot) + key;
            string source = !LegacyAllowed(slot) ? null : UnityEngine.PlayerPrefs.HasKey(prefixed) ? prefixed : slot == 1 && UnityEngine.PlayerPrefs.HasKey(key) ? key : null;
            string value = fallback;
            if (source != null) value = type == "int" ? UnityEngine.PlayerPrefs.GetInt(source).ToString(CultureInfo.InvariantCulture) : type == "float" ? UnityEngine.PlayerPrefs.GetFloat(source).ToString(CultureInfo.InvariantCulture) : UnityEngine.PlayerPrefs.GetString(source, fallback);
            entry = new Entry { key = key, type = type, value = value }; Data(slot).entries.Add(entry); return entry;
        }
        internal static bool SlotExists(int slot) => slot >= 1 && slot <= 3 && (UnityEngine.PlayerPrefs.HasKey(Prefix(slot) + "created") || UnityEngine.PlayerPrefs.HasKey(EnvelopeKey(slot)) || (slot == 1 && (UnityEngine.PlayerPrefs.HasKey("foundation.daycycle") || UnityEngine.PlayerPrefs.HasKey("Global_Resource_Human"))));
        internal static void MarkSelected(int slot) { UnityEngine.PlayerPrefs.SetInt("foundation.selectedSaveSlot", slot); UnityEngine.PlayerPrefs.SetInt(Prefix(slot) + "created", 1); Save(slot); }
        internal static bool HasKey(int slot, string key) => Find(slot, key) != null || (LegacyAllowed(slot) && (UnityEngine.PlayerPrefs.HasKey(Prefix(slot) + key) || (slot == 1 && UnityEngine.PlayerPrefs.HasKey(key))));
        internal static int GetInt(int slot, string key, int fallback) => int.TryParse(GetOrMigrate(slot, key, "int", fallback.ToString(CultureInfo.InvariantCulture)).value, out int value) ? value : fallback;
        internal static float GetFloat(int slot, string key, float fallback) => float.TryParse(GetOrMigrate(slot, key, "float", fallback.ToString(CultureInfo.InvariantCulture)).value, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : fallback;
        internal static string GetString(int slot, string key, string fallback) => GetOrMigrate(slot, key, "string", fallback).value ?? fallback;
        internal static void Set(int slot, string key, string type, string value) { if (WritesSuspended) return; Entry entry = Find(slot, key); if (entry == null) { entry = new Entry { key = key }; Data(slot).entries.Add(entry); } entry.type = type; entry.value = value; }
        internal static void Delete(int slot, string key) { if (WritesSuspended) return; Data(slot).entries.RemoveAll(item => item != null && item.key == key); UnityEngine.PlayerPrefs.DeleteKey(Prefix(slot) + key); if (slot == 1) UnityEngine.PlayerPrefs.DeleteKey(key); }
        internal static void Save(int slot) { if (WritesSuspended) return; if (batchDepth > 0) { pendingSaves.Add(slot); return; } UnityEngine.PlayerPrefs.SetString(EnvelopeKey(slot), JsonUtility.ToJson(Data(slot))); UnityEngine.PlayerPrefs.Save(); }
        internal static void ResetCache() => LoadedSlots.Clear();
    }
}
