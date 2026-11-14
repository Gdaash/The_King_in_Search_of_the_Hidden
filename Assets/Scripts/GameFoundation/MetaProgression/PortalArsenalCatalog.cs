using System;
using System.Collections.Generic;
using UnityEngine;
namespace GameFoundation.MetaProgression
{
    [CreateAssetMenu(menuName = "Game Foundation/Portal Arsenal")]
    public sealed class PortalArsenalCatalog : ScriptableObject
    {
        public enum Requirement { Starting, EnemyKills, Population, ExploredHexes, TowerLevel, Resource }
        [Serializable] public sealed class Entry
        {
            public PortalTowerBalance.Weapon weapon;
            public Requirement requirement;
            public string enemyId;
            [Min(1)] public int amount = 1;
            public ResourceType resource;
            [Min(0)] public int requiredPortalLevel;
            public PortalTowerBalance.Weapon requiredWeapon;
            [Min(0)] public int requiredEnhancements;
            public List<PortalWeaponChallengeTable.KillRequirement> requiredKills = new();
        }
        public List<Entry> entries = new();
        public Entry Find(PortalTowerBalance.Weapon weapon) => entries.Find(e => e.weapon == weapon);
    }
}
