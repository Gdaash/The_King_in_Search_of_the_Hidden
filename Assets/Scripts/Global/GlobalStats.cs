using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "NewGlobalStats", menuName = "Game/Global Stats")]
public class GlobalStats : ScriptableObject
{
    public string unitTypeKey; 

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

    [Header("Здоровье и Регенерация")]
    public float baseMaxHealth = 100f;
    public float bonusHealth = 0f;
    public float TotalMaxHealth => baseMaxHealth + bonusHealth;

    public float baseRegenAmount = 5f;
    public float bonusRegenAmount = 0f;
    public float TotalRegenAmount => baseRegenAmount + bonusRegenAmount;

    public float baseRegenDelay = 5f;
    public float bonusRegenDelayReduction = 0f; 
    public float TotalRegenDelay => Mathf.Max(0.5f, baseRegenDelay - bonusRegenDelayReduction);

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

    [Header("Мировые настройки (Difficulty)")]
    public float baseDifficultyMultiplier = 120f;
    public float bonusDifficultyReduction = 0f;
    public float TotalDifficultyMultiplier => baseDifficultyMultiplier + bonusDifficultyReduction;

    [Header("Урон (Список)")]
    public List<DamageInfo> damageSettings = new List<DamageInfo>();

    [Header("Гексы (Контент)")]
    public List<string> unlockedHexContentIDs = new List<string>();

    public event Action OnStatsUpdated;

    // --- МЕТОДЫ ДЛЯ UNITY EVENTS (КНОПОК) ---
    public void AddPhysicalDamage(float amt) => AddDamageUpgrade(DamageType.Physical, amt);
    public void AddFireDamage(float amt) => AddDamageUpgrade(DamageType.Fire, amt);
    public void AddMagicDamage(float amt) => AddDamageUpgrade(DamageType.Magic, amt);
    public void AddPhysicalResist(float amt) => AddResistUpgrade(DamageType.Physical, amt);
    public void AddFireResist(float amt) => AddResistUpgrade(DamageType.Fire, amt);
    public void AddMagicResist(float amt) => AddResistUpgrade(DamageType.Magic, amt);

    // --- ЛОГИКА УЛУЧШЕНИЙ ---
    public void AddHealthUpgrade(float amount) => SaveValue(ref bonusHealth, "_BonusHP", amount);
    public void AddRegenAmountUpgrade(float amount) => SaveValue(ref bonusRegenAmount, "_BonusRegenAmt", amount);
    public void AddRegenDelayUpgrade(float amount) => SaveValue(ref bonusRegenDelayReduction, "_BonusRegenDelay", amount);
    public void AddSpeedUpgrade(float amount) => SaveValue(ref bonusSpeed, "_BonusSpeed", amount);
    public void AddAttackSpeedUpgrade(float amount) => SaveValue(ref bonusAttackSpeed, "_BonusAtkSpeed", amount);
    public void AddRangeUpgrade(float amount) => SaveValue(ref bonusAttackRange, "_BonusRange", amount);
    public void AddProductionSpeedUpgrade(float amount) => SaveValue(ref bonusProductionSpeed, "_BonusProdSpeed", amount);

    public void AddDifficultyMultiplierUpgrade(float amount)
    {
        bonusDifficultyReduction += amount;
        if (!string.IsNullOrEmpty(unitTypeKey))
        {
            PlayerPrefs.SetFloat(unitTypeKey + "_DifficultyBonus", bonusDifficultyReduction);
            PlayerPrefs.Save();
        }
        SyncDifficulty();
        OnStatsUpdated?.Invoke();
    }

    private void SyncDifficulty()
    {
        GlobalSettings.DifficultyTimerMultiplier = TotalDifficultyMultiplier;
    }

    // --- ЛОГИКА ГЕКСОВ ---
    public void UnlockHexContent(string contentID)
    {
        if (string.IsNullOrEmpty(contentID)) return;
        if (!unlockedHexContentIDs.Contains(contentID))
        {
            unlockedHexContentIDs.Add(contentID);
            SaveHexList();
        }
    }

    public void LockHexContent(string contentID)
    {
        if (string.IsNullOrEmpty(contentID)) return;
        if (unlockedHexContentIDs.Contains(contentID))
        {
            unlockedHexContentIDs.Remove(contentID);
            SaveHexList();
        }
    }

    private void SaveHexList()
    {
        if (string.IsNullOrEmpty(unitTypeKey)) return;
        if (unlockedHexContentIDs.Count > 0)
            PlayerPrefs.SetString(unitTypeKey + "_UnlockedHex", string.Join(",", unlockedHexContentIDs));
        else
            PlayerPrefs.DeleteKey(unitTypeKey + "_UnlockedHex");

        PlayerPrefs.Save();
        OnStatsUpdated?.Invoke();
    }

    // --- ЗАГРУЗКА И СОХРАНЕНИЕ ---
    public void LoadStats()
    {
        if (string.IsNullOrEmpty(unitTypeKey)) return;

        bonusHealth = PlayerPrefs.GetFloat(unitTypeKey + "_BonusHP", 0f);
        bonusRegenAmount = PlayerPrefs.GetFloat(unitTypeKey + "_BonusRegenAmt", 0f);
        bonusRegenDelayReduction = PlayerPrefs.GetFloat(unitTypeKey + "_BonusRegenDelay", 0f);
        bonusSpeed = PlayerPrefs.GetFloat(unitTypeKey + "_BonusSpeed", 0f);
        bonusAttackSpeed = PlayerPrefs.GetFloat(unitTypeKey + "_BonusAtkSpeed", 0f);
        bonusAttackRange = PlayerPrefs.GetFloat(unitTypeKey + "_BonusRange", 0f);
        bonusProductionSpeed = PlayerPrefs.GetFloat(unitTypeKey + "_BonusProdSpeed", 0f);
        bonusDifficultyReduction = PlayerPrefs.GetFloat(unitTypeKey + "_DifficultyBonus", 0f);

        foreach (var d in damageSettings)
            d.bonusDamage = PlayerPrefs.GetFloat(unitTypeKey + "_BonusDmg_" + d.type.ToString(), 0f);

        foreach (var r in resistances)
            r.bonusResist = PlayerPrefs.GetFloat(unitTypeKey + "_BonusRes_" + r.type.ToString(), 0f);

        string savedHex = PlayerPrefs.GetString(unitTypeKey + "_UnlockedHex", "");
        if (!string.IsNullOrEmpty(savedHex))
            unlockedHexContentIDs = savedHex.Split(',').ToList();
        else
            unlockedHexContentIDs.Clear();
        
        SyncDifficulty();
        OnStatsUpdated?.Invoke();
    }

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

    public void DeleteDataByID(string keyToDelete)
    {
        if (PlayerPrefs.HasKey(keyToDelete))
        {
            PlayerPrefs.DeleteKey(keyToDelete);
            PlayerPrefs.Save();
            LoadStats(); 
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
        bonusHealth = 0; bonusRegenAmount = 0; bonusRegenDelayReduction = 0;
        bonusSpeed = 0; bonusAttackSpeed = 0; bonusAttackRange = 0; 
        bonusProductionSpeed = 0; bonusDifficultyReduction = 0;
        unlockedHexContentIDs.Clear();

        if (!string.IsNullOrEmpty(unitTypeKey))
        {
            PlayerPrefs.DeleteKey(unitTypeKey + "_BonusHP");
            PlayerPrefs.DeleteKey(unitTypeKey + "_BonusRegenAmt");
            PlayerPrefs.DeleteKey(unitTypeKey + "_BonusRegenDelay");
            PlayerPrefs.DeleteKey(unitTypeKey + "_BonusSpeed");
            PlayerPrefs.DeleteKey(unitTypeKey + "_BonusAtkSpeed");
            PlayerPrefs.DeleteKey(unitTypeKey + "_BonusRange");
            PlayerPrefs.DeleteKey(unitTypeKey + "_BonusProdSpeed");
            PlayerPrefs.DeleteKey(unitTypeKey + "_DifficultyBonus");
            PlayerPrefs.DeleteKey(unitTypeKey + "_UnlockedHex");

            foreach (var d in damageSettings) PlayerPrefs.DeleteKey(unitTypeKey + "_BonusDmg_" + d.type.ToString());
            foreach (var r in resistances) PlayerPrefs.DeleteKey(unitTypeKey + "_BonusRes_" + r.type.ToString());
        }

        PlayerPrefs.Save();
        SyncDifficulty();
        OnStatsUpdated?.Invoke();
    }
}
