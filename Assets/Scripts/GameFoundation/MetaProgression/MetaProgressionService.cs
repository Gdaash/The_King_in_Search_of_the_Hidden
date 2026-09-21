using PlayerPrefs = GameFoundation.Saves.SaveSlotPrefs;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFoundation.MetaProgression
{
    [Serializable] public class MetaUpgrade { public string id; public int level; public int maxLevel = 5; }

    public sealed class MetaProgressionService : MonoBehaviour
    {
        public static MetaProgressionService Instance { get; private set; }
        [SerializeField] private List<MetaUpgrade> upgrades = new();
        public event Action Changed;
        private const string SaveKey = "foundation.meta.upgrades";

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject); Load();
        }
        public int GetLevel(string id) => upgrades.Find(x => x.id == id)?.level ?? 0;
        public static int GetSavedLevel(string id)
        {
            if (!PlayerPrefs.HasKey(SaveKey)) return 0;
            var data = JsonUtility.FromJson<MetaSave>(PlayerPrefs.GetString(SaveKey));
            return data?.upgrades?.Find(x => x.id == id)?.level ?? 0;
        }
        public bool TryUpgrade(string id, int maximumLevel)
        {
            var item = upgrades.Find(x => x.id == id);
            if (item == null) { item = new MetaUpgrade { id = id, maxLevel = maximumLevel }; upgrades.Add(item); }
            if (item.level >= item.maxLevel) return false;
            item.level++; Save(); Changed?.Invoke(); return true;
        }
        private void Save()
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(new MetaSave { upgrades = upgrades }));
            PlayerPrefs.Save();
        }
        private void Load()
        {
            if (!PlayerPrefs.HasKey(SaveKey)) return;
            var data = JsonUtility.FromJson<MetaSave>(PlayerPrefs.GetString(SaveKey));
            if (data?.upgrades != null) upgrades = data.upgrades;
        }
        [Serializable] private class MetaSave { public List<MetaUpgrade> upgrades; }
    }
}


