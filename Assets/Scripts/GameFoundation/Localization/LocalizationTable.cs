using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFoundation.Localization
{
    [CreateAssetMenu(menuName = "Game Foundation/Localization Table")]
    public sealed class LocalizationTable : ScriptableObject
    {
        [Serializable] public class Entry { public string key; public List<string> values = new(); }
        public List<string> languages = new() { "ru", "en" };
        public List<Entry> entries = new();
        public string Get(string key, string language)
        {
            var entry = entries.Find(x => x.key == key); var index = languages.IndexOf(language);
            return entry != null && index >= 0 && index < entry.values.Count && !string.IsNullOrEmpty(entry.values[index]) ? entry.values[index] : key;
        }
    }
}
