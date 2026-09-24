#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class WorldDifficultyTablesMenu
{
    private const string SheetId = "1eepDmDSn5Y-Bs6qj49qZV8EjP1zo-pi58c8EeYKflyg";
    private const string HexSheet = "HexManager";
    private const string AlarmSheet = "AlarmSpawner";
    private const string HexAssetPath = "Assets/Resources/World/HexDifficultyTable.asset";
    private const string AlarmAssetPath = "Assets/Resources/World/AlarmDifficultyTable.asset";
    private const string HexManagerPrefabPath = "Assets/Prefabs/Managers/HexManager.prefab";
    private const string HexHeader = "difficulty,group_id,prefab_path,content_id,mandatory,auto_unlock,start_locked";
    private const string AlarmHeader = "difficulty,threshold,alarm_value,min_spawn_interval,max_spawn_interval,min_enemies,max_enemies,enemy_prefab_path,enemy_weight";

    [MenuItem("Tools/Таблицы/HexManager/Экспорт в Google Sheets")]
    private static void CopyHexGoogle() => CopyForGoogle(BuildHexCsv(), HexSheet, "наполнение HexManager");

    [MenuItem("Tools/Таблицы/HexManager/Импорт из Google Sheets")]
    private static async void ImportHexGoogle() => await ImportGoogle(HexSheet, ImportHex);

    [MenuItem("Tools/Таблицы/Alarm Bar/Экспорт в Google Sheets")]
    private static void CopyAlarmGoogle() => CopyForGoogle(BuildAlarmCsv(), AlarmSheet, "настройки спавнера тревоги");

    [MenuItem("Tools/Таблицы/Alarm Bar/Импорт из Google Sheets")]
    private static async void ImportAlarmGoogle() => await ImportGoogle(AlarmSheet, ImportAlarm);

    private static string BuildHexCsv()
    {
        HexDifficultyTable table = AssetDatabase.LoadAssetAtPath<HexDifficultyTable>(HexAssetPath);
        if (table == null) throw new InvalidDataException("Не найден " + HexAssetPath);
        HexManager manager = LoadHexManagerPrefab();
        SynchronizeDifficultyOne(table, manager.ConfiguredGroups);
        var rows = new List<string> { HexHeader };
        foreach (HexDifficultyTable.Difficulty difficulty in table.difficulties.OrderBy(item => item.level))
        foreach (HexManager.HexGroupSettings group in difficulty.groups.OrderBy(item => item.groupID))
        foreach (HexManager.HexPrefabData item in group.prefabsForGroup)
            rows.Add(Row(difficulty.level, group.groupID, AssetDatabase.GetAssetPath(item.prefab), item.contentID,
                item.isMandatory, item.autoUnlockHex, item.startLocked));
        return string.Join("\r\n", rows);
    }

    private static string BuildAlarmCsv()
    {
        AlarmDifficultyTable table = AssetDatabase.LoadAssetAtPath<AlarmDifficultyTable>(AlarmAssetPath);
        if (table == null) throw new InvalidDataException("Не найден " + AlarmAssetPath);
        var rows = new List<string> { AlarmHeader };
        foreach (AlarmDifficultyTable.Difficulty difficulty in table.difficulties.OrderBy(item => item.level))
        for (int thresholdIndex = 0; thresholdIndex < difficulty.thresholds.Count; thresholdIndex++)
        {
            AlarmThreshold threshold = difficulty.thresholds[thresholdIndex];
            if (threshold.enemies == null || threshold.enemies.Count == 0)
                rows.Add(Row(difficulty.level, thresholdIndex + 1, threshold.alarmValue, threshold.minSpawnInterval,
                    threshold.maxSpawnInterval, threshold.minEnemiesPerWave, threshold.maxEnemiesPerWave, "", 1));
            else foreach (AlarmEnemy enemy in threshold.enemies)
                rows.Add(Row(difficulty.level, thresholdIndex + 1, threshold.alarmValue, threshold.minSpawnInterval,
                    threshold.maxSpawnInterval, threshold.minEnemiesPerWave, threshold.maxEnemiesPerWave,
                    AssetDatabase.GetAssetPath(enemy.prefab), enemy.weight));
        }
        return string.Join("\r\n", rows);
    }

    private static void ImportHex(string csv)
    {
        List<List<string>> rows = Parse(csv);
        ValidateHeader(rows, HexHeader);
        HexDifficultyTable table = LoadOrCreate<HexDifficultyTable>(HexAssetPath);
        var difficulties = new Dictionary<int, HexDifficultyTable.Difficulty>();
        foreach (List<string> cells in rows.Skip(1))
        {
            if (cells.Count < 7 || string.IsNullOrWhiteSpace(cells[0])) continue;
            int level = ParseLevel(cells[0]);
            int groupId = int.Parse(cells[1], CultureInfo.InvariantCulture);
            if (!difficulties.TryGetValue(level, out HexDifficultyTable.Difficulty difficulty))
            {
                difficulty = new HexDifficultyTable.Difficulty { level = level };
                difficulties.Add(level, difficulty);
            }
            HexManager.HexGroupSettings group = difficulty.groups.FirstOrDefault(item => item.groupID == groupId);
            if (group == null)
            {
                group = new HexManager.HexGroupSettings { groupID = groupId, prefabsForGroup = new List<HexManager.HexPrefabData>() };
                difficulty.groups.Add(group);
            }
            GameObject prefab = string.IsNullOrWhiteSpace(cells[2]) ? null : LoadPrefab(cells[2]);
            group.prefabsForGroup.Add(new HexManager.HexPrefabData
            {
                prefab = prefab,
                contentID = cells[3],
                isMandatory = ParseBool(cells[4]),
                autoUnlockHex = ParseBool(cells[5]),
                startLocked = ParseBool(cells[6])
            });
        }
        Undo.RecordObject(table, "Import HexManager difficulties");
        table.difficulties = difficulties.Values.OrderBy(item => item.level).ToList();
        HexDifficultyTable.Difficulty difficultyOne = table.difficulties.FirstOrDefault(item => item.level == 1);
        if (difficultyOne != null)
        {
            HexManager manager = LoadHexManagerPrefab();
            Undo.RecordObject(manager, "Import HexManager difficulty 1");
            manager.ReplaceConfiguredGroups(CloneGroups(difficultyOne.groups));
            EditorUtility.SetDirty(manager);
            PrefabUtility.SavePrefabAsset(manager.gameObject);
        }
        Save(table, $"Импортировано строк HexManager: {rows.Count - 1}");
    }

    private static HexManager LoadHexManagerPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HexManagerPrefabPath);
        HexManager manager = prefab != null ? prefab.GetComponent<HexManager>() : null;
        if (manager == null) throw new InvalidDataException("Не найден HexManager в " + HexManagerPrefabPath);
        return manager;
    }

    private static void SynchronizeDifficultyOne(HexDifficultyTable table, IReadOnlyList<HexManager.HexGroupSettings> source)
    {
        HexDifficultyTable.Difficulty difficulty = table.difficulties.FirstOrDefault(item => item.level == 1);
        if (difficulty == null)
        {
            difficulty = new HexDifficultyTable.Difficulty { level = 1 };
            table.difficulties.Add(difficulty);
        }
        difficulty.groups = CloneGroups(source);
        table.difficulties = table.difficulties.OrderBy(item => item.level).ToList();
        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssets();
    }

    private static List<HexManager.HexGroupSettings> CloneGroups(IEnumerable<HexManager.HexGroupSettings> source)
    {
        return source.Select(group => new HexManager.HexGroupSettings
        {
            groupID = group.groupID,
            prefabsForGroup = group.prefabsForGroup.Select(item => new HexManager.HexPrefabData
            {
                contentID = item.contentID,
                prefab = item.prefab,
                isMandatory = item.isMandatory,
                autoUnlockHex = item.autoUnlockHex,
                startLocked = item.startLocked
            }).ToList()
        }).ToList();
    }

    private static void ImportAlarm(string csv)
    {
        List<List<string>> rows = Parse(csv);
        ValidateHeader(rows, AlarmHeader);
        AlarmDifficultyTable table = LoadOrCreate<AlarmDifficultyTable>(AlarmAssetPath);
        var difficulties = new Dictionary<int, AlarmDifficultyTable.Difficulty>();
        var thresholds = new Dictionary<(int level, int index), AlarmThreshold>();
        foreach (List<string> cells in rows.Skip(1))
        {
            if (cells.Count < 9 || string.IsNullOrWhiteSpace(cells[0])) continue;
            int level = ParseLevel(cells[0]);
            int index = Mathf.Max(1, int.Parse(cells[1], CultureInfo.InvariantCulture));
            if (!difficulties.TryGetValue(level, out AlarmDifficultyTable.Difficulty difficulty))
            {
                difficulty = new AlarmDifficultyTable.Difficulty { level = level };
                difficulties.Add(level, difficulty);
            }
            if (!thresholds.TryGetValue((level, index), out AlarmThreshold threshold))
            {
                threshold = new AlarmThreshold
                {
                    alarmValue = ParseFloat(cells[2]), minSpawnInterval = ParseFloat(cells[3]),
                    maxSpawnInterval = ParseFloat(cells[4]), minEnemiesPerWave = ParseInt(cells[5]),
                    maxEnemiesPerWave = ParseInt(cells[6]), enemies = new List<AlarmEnemy>()
                };
                thresholds.Add((level, index), threshold);
            }
            if (!string.IsNullOrWhiteSpace(cells[7]))
                threshold.enemies.Add(new AlarmEnemy { prefab = LoadPrefab(cells[7]), weight = Mathf.Max(1, ParseInt(cells[8])) });
        }
        foreach (var pair in difficulties)
            pair.Value.thresholds = thresholds.Where(item => item.Key.level == pair.Key).OrderBy(item => item.Key.index).Select(item => item.Value).ToList();
        Undo.RecordObject(table, "Import Alarm difficulties");
        table.difficulties = difficulties.Values.OrderBy(item => item.level).ToList();
        Save(table, $"Импортировано строк Alarm Bar: {rows.Count - 1}");
    }

    private static async System.Threading.Tasks.Task ImportGoogle(string sheet, Action<string> importer)
    {
        try
        {
            string url = $"https://docs.google.com/spreadsheets/d/{SheetId}/gviz/tq?tqx=out:csv&sheet={Uri.EscapeDataString(sheet)}";
            using var client = new HttpClient();
            string csv = await client.GetStringAsync(url);
            if (string.IsNullOrWhiteSpace(csv)) throw new InvalidDataException($"Вкладка {sheet} пуста или недоступна.");
            importer(csv);
        }
        catch (Exception error) { EditorUtility.DisplayDialog("Импорт " + sheet, error.Message, "OK"); }
    }

    private static void CopyForGoogle(string csv, string sheet, string title)
    {
        List<List<string>> rows = Parse(csv);
        EditorGUIUtility.systemCopyBuffer = string.Join("\n", rows.Select(row => string.Join("\t", row)));
        Application.OpenURL($"https://docs.google.com/spreadsheets/d/{SheetId}/edit");
        EditorUtility.DisplayDialog("Экспорт " + title, $"Откройте вкладку {sheet}, выберите A1 и вставьте данные.", "OK");
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "Assets/Resources");
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void Save(ScriptableObject asset, string message)
    {
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Таблицы мира", message, "OK");
    }

    private static GameObject LoadPrefab(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) throw new InvalidDataException("Не найден префаб: " + path);
        return prefab;
    }

    private static void ValidateHeader(List<List<string>> rows, string expected)
    {
        if (rows.Count == 0 || !rows[0].SequenceEqual(expected.Split(',')))
            throw new InvalidDataException("Нужны столбцы: " + expected);
    }

    private static int ParseLevel(string value) => Mathf.Clamp(ParseInt(value), 1, 5);
    private static int ParseInt(string value) => int.Parse(value, CultureInfo.InvariantCulture);
    private static float ParseFloat(string value) => float.Parse(value, CultureInfo.InvariantCulture);
    private static bool ParseBool(string value) => value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("yes", StringComparison.OrdinalIgnoreCase);
    private static string Row(params object[] cells) => string.Join(",", cells.Select(cell => "\"" + Convert.ToString(cell, CultureInfo.InvariantCulture).Replace("\"", "\"\"") + "\""));

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
#endif
