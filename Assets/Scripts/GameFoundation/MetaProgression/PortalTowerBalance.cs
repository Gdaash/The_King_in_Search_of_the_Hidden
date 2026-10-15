using System;
using System.Collections.Generic;
using UnityEngine;
using GameFoundation.Combat;

namespace GameFoundation.MetaProgression
{
    [CreateAssetMenu(menuName = "Game Foundation/Portal Tower Balance")]
    public sealed class PortalTowerBalance : ScriptableObject
    {
        public enum Weapon { None, Bolts, Beam, Rings, Lightning, FrostSeal, FireShot, ElectricOrb }
        public enum DeliveryMethod { Projectile, Area, InstantHit }
        public enum UpgradeScope { AllWeapons, DeliveryMethod, DamageType, Portal }
        public enum Effect { UnlockAttack, ExtraLight, Damage, AttackSpeed, Projectiles, Range, UnlockWeapon, Area, Duration, ExpansionSpeed, Targets, SlowStrength, TickInterval, AttackInterval }
        public enum Quality { Common, Rare, Legendary }
        public const float MinimumInterval = .1f;
        public static Quality RollQuality(float roll) => roll < .6f ? Quality.Common : roll < .9f ? Quality.Rare : Quality.Legendary;
        [Serializable]
        public sealed class WeaponDefinition
        {
            public Weapon weapon;
            public DeliveryMethod deliveryMethod;
            public bool disabled;
            public string title, englishTitle;
            [TextArea] public string description, englishDescription;
            public Sprite icon;
            public CombatWeapon combat;
            [Min(.01f)] public float duration=.16f;
            [Min(0)] public float damageTickInterval;
            public bool continuous;
            public bool HasTicks => damageTickInterval > 0;
            public bool HasDuration => HasTicks && !continuous && deliveryMethod == DeliveryMethod.Area;
            [Range(0, .8f)] public float slowFraction;
            [Tooltip("Смещение центра области по ходу движения цели, в долях базового радиуса.")]
            [Range(0, .5f)] public float placementRadiusFraction = .25f;
            public GameObject projectilePrefab;
            [Min(.01f)] public float projectileSpeedMultiplier = 1;
            [Min(.1f)] public float projectileScale = 1;
            [Range(0, 90)] public float spreadAngle;
            public float damage => combat!=null?combat.damage:0;
            public float cooldown => combat!=null?combat.interval:1;
            public float range => combat!=null?combat.range:0;
            public float radius => combat!=null?combat.radius:0;
            public float expansionSpeed => combat!=null?combat.expansionSpeed:3;
            public int count => combat!=null?combat.count:1;
            public DamageType damageType => combat!=null?combat.damageType:DamageType.Magic;
        }
        [Serializable]
        public sealed class Upgrade
        {
            public string id, title, englishTitle;
            [TextArea(2, 5)] public string description, englishDescription;
            public Sprite icon;
            public Effect effect;
            [Tooltip("Weapon используется только при получении нового оружия. Остальные улучшения выбирают общую категорию ниже.")]
            public Weapon weapon;
            public UpgradeScope scope;
            public DeliveryMethod deliveryMethod;
            public DamageType damageType;
            [Min(0)] public float value = 1;
            [Tooltip("0 — можно улучшать без ограничения.")][Min(0)] public int maximumRank = 1;
            public bool requiresAttack;
            public bool hasQuality;
            [Min(0)] public float rareValue, legendaryValue;
            [NonSerialized] public Quality quality;
            public float OfferedValue => !hasQuality || quality == Quality.Common ? value : quality == Quality.Rare ? rareValue : legendaryValue;
            public Upgrade CreateOffer(Quality selectedQuality)
            {
                var offer = (Upgrade)MemberwiseClone();
                offer.quality = selectedQuality;
                return offer;
            }
        }
        [Range(1, 32)] public int minimumCrystalsPerEmptyHex = 1;
        [Range(1, 32)] public int maximumCrystalsPerEmptyHex = 5;
        [Min(1)] public int experiencePerEnemy = 1;
        [Min(1)] public int firstLevelExperience = 12;
        [Min(0)] public int experienceIncreasePerLevel = 6;
        public Sprite levelPointIcon;
        public List<Upgrade> upgrades = new();
        public List<WeaponDefinition> weapons = new();
        public WeaponDefinition FindWeapon(Weapon weapon) => weapons.Find(w => w.weapon == weapon);
        public bool IsWeaponEnabled(Weapon weapon) => weapon==Weapon.None || (FindWeapon(weapon)!=null && !FindWeapon(weapon).disabled);
        public int RequiredExperience(int level) => Mathf.Max(1, firstLevelExperience + (Mathf.Max(1, level) - 1) * experienceIncreasePerLevel);
        public Upgrade Find(string id) => upgrades.Find(u => u.id == id);
    }
}
