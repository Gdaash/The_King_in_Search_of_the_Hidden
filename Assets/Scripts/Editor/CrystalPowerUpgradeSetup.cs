#if UNITY_EDITOR
using System.Linq;
using GameFoundation.Localization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class CrystalPowerUpgradeSetup
{
    private const string TreePath = "Assets/Prefabs/Base/Laboratory Skill Tree.prefab";
    public static void Install()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play first");
        var table = Resources.Load<ScientificUpgradeTable>("ScientificUpgradeTable");
        EnsureDefinitions(table);
        EditorUtility.SetDirty(table); AssetDatabase.SaveAssetIfDirty(table);
        LaboratoryListSetup.Install();
        var localization = Resources.Load<LocalizationTable>("Localization/Base Localization");
        for (int i = 0; i < ScientificUpgrades.CrystalPower.Length; i++)
        {
            var entry = table.Find(ScientificUpgrades.CrystalPower[i]);
            Translate(localization, "skill." + entry.id + ".title", entry.title, "Crystal power " + (i + 1));
            Translate(localization, "skill." + entry.id + ".description", entry.description,
                "Adds 20% of base crystal charging power. Power is shared equally by cells that are currently recharging. Bonuses add together; five upgrades give +100% power.");
        }
        EditorUtility.SetDirty(localization); AssetDatabase.SaveAssetIfDirty(localization);
        Debug.Log("Installed five crystal charging power upgrades in the shared laboratory list.");
    }
    public static void EnsureDefinitions(ScientificUpgradeTable table)
    {
        var ore = Resources.Load<ResourceType>("ResourceTypes/MagicOre");
        for (int i = 0; i < ScientificUpgrades.CrystalPower.Length; i++)
        {
            string id = ScientificUpgrades.CrystalPower[i];
            if (table.Find(id) != null) continue;
            table.entries.Add(new ScientificUpgradeTable.Entry {
                id = id, parentId = i == 0 ? ScientificUpgrades.PortalArrows : ScientificUpgrades.CrystalPower[i - 1],
                title = "Мощность кристалла " + (i + 1),
                description = "Добавляет 20% базовой мощности зарядки кристалла. Мощность поровну делится между заряжающимися ячейками. Бонусы складываются: пять улучшений дают +100% мощности.",
                costResource = ore, cost = (i + 1) * 5, effectValue = .2f
            });
        }
    }
    public static void AddBranch(GameObject tree, ScientificUpgradeTable table)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/SkillButton.prefab");
        var arrowSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/ArrowSkills.prefab");
        var stats = Resources.Load<GlobalStats>("Global/globalHexStats");
        var parent = tree.GetComponentsInChildren<SkillButton>(true).Single(s => s.skillID == ScientificUpgrades.PortalArrows);
        var iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Evolution adventure/Sprites/UI/Sprites/Gameplay/Sprites/NewUi/iconEnergy.png");
        for (int i = 0; i < ScientificUpgrades.CrystalPower.Length; i++)
        {
            string id = ScientificUpgrades.CrystalPower[i];
            var skill = tree.GetComponentsInChildren<SkillButton>(true).FirstOrDefault(s => s.skillID == id);
            if (skill == null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(source, tree.transform);
                skill = instance.GetComponent<SkillButton>();
                var rect = instance.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                rect.anchoredPosition = new Vector2(270, -230 * (i + 1));
                rect.localScale = Vector3.one * 1.5f;
            }
            var entry = table.Find(id);
            skill.name = id + " " + entry.title; skill.skillID = id; skill.title = entry.title; skill.description = entry.description;
            skill.cost = entry.cost; skill.isPurchased = skill.isUnlocked = false;
            var so = new SerializedObject(skill);
            so.FindProperty("purchaseResourceType").objectReferenceValue = entry.costResource;
            so.FindProperty("upgradeStats").objectReferenceValue = stats; so.ApplyModifiedPropertiesWithoutUndo();
            var icon = skill.transform.Find("CostIconBacking/CostResourceIcon").GetComponent<Image>();
            ResourceIconSizing.Apply(icon, entry.costResource.resourceIcon);
            var upgradeIcon = skill.transform.Find("UpgradeIcon").GetComponent<Image>();
            upgradeIcon.sprite = iconSprite; upgradeIcon.preserveAspect = true; upgradeIcon.enabled = true;
            upgradeIcon.rectTransform.sizeDelta = iconSprite.rect.size * 2;
            if (!parent.nextSkills.Contains(skill)) parent.nextSkills = parent.nextSkills.Append(skill).ToArray();
            string arrowName = "Arrow to " + id;
            if (tree.transform.Find(arrowName) == null)
            {
                var arrow = (GameObject)PrefabUtility.InstantiatePrefab(arrowSource, tree.transform);
                arrow.name = arrowName; arrow.transform.SetAsFirstSibling();
                var rect = arrow.GetComponent<RectTransform>();
                Vector2 from = parent.GetComponent<RectTransform>().anchoredPosition;
                Vector2 to = skill.GetComponent<RectTransform>().anchoredPosition;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                rect.anchoredPosition = (from + to) * .5f;
                rect.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg + 180);
                var image = arrow.GetComponent<Image>(); image.preserveAspect = true; image.raycastTarget = false;
                rect.sizeDelta = image.sprite.rect.size * 2;
                var line = new SerializedObject(arrow.GetComponent<SkillLine>());
                line.FindProperty("parentSkill").objectReferenceValue = parent; line.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.RecordPrefabInstancePropertyModifications(arrow.GetComponent<SkillLine>());
                PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(skill);
            PrefabUtility.RecordPrefabInstancePropertyModifications(parent);
            PrefabUtility.RecordPrefabInstancePropertyModifications(icon);
            PrefabUtility.RecordPrefabInstancePropertyModifications(icon.rectTransform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(upgradeIcon);
            PrefabUtility.RecordPrefabInstancePropertyModifications(upgradeIcon.rectTransform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(skill.transform);
            parent = skill;
        }
    }
    private static void Translate(LocalizationTable table, string key, string ru, string en)
    {
        var entry = table.entries.Find(x => x.key == key);
        if (entry == null) { entry = new LocalizationTable.Entry { key = key }; table.entries.Add(entry); }
        entry.values = new() { ru, en };
    }
}
#endif
