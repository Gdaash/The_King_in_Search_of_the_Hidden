using System;
using UnityEngine;

namespace GameFoundation.Localization
{
    public sealed class LocalizationService : MonoBehaviour
    {
        public static LocalizationService Instance { get; private set; }
        [SerializeField] private LocalizationTable table;
        public string Language { get; private set; }
        public event Action LanguageChanged;
        private const string Key = "foundation.language";
        private void Awake() { if (Instance != null) { Destroy(this); return; } Instance=this; DontDestroyOnLoad(gameObject); Language=PlayerPrefs.GetString(Key, Application.systemLanguage == SystemLanguage.Russian ? "ru" : "en"); }
        public string Get(string key) => table == null ? key : table.Get(key, Language);
        public void SetLanguage(string code) { Language=code; PlayerPrefs.SetString(Key,code); PlayerPrefs.Save(); LanguageChanged?.Invoke(); }
        public void SetTable(LocalizationTable value) { table=value; LanguageChanged?.Invoke(); }
    }
}
