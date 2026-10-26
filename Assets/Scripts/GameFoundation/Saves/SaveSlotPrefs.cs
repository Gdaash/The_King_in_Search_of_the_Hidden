using System.Globalization;
using UnityEngine;

namespace GameFoundation.Saves
{
    /// <summary>Compatibility facade. All gameplay persistence is owned by GameSaveService.</summary>
    public static class SaveSlotPrefs
    {
        private const string TimeKey = "playSeconds";
        private static int _selected;
        public static event System.Action SelectionChanged;
        public static int SelectedSlot => _selected == 0 ? _selected = Mathf.Clamp(UnityEngine.PlayerPrefs.GetInt("foundation.selectedSaveSlot", 1), 1, 3) : _selected;
        public static void Select(int slot) { _selected = Mathf.Clamp(slot, 1, 3); GameSaveService.MarkSelected(_selected); SelectionChanged?.Invoke(); }
        public static bool SlotExists(int slot) => GameSaveService.SlotExists(slot);
        public static bool ClearSlot(int slot) => GameSaveService.ClearSlot(slot);
        public static int SlotResource(int slot, string resource) => GameSaveService.GetInt(slot, "Global_Resource_" + resource, 0);
        public static float SlotPlaySeconds(int slot) => GameSaveService.GetFloat(slot, TimeKey, 0f);
        public static void AddPlaySeconds(float seconds) { if (seconds > 0f) { SetFloat(TimeKey, SlotPlaySeconds(SelectedSlot) + seconds); Save(); } }
        public static bool HasKey(string key) => GameSaveService.HasKey(SelectedSlot, key);
        public static int GetInt(string key, int fallback = 0) => GameSaveService.GetInt(SelectedSlot, key, fallback);
        public static float GetFloat(string key, float fallback = 0f) => GameSaveService.GetFloat(SelectedSlot, key, fallback);
        public static string GetString(string key, string fallback = "") => GameSaveService.GetString(SelectedSlot, key, fallback);
        public static void SetInt(string key, int value) => GameSaveService.Set(SelectedSlot, key, "int", value.ToString(CultureInfo.InvariantCulture));
        public static void SetFloat(string key, float value) => GameSaveService.Set(SelectedSlot, key, "float", value.ToString(CultureInfo.InvariantCulture));
        public static void SetString(string key, string value) => GameSaveService.Set(SelectedSlot, key, "string", value ?? string.Empty);
        public static void DeleteKey(string key) => GameSaveService.Delete(SelectedSlot, key);
        public static void Save() => GameSaveService.Save(SelectedSlot);
        public static System.IDisposable Batch() => GameSaveService.Batch();
        public static bool BeginRunCheckpoint() => GameSaveService.BeginRunCheckpoint(SelectedSlot);
        public static void CompleteRunCheckpoint() => GameSaveService.CompleteRunCheckpoint(SelectedSlot);
        public static void ResetAll()
        {
            if (GameSaveService.WritesSuspended) return;
            GameSaveService.ClearAllFiles();
            UnityEngine.PlayerPrefs.DeleteAll();
            UnityEngine.PlayerPrefs.Save();
            _selected = 0;
            GameSaveService.ResetRunSession();
        }
    }
}
