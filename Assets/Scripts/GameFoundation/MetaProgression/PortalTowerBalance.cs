using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFoundation.MetaProgression
{
    [CreateAssetMenu(menuName = "Game Foundation/Portal Tower Balance")]
    public sealed class PortalTowerBalance : ScriptableObject
    {
        public enum Weapon { None, Bolts, Beam, Rings, Lightning }
        public enum Effect { UnlockAttack, ExtraLight, Damage, AttackSpeed, Projectiles, Range, UnlockWeapon, Area, Duration, ExpansionSpeed }
        [Serializable]
        public sealed class WeaponDefinition
        {
            public Weapon weapon;
            public string title, englishTitle;
            [TextArea] public string description, englishDescription;
            public Sprite icon;
            [Min(.01f)] public float damage = 10, cooldown = 1, range = 6, radius = .75f, duration = 1, expansionSpeed = 3;
            [Min(1)] public int count = 1;
        }
        [Serializable]
        public sealed class Upgrade
        {
            public string id, title, englishTitle;
            [TextArea(2, 5)] public string description, englishDescription;
            public Sprite icon;
            public Effect effect;
            public Weapon weapon;
            [Min(0)] public float value = 1;
            [Tooltip("0 — можно улучшать без ограничения.")][Min(0)] public int maximumRank = 1;
            public bool requiresAttack;
        }
        [Min(1)] public int experiencePerHex = 3;
        [Min(1)] public int experiencePerEnemy = 1;
        [Min(1)] public int firstLevelExperience = 12;
        [Min(0)] public int experienceIncreasePerLevel = 6;
        public Sprite levelPointIcon;
        public List<Upgrade> upgrades = new();
        public List<WeaponDefinition> weapons = new();
        public WeaponDefinition FindWeapon(Weapon weapon) => weapons.Find(w => w.weapon == weapon);
        public int RequiredExperience(int level) => Mathf.Max(1, firstLevelExperience + (Mathf.Max(1, level) - 1) * experienceIncreasePerLevel);
        public Upgrade Find(string id) => upgrades.Find(u => u.id == id);
    }
}
