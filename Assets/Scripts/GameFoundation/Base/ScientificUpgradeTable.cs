using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game Foundation/Scientific Upgrade Table")]
public sealed class ScientificUpgradeTable : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public string id;
        public GameFoundation.Quests.ContentUnlockDefinition requiredUnlock;
        public bool IsAvailable => GameFoundation.Quests.ContentUnlocks.IsUnlocked(requiredUnlock);
        // Kept for backwards-compatible Google Sheets imports; no longer used for unlocking.
        [HideInInspector] public string parentId;
        [Tooltip("Общий ID серии. Уровни одной серии занимают одну строку лаборатории.")]
        public string groupId;
        public string groupTitle;
        public Sprite icon;
        [Min(1)] public int level = 1;
        [Tooltip("Сколько любых уровней улучшений нужно купить для открытия этого уровня.")]
        [Min(0)] public int requiredPurchases;
        public string title;
        [TextArea(2, 5)] public string description;
        public ResourceType costResource;
        [Min(0)] public int cost;
        public float effectValue;
        public string GroupId => string.IsNullOrEmpty(groupId) ? id : groupId;
    }

    public List<Entry> entries = new();
    public Entry Find(string id) => entries.Find(entry => entry.id == id);
}
