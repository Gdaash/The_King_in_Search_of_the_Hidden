using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "NewGlobalStats", menuName = "Game/Global Stats")]
public class GlobalStats : ScriptableObject
{
    public string unitTypeKey; // Уникальный ID для сохранения (напр. "Archer", "Barracks")

    [System.Serializable]
    public class DamageInfo
    {
        public DamageType type;
        public float baseDamage;
        public float bonusDamage;
        public float TotalDamage => baseDamage + bonusDamage;
    }

    [System.Serializable]
    public class ResistanceInfo
    {
        public DamageType type;
        [Range(0, 2)] public float baseMult = 1f; 
        public float bonusResist = 0f; 
        public float CurrentMult => Mathf.Clamp(baseMult - bonusResist, 0, 2);
    }

    [Header("Здоровье")]
    public float baseMaxHealth = 100f;
    public float bonusHealth = 0f;
    public float TotalMaxHealth => baseMaxHealth + bonusHealth;

    [Header("Сопротивления (Резисты)")]
    public List<ResistanceInfo> resistances = new List<ResistanceInfo>();

    [Header("Передвижение")]
    public float baseSpeed = 3f;
    public float bonusSpeed = 0f;
    public float TotalSpeed => baseSpeed + bonusSpeed;

    [Header("Атака")]
    public float baseAttackCooldown = 1.5f;
    public float bonusAttackSpeed = 0f; 
    public float TotalCooldown => Mathf.Max(0.1f, baseAttackCooldown - bonusAttackSpeed);

    public float baseAttackRange = 10f;
    public float bonusAttackRange = 0f;
    public float TotalAttackRange => baseAttackRange + bonusAttackRange;

    [Header("Производство (Таймеры)")]
    public float baseProductionTime = 5f;
    public float bonusProductionSpeed = 0f; 
    public float TotalProductionTime => Mathf.Max(0.2f, baseProductionTime - bonusProductionSpeed);

    [Header("Урон (Список)")]
    public List<DamageInfo> damageSettings = new List<DamageInfo>();

    public event Action OnStatsUpdated;

    // --- МЕТОДЫ ДЛЯ UNITY EVENTS (КНОПОК) ---
    // Вспомогательные методы с одним аргументом для инспектора

    public void AddPhysicalDamage(float amt) => AddDamageUpgrade(DamageType.Physical, amt);
    public void AddFireDamage(float amt) => AddDamageUpgrade(DamageType.Fire, amt);
    public void AddMagicDamage(float amt) => AddDamageUpgrade(DamageType.Magic, amt);

    public void AddPhysicalResist(float amt) => AddResistUpgrade(DamageType.Physical, amt);
    public void AddFireResist(float amt) => AddResistUpgrade(DamageType.Fire, amt);
    public void AddMagicResist(float amt) => AddResistUpgrade(DamageType.Magic, amt);
    

    // --- ОСНОВНАЯ ЛОГИКА ЗАГРУЗКИ И СОХРАНЕНИЯ ---

    public void LoadStats()
    {
        if (string.IsNullOrEmpty(unitTypeKey)) return;

        bonusHealth = PlayerPrefs.GetFloat(unitTypeKey + "_BonusHP", 0f);
        bonusSpeed = PlayerPrefs.GetFloat(unitTypeKey + "_BonusSpeed", 0f);
        bonusAttackSpeed = PlayerPrefs.GetFloat(unitTypeKey + "_BonusAtkSpeed", 0f);
        bonusAttackRange = PlayerPrefs.GetFloat(unitTypeKey + "_BonusRange", 0f);
        bonusProductionSpeed = PlayerPrefs.GetFloat(unitTypeKey + "_BonusProdSpeed", 0f);

        foreach (var d in damageSettings)
            d.bonusDamage = PlayerPrefs.GetFloat(unitTypeKey + "_BonusDmg_" + d.type.ToString(), 0f);

        foreach (var r in resistances)
            r.bonusResist = PlayerPrefs.GetFloat(unitTypeKey + "_BonusRes_" + r.type.ToString(), 0f);
        
        OnStatsUpdated?.Invoke();
    }

    public void AddHealthUpgrade(float amount) => SaveValue(ref bonusHealth, "_BonusHP", amount);
    public void AddSpeedUpgrade(float amount) => SaveValue(ref bonusSpeed, "_BonusSpeed", amount);
    public void AddAttackSpeedUpgrade(float amount) => SaveValue(ref bonusAttackSpeed, "_BonusAtkSpeed", amount);
    public void AddRangeUpgrade(float amount) => SaveValue(ref bonusAttackRange, "_BonusRange", amount);
    public void AddProductionSpeedUpgrade(float amount) => SaveValue(ref bonusProductionSpeed, "_BonusProdSpeed", amount);

    public void AddDamageUpgrade(DamageType type, float amount)
    {
        var d = damageSettings.FirstOrDefault(x => x.type == type);
        if (d != null) {
            d.bonusDamage += amount;
            PlayerPrefs.SetFloat(unitTypeKey + "_BonusDmg_" + type.ToString(), d.bonusDamage);
            PlayerPrefs.Save();
            OnStatsUpdated?.Invoke();
        }
    }

    public void AddResistUpgrade(DamageType type, float amount)
    {
        var r = resistances.FirstOrDefault(x => x.type == type);
        if (r != null) {
            r.bonusResist += amount;
            PlayerPrefs.SetFloat(unitTypeKey + "_BonusRes_" + r.type.ToString(), r.bonusResist);
            PlayerPrefs.Save();
            OnStatsUpdated?.Invoke();
        }
    }

    private void SaveValue(ref float field, string subKey, float amount)
    {
        field += amount;
        if (!string.IsNullOrEmpty(unitTypeKey)) {
            PlayerPrefs.SetFloat(unitTypeKey + subKey, field);
            PlayerPrefs.Save();
            OnStatsUpdated?.Invoke();
        }
    }

    [ContextMenu("Reset Progress")]
    public void ResetProgress()
    {
        bonusHealth = 0;
        bonusSpeed = 0;
        bonusAttackSpeed = 0;
        bonusAttackRange = 0;
        bonusProductionSpeed = 0;

        if (!string.IsNullOrEmpty(unitTypeKey))
        {
            PlayerPrefs.DeleteKey(unitTypeKey + "_BonusHP");
            PlayerPrefs.DeleteKey(unitTypeKey + "_BonusSpeed");
            PlayerPrefs.DeleteKey(unitTypeKey + "_BonusAtkSpeed");
            PlayerPrefs.DeleteKey(unitTypeKey + "_BonusRange");
            PlayerPrefs.DeleteKey(unitTypeKey + "_BonusProdSpeed");

            foreach (var d in damageSettings)
            {
                d.bonusDamage = 0;
                PlayerPrefs.DeleteKey(unitTypeKey + "_BonusDmg_" + d.type.ToString());
            }

            foreach (var r in resistances)
            {
                r.bonusResist = 0;
                PlayerPrefs.DeleteKey(unitTypeKey + "_BonusRes_" + r.type.ToString());
            }
        }

        PlayerPrefs.Save();
        OnStatsUpdated?.Invoke();
    }
}
