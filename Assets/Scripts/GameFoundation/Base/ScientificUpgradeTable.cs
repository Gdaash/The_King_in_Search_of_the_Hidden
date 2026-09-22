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
        public string parentId;
        public string title;
        [TextArea(2, 5)] public string description;
        public ResourceType costResource;
        [Min(0)] public int cost;
        public float effectValue;
    }

    public List<Entry> entries = new();
    public Entry Find(string id) => entries.Find(entry => entry.id == id);
}
