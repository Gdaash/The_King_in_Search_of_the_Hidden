using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFoundation.Base
{
    [CreateAssetMenu(menuName = "Those UnderHex/Building upgrades")]
    public sealed class BuildingUpgradeCatalog : ScriptableObject
    {
        [Serializable] public sealed class Level
        {
            [Min(0)] public int additionalCapacity = 1;
            public ResourceType resourceA;
            [Min(0)] public int costA = 1;
            public ResourceType resourceB;
            [Min(0)] public int costB = 1;
        }
        [Serializable] public sealed class Building
        {
            public string id;
            public string nameKey;
            public string displayName;
            public ResourceType capacityResource;
            [Min(0)] public int capacityBeforeConstruction;
            [Min(0)] public int constructionCapacity = 3;
            public List<Level> levels = new();
            public int Capacity(bool built, int level)
            {
                int result = capacityBeforeConstruction;
                if (!built) return result;
                result += constructionCapacity;
                for (int i = 0; i < Mathf.Clamp(level, 0, levels.Count); i++) result += levels[i].additionalCapacity;
                return result;
            }
        }
        public List<Building> buildings = new();
        public Building Find(string id) => buildings.Find(b => b.id == id);
    }
}
