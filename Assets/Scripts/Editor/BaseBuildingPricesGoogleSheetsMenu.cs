#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using GameFoundation.Base;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Synchronizes construction prices from the Base scene with the project's Google Sheet.</summary>
public static class BaseBuildingPricesGoogleSheetsMenu
{
    private const string SheetId = "1eepDmDSn5Y-Bs6qj49qZV8EjP1zo-pi58c8EeYKflyg";
    private const string SheetName = "BaseBuildings";
    private const string ScenePath = "Assets/Scenes/Base.unity";
    private const string LegacyHeader = "building_id,building_name,resource_type,cost_amount";
    private const string Header = LegacyHeader + ",upgrade_level,capacity_bonus";

    [MenuItem("Tools/Таблицы/Строения базы/Экспорт в Google Sheets")]
    private static void ExportToGoogleSheets()
    {
        EditorGUIUtility.systemCopyBuffer = ToTabSeparated(BuildCsv());
        Application.OpenURL($"https://docs.google.com/spreadsheets/d/{SheetId}/edit");
        EditorUtility.DisplayDialog("Экспорт строений базы",
            $"Цены скопированы. Откройте вкладку {SheetName}, выберите A1 и вставьте данные.", "OK");
    }

    [MenuItem("Tools/Таблицы/Строения базы/Импорт из Google Sheets")]
    private static async void ImportFromGoogleSheets()
    {
        try
        {
            using var client = new HttpClient();
            string url = $"https://docs.google.com/spreadsheets/d/{SheetId}/gviz/tq?tqx=out:csv&sheet={Uri.EscapeDataString(SheetName)}";
            string content = await client.GetStringAsync(url);
            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidDataException($"Вкладка {SheetName} пуста или недоступна.");
            Import(content);
        }
        catch (Exception error)
        {
            EditorUtility.DisplayDialog("Импорт строений базы", error.Message, "OK");
        }
    }

    public static string BuildCsv()
    {
        var rows = new List<string> { Header };
        foreach (BaseBuildingConstruction building in LoadBuildings().OrderBy(item => Id(item), StringComparer.Ordinal))
        {
            var serialized = new SerializedObject(building);
            AddRow(rows, Id(serialized), DisplayName(serialized),
                serialized.FindProperty("wood").objectReferenceValue as ResourceType,
                serialized.FindProperty("woodCost").intValue);
            AddRow(rows, Id(serialized), DisplayName(serialized),
                serialized.FindProperty("stone").objectReferenceValue as ResourceType,
                serialized.FindProperty("stoneCost").intValue);
        }
        var catalog = BuildingUpgradeService.Catalog;
        if (catalog != null)
            foreach (var building in catalog.buildings)
                for (int i = 0; i < building.levels.Count; i++)
                {
                    var level = building.levels[i];
                    if (level.resourceA != null) rows.Add(Row(building.id, building.displayName, level.resourceA.Id, level.costA.ToString(), (i + 1).ToString(), level.additionalCapacity.ToString()));
                    if (level.resourceB != null) rows.Add(Row(building.id, building.displayName, level.resourceB.Id, level.costB.ToString(), (i + 1).ToString(), level.additionalCapacity.ToString()));
                }
        return string.Join("\r\n", rows);
    }

    public static void Import(string csv, bool showDialog = true, bool validateOnly = false)
    {
        List<List<string>> rows = Parse(csv);
        if (rows.Count == 0 || (!rows[0].SequenceEqual(Header.Split(',')) && !rows[0].SequenceEqual(LegacyHeader.Split(','))))
            throw new InvalidDataException("Нужны столбцы: " + Header);

        var prices = new Dictionary<string, List<(ResourceType resource, int amount)>>(StringComparer.Ordinal);
        var upgrades = new Dictionary<(string id, int level), List<(ResourceType resource, int amount, int bonus)>>();
        foreach (List<string> cells in rows.Skip(1))
        {
            if (cells.Count < 4 || string.IsNullOrWhiteSpace(cells[0])) continue;
            ResourceType resource = FindResource(cells[2]);
            if (resource == null) throw new InvalidDataException("Неизвестный ресурс: " + cells[2]);
            if (!int.TryParse(cells[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int amount) || amount < 0)
                throw new InvalidDataException($"Некорректная цена для {cells[0]}: {cells[3]}");
            int level = 0;
            if (cells.Count > 4 && (!int.TryParse(cells[4], out level) || level < 0))
                throw new InvalidDataException("Некорректный upgrade_level: " + cells[4]);
            if (level > 0)
            {
                if (cells.Count < 6 || !int.TryParse(cells[5], out int bonus) || bonus < 0)
                    throw new InvalidDataException("Некорректный capacity_bonus для " + cells[0]);
                var definition = BuildingUpgradeService.Catalog?.Find(cells[0]);
                if (definition == null || level > definition.levels.Count)
                    throw new InvalidDataException("Неизвестное улучшение: " + cells[0] + " / " + level);
                var key = (cells[0], level);
                if (!upgrades.TryGetValue(key, out var upgrade)) upgrades.Add(key, upgrade = new());
                upgrade.Add((resource, amount, bonus));
                continue;
            }
            if (!prices.TryGetValue(cells[0], out var costs)) prices.Add(cells[0], costs = new List<(ResourceType, int)>());
            costs.Add((resource, amount));
        }

        BaseBuildingConstruction[] buildings = LoadBuildings();
        var byId = buildings.ToDictionary(Id, StringComparer.Ordinal);
        foreach (string id in prices.Keys)
            if (!byId.ContainsKey(id)) throw new InvalidDataException("Строение отсутствует на Base: " + id);
        foreach (string id in byId.Keys)
            if (!prices.ContainsKey(id)) throw new InvalidDataException("В таблице нет цен строения: " + id);

        // Validate the complete input before changing any asset or scene.
        foreach (var pair in prices)
            if (pair.Value.Count != 2 || pair.Value[0].resource == pair.Value[1].resource)
                throw new InvalidDataException("Нужны две разные цены: " + pair.Key);
        foreach (var pair in upgrades)
            if (pair.Value.Count < 1 || pair.Value.Count > 2 || pair.Value.Count == 2 &&
                (pair.Value[0].resource == pair.Value[1].resource || pair.Value[0].bonus != pair.Value[1].bonus))
                throw new InvalidDataException("Для улучшения нужны одна или две строки с одинаковым capacity_bonus: " + pair.Key);
        if (rows[0].Count > 4)
            foreach (var building in BuildingUpgradeService.Catalog.buildings)
                for (int i = 1; i <= building.levels.Count; i++)
                    if (!upgrades.ContainsKey((building.id, i)) &&
                        // Sheets exported before portal progression have no portal rows yet. Keep its Inspector prices.
                        !(building.effect == BuildingUpgradeCatalog.UpgradeEffect.PortalAccess && !upgrades.Keys.Any(k => k.id == building.id)))
                        throw new InvalidDataException("Нет цены улучшения " + building.id + " / " + i);
        if (validateOnly) return;
        foreach (var pair in upgrades)
        {
            var target = BuildingUpgradeService.Catalog.Find(pair.Key.id).levels[pair.Key.level - 1];
            target.resourceA = pair.Value[0].resource; target.costA = pair.Value[0].amount;
            target.resourceB = pair.Value.Count > 1 ? pair.Value[1].resource : null; target.costB = pair.Value.Count > 1 ? pair.Value[1].amount : 0;
            target.additionalCapacity = pair.Value[0].bonus;
        }
        if (upgrades.Count > 0) EditorUtility.SetDirty(BuildingUpgradeService.Catalog);
        foreach (var pair in prices)
        {
            if (pair.Value.Count != 2)
                throw new InvalidDataException($"У {pair.Key} должно быть ровно две строки цен.");
            if (pair.Value[0].resource == pair.Value[1].resource)
                throw new InvalidDataException($"У {pair.Key} один и тот же ресурс указан дважды.");

            var serialized = new SerializedObject(byId[pair.Key]);
            serialized.FindProperty("wood").objectReferenceValue = pair.Value[0].resource;
            serialized.FindProperty("woodCost").intValue = pair.Value[0].amount;
            serialized.FindProperty("stone").objectReferenceValue = pair.Value[1].resource;
            serialized.FindProperty("stoneCost").intValue = pair.Value[1].amount;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var component = byId[pair.Key];
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            string prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(component);
            if (!string.IsNullOrEmpty(prefab))
                foreach (string field in new[] { "wood", "woodCost", "stone", "stoneCost" })
                    PrefabUtility.ApplyPropertyOverride(serialized.FindProperty(field), prefab, InteractionMode.AutomatedAction);
            EditorUtility.SetDirty(component);
        }

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        if (showDialog) EditorUtility.DisplayDialog("Импорт строений базы", $"Обновлены строения: {prices.Count}, улучшения: {upgrades.Count}.", "OK");
    }

    private static BaseBuildingConstruction[] LoadBuildings()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                throw new OperationCanceledException("Импорт/экспорт отменён.");
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
        BaseBuildingConstruction[] buildings = UnityEngine.Object.FindObjectsByType<BaseBuildingConstruction>(
            FindObjectsInactive.Include, FindObjectsSortMode.None).Where(b => b.gameObject.activeInHierarchy).ToArray();
        if (buildings.Length == 0) throw new InvalidDataException("На сцене Base не найдены строения.");
        if (buildings.GroupBy(Id).Any(group => string.IsNullOrWhiteSpace(group.Key) || group.Count() != 1))
            throw new InvalidDataException("У каждого строения Base должен быть уникальный идентификатор.");
        return buildings;
    }

    private static string Id(BaseBuildingConstruction building) => Id(new SerializedObject(building));
    private static string Id(SerializedObject building) => building.FindProperty("buildingId").stringValue;
    private static string DisplayName(SerializedObject building) => building.FindProperty("fallbackName").stringValue;
    private static ResourceType FindResource(string name) => AssetDatabase.FindAssets("t:ResourceType")
        .Select(AssetDatabase.GUIDToAssetPath)
        .Select(AssetDatabase.LoadAssetAtPath<ResourceType>)
        .FirstOrDefault(resource => resource != null && string.Equals(resource.Id, name, StringComparison.OrdinalIgnoreCase));
    private static void AddRow(List<string> rows, string id, string name, ResourceType resource, int amount) =>
        rows.Add(Row(id, name, resource != null ? resource.Id : "", amount.ToString(CultureInfo.InvariantCulture), "0", ""));
    private static string Row(params string[] cells) => string.Join(",", cells.Select(cell => "\"" + (cell ?? "").Replace("\"", "\"\"") + "\""));
    private static string ToTabSeparated(string csv) => string.Join("\n", Parse(csv).Select(row => string.Join("\t", row)));

    private static List<List<string>> Parse(string csv)
    {
        var rows = new List<List<string>>(); var row = new List<string>(); var value = new StringBuilder(); bool quoted = false;
        for (int i = 0; i < csv.Length; i++)
        {
            char c = csv[i];
            if (c == '\"' && quoted && i + 1 < csv.Length && csv[i + 1] == '\"') { value.Append('\"'); i++; }
            else if (c == '\"') quoted = !quoted;
            else if (c == ',' && !quoted) { row.Add(value.ToString()); value.Clear(); }
            else if ((c == '\n' || c == '\r') && !quoted)
            {
                if (c == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n') i++;
                row.Add(value.ToString()); value.Clear();
                if (row.Any(cell => cell.Length > 0)) rows.Add(row);
                row = new List<string>();
            }
            else value.Append(c);
        }
        row.Add(value.ToString());
        if (row.Any(cell => cell.Length > 0)) rows.Add(row);
        if (rows.Count > 0 && rows[0].Count > 0) rows[0][0] = rows[0][0].TrimStart('\ufeff');
        return rows;
    }
}
#endif
