using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using GameFoundation.Localization;
using UnityEditor;
using UnityEngine;

public static class GameTablesMenu
{
    private const string UpgradePath = "Assets/Resources/ScientificUpgradeTable.asset";
    private const string LocalizationPath = "Assets/Prefabs/Base/Base Localization.asset";
    private const string SheetId = "1eepDmDSn5Y-Bs6qj49qZV8EjP1zo-pi58c8EeYKflyg";
    private const string SheetCsvUrl = "https://docs.google.com/spreadsheets/d/" + SheetId + "/export?format=csv&gid=0";
    private const string UpgradeHeader = "id,parent,title_ru,description_ru,cost_resource,cost_amount,effect_value";

    [MenuItem("Tools/Таблицы/Улучшения/Экспорт CSV")]
    private static void ExportUpgrades()
    {
        var table = AssetDatabase.LoadAssetAtPath<ScientificUpgradeTable>(UpgradePath);
        if (table == null) { EditorUtility.DisplayDialog("Улучшения", "Таблица улучшений не найдена.", "OK"); return; }
        string path = EditorUtility.SaveFilePanel("Экспорт улучшений", "", "scientific_upgrades.csv", "csv");
        if (string.IsNullOrEmpty(path)) return;
        var rows = new List<string> { UpgradeHeader };
        foreach (var entry in table.entries)
            rows.Add(Row(entry.id, entry.parentId, entry.title, entry.description,
                entry.costResource != null ? entry.costResource.name : "", entry.cost.ToString(CultureInfo.InvariantCulture),
                entry.effectValue.ToString(CultureInfo.InvariantCulture)));
        File.WriteAllText(path, string.Join("\r\n", rows), new UTF8Encoding(true));
        EditorUtility.RevealInFinder(path);
    }

    [MenuItem("Tools/Таблицы/Улучшения/Скопировать для Google Sheets")]
    private static void CopyUpgradesForGoogle()
    {
        var table = AssetDatabase.LoadAssetAtPath<ScientificUpgradeTable>(UpgradePath);
        if (table == null) return;
        var rows = new List<string> { UpgradeHeader.Replace(',', '\t') };
        foreach (var entry in table.entries)
            rows.Add(string.Join("\t", new[] { entry.id, entry.parentId, entry.title, entry.description,
                entry.costResource != null ? entry.costResource.name : "",
                entry.cost.ToString(CultureInfo.InvariantCulture), entry.effectValue.ToString(CultureInfo.InvariantCulture) }));
        EditorGUIUtility.systemCopyBuffer = string.Join("\n", rows);
        Application.OpenURL("https://docs.google.com/spreadsheets/d/" + SheetId + "/edit?gid=0#gid=0");
        EditorUtility.DisplayDialog("Экспорт улучшений", "Данные скопированы. Вставьте их в ячейку A1 таблицы Google Sheets.", "OK");
    }

    [MenuItem("Tools/Таблицы/Улучшения/Импорт CSV")]
    private static void ImportUpgradesFile()
    {
        string path = EditorUtility.OpenFilePanel("Импорт улучшений", "", "csv");
        if (!string.IsNullOrEmpty(path)) ImportUpgrades(File.ReadAllText(path, Encoding.UTF8));
    }

    [MenuItem("Tools/Таблицы/Улучшения/Импорт из Google Sheets")]
    private static async void ImportUpgradesGoogle()
    {
        try
        {
            using var client = new System.Net.Http.HttpClient();
            var csv = await client.GetStringAsync(SheetCsvUrl);
            if (string.IsNullOrWhiteSpace(csv)) throw new InvalidDataException("Таблица Google Sheets пока пустая или не опубликована как CSV.");
            ImportUpgrades(csv);
        }
        catch (Exception error) { EditorUtility.DisplayDialog("Импорт улучшений", error.Message, "OK"); }
    }

    private static void ImportUpgrades(string csv)
    {
        var rows = Parse(csv);
        if (rows.Count == 0 || !rows[0].SequenceEqual(UpgradeHeader.Split(',')))
        { EditorUtility.DisplayDialog("Импорт улучшений", "Нужны столбцы: " + UpgradeHeader, "OK"); return; }
        var table = AssetDatabase.LoadAssetAtPath<ScientificUpgradeTable>(UpgradePath);
        if (table == null) throw new InvalidDataException("Таблица улучшений не найдена: " + UpgradePath);
        var entries = new List<ScientificUpgradeTable.Entry>();
        foreach (var cells in rows.Skip(1))
        {
            if (cells.Count < 7 || string.IsNullOrWhiteSpace(cells[0])) continue;
            var resource = AssetDatabase.FindAssets("t:ResourceType")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath<ResourceType>(path))
                .FirstOrDefault(item => item != null && item.name == cells[4]);
            if (resource == null) throw new InvalidDataException("Неизвестный ресурс: " + cells[4]);
            entries.Add(new ScientificUpgradeTable.Entry { id = cells[0], parentId = cells[1], title = cells[2],
                description = cells[3], costResource = resource,
                cost = int.Parse(cells[5], CultureInfo.InvariantCulture),
                effectValue = float.Parse(cells[6], CultureInfo.InvariantCulture) });
        }
        Undo.RecordObject(table, "Import scientific upgrades");
        table.entries = entries;
        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Импорт улучшений", "Импортировано улучшений: " + entries.Count, "OK");
    }

    [MenuItem("Tools/Таблицы/Локализация/Экспорт CSV")]
    private static void ExportLocalization()
    {
        var table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(LocalizationPath);
        if (table == null) return;
        string path = EditorUtility.SaveFilePanel("Экспорт локализации", "", "localization.csv", "csv");
        if (string.IsNullOrEmpty(path)) return;
        var rows = new List<string> { Row((new[] { "key" }).Concat(table.languages).ToArray()) };
        foreach (var entry in table.entries)
            rows.Add(Row((new[] { entry.key }).Concat(Enumerable.Range(0, table.languages.Count)
                .Select(i => i < entry.values.Count ? entry.values[i] : "")).ToArray()));
        File.WriteAllText(path, string.Join("\r\n", rows), new UTF8Encoding(true));
        EditorUtility.RevealInFinder(path);
    }

    [MenuItem("Tools/Таблицы/Локализация/Импорт CSV")]
    private static void ImportLocalizationFile()
    {
        string path = EditorUtility.OpenFilePanel("Импорт локализации", "", "csv");
        if (string.IsNullOrEmpty(path)) return;
        var rows = Parse(File.ReadAllText(path, Encoding.UTF8));
        if (rows.Count == 0 || rows[0].Count < 2 || rows[0][0] != "key")
        { EditorUtility.DisplayDialog("Импорт локализации", "Первый столбец должен быть key.", "OK"); return; }
        var table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(LocalizationPath);
        if (table == null) return;
        Undo.RecordObject(table, "Import localization");
        table.languages = rows[0].Skip(1).ToList();
        table.entries = rows.Skip(1).Where(row => row.Count > 0 && !string.IsNullOrWhiteSpace(row[0]))
            .Select(row => new LocalizationTable.Entry { key = row[0], values = Enumerable.Range(1, table.languages.Count)
                .Select(i => i < row.Count ? row[i] : "").ToList() }).ToList();
        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Импорт локализации", "Импортировано строк: " + table.entries.Count, "OK");
    }

    private static string Row(params string[] cells) => string.Join(",", cells.Select(cell => "\"" + (cell ?? "").Replace("\"", "\"\"") + "\""));

    private static List<List<string>> Parse(string csv)
    {
        var rows = new List<List<string>>(); var row = new List<string>(); var value = new StringBuilder(); bool quoted = false;
        for (int i = 0; i < csv.Length; i++)
        {
            char c = csv[i];
            if (c == '"' && quoted && i + 1 < csv.Length && csv[i + 1] == '"') { value.Append('"'); i++; }
            else if (c == '"') quoted = !quoted;
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
        row.Add(value.ToString()); if (row.Any(cell => cell.Length > 0)) rows.Add(row);
        if (rows.Count > 0 && rows[0].Count > 0) rows[0][0] = rows[0][0].TrimStart('\ufeff');
        return rows;
    }
}
