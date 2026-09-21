using UnityEngine;

namespace GameFoundation.Saves
{
    public static class SaveSlotPrefs
    {
        private const string SelectedKey = "foundation.selectedSaveSlot";
        private const string LegacyDayKey = "foundation.daycycle";
        private const string TimeKey = "playSeconds";
        private static int _selected;

        public static int SelectedSlot
        {
            get
            {
                if (_selected == 0) _selected = Mathf.Clamp(UnityEngine.PlayerPrefs.GetInt(SelectedKey, 1), 1, 3);
                return _selected;
            }
        }

        private static string Prefix(int slot) => $"foundation.slot.{slot}.";
        private static string Key(string key) => Prefix(SelectedSlot) + key;
        private static bool UseLegacy(string key) => SelectedSlot == 1 && UnityEngine.PlayerPrefs.HasKey(key);

        public static void Select(int slot)
        {
            _selected = Mathf.Clamp(slot, 1, 3);
            UnityEngine.PlayerPrefs.SetInt(SelectedKey, _selected);
            UnityEngine.PlayerPrefs.SetInt(Prefix(_selected) + "created", 1);
            UnityEngine.PlayerPrefs.Save();
        }

        public static bool SlotExists(int slot)
        {
            if (slot < 1 || slot > 3) return false;
            return UnityEngine.PlayerPrefs.HasKey(Prefix(slot) + "created") ||
                (slot == 1 && (UnityEngine.PlayerPrefs.HasKey(LegacyDayKey) ||
                 UnityEngine.PlayerPrefs.HasKey("Global_Resource_Human")));
        }

        public static int SlotResource(int slot, string resource)
        {
            string key = "Global_Resource_" + resource;
            return UnityEngine.PlayerPrefs.GetInt(Prefix(slot) + key,
                slot == 1 ? UnityEngine.PlayerPrefs.GetInt(key, 0) : 0);
        }

        public static float SlotPlaySeconds(int slot) => UnityEngine.PlayerPrefs.GetFloat(Prefix(slot) + TimeKey, 0f);

        public static void AddPlaySeconds(float seconds)
        {
            if (seconds <= 0f) return;
            UnityEngine.PlayerPrefs.SetFloat(Key(TimeKey), SlotPlaySeconds(SelectedSlot) + seconds);
            UnityEngine.PlayerPrefs.Save();
        }

        public static bool HasKey(string key) => UnityEngine.PlayerPrefs.HasKey(Key(key)) || UseLegacy(key);

        public static int GetInt(string key, int fallback = 0)
        {
            if (UnityEngine.PlayerPrefs.HasKey(Key(key))) return UnityEngine.PlayerPrefs.GetInt(Key(key), fallback);
            if (UseLegacy(key))
            {
                int value = UnityEngine.PlayerPrefs.GetInt(key, fallback);
                UnityEngine.PlayerPrefs.SetInt(Key(key), value);
                return value;
            }
            return fallback;
        }

        public static float GetFloat(string key, float fallback = 0f)
        {
            if (UnityEngine.PlayerPrefs.HasKey(Key(key))) return UnityEngine.PlayerPrefs.GetFloat(Key(key), fallback);
            if (UseLegacy(key))
            {
                float value = UnityEngine.PlayerPrefs.GetFloat(key, fallback);
                UnityEngine.PlayerPrefs.SetFloat(Key(key), value);
                return value;
            }
            return fallback;
        }

        public static string GetString(string key, string fallback = "")
        {
            if (UnityEngine.PlayerPrefs.HasKey(Key(key))) return UnityEngine.PlayerPrefs.GetString(Key(key), fallback);
            if (UseLegacy(key))
            {
                string value = UnityEngine.PlayerPrefs.GetString(key, fallback);
                UnityEngine.PlayerPrefs.SetString(Key(key), value);
                return value;
            }
            return fallback;
        }

        public static void SetInt(string key, int value) => UnityEngine.PlayerPrefs.SetInt(Key(key), value);
        public static void SetFloat(string key, float value) => UnityEngine.PlayerPrefs.SetFloat(Key(key), value);
        public static void SetString(string key, string value) => UnityEngine.PlayerPrefs.SetString(Key(key), value);
        public static void DeleteKey(string key)
        {
            UnityEngine.PlayerPrefs.DeleteKey(Key(key));
            if (SelectedSlot == 1) UnityEngine.PlayerPrefs.DeleteKey(key);
        }
        public static void Save() => UnityEngine.PlayerPrefs.Save();
    }
}
