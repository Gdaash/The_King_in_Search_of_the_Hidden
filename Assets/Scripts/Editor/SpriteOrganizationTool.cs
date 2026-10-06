#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>GUID-preserving sprite relocation with a journal and dependency validation.</summary>
public static class SpriteOrganizationTool
{
    const string Journal = "Temp/SpriteOrganizationPlan.json";
    [Serializable] public class Item
    {
        public string guid, oldPath, newPath, hash, metaHash;
        public bool used;
        public long[] spriteIds;
    }
    [Serializable] public class Root
    {
        public string guid, path;
        public string[] dependencies;
    }
    [Serializable] public class Plan
    {
        public Item[] items;
        public Root[] roots;
        public string[] liveSpriteGuids;
        public Item[] supportItems = Array.Empty<Item>();
    }
    static readonly Dictionary<string, string> Folders = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Assets/Art/BaseScene/Layers"] = "Assets/Sprites/World/Base/Layers",
        ["Assets/Art/ResourcePanel"] = "Assets/Sprites/UI/ResourcePanel",
        ["Assets/Sprites/Evolution adventure/Sprites/UI/Sprites"] = "Assets/Sprites/UI/Evolution",
        ["Assets/Sprites/Evolution adventure/Sprites/UI"] = "Assets/Sprites/UI/Evolution/Sheets",
        ["Assets/Sprites/Evolution adventure/Sprites/CharactersAndItems"] = "Assets/Sprites/Units/Evolution",
        ["Assets/Sprites/Evolution adventure/Sprites/Tiles"] = "Assets/Sprites/World/Tiles/Evolution",
        ["Assets/Sprites/Evolution adventure/Sprites/Clouds"] = "Assets/Sprites/VFX/Clouds",
        ["Assets/Sprites/Evolution adventure/Sprites"] = "Assets/Sprites/Libraries/Evolution",
        ["Assets/Sprites/UI Kit"] = "Assets/Sprites/UI/Kit",
        ["Assets/Sprites/Ui/Unit Stats"] = "Assets/Sprites/UI/UnitStats",
        ["Assets/Sprites/Ui/Enemy Portraits"] = "Assets/Sprites/UI/Portraits/Enemies",
        ["Assets/Sprites/Ui/HexIcons"] = "Assets/Sprites/UI/Icons/Hexes",
        ["Assets/Sprites/Ui"] = "Assets/Sprites/UI/Common",
        ["Assets/Sprites/Enemies"] = "Assets/Sprites/Units/Enemies",
        ["Assets/Sprites/Monsters"] = "Assets/Sprites/Units/Enemies/Legacy",
        ["Assets/Sprites/RedHuman"] = "Assets/Sprites/Units/Allies/Red",
        ["Assets/Sprites/GreenHuman"] = "Assets/Sprites/Units/NPCs/Green",
        ["Assets/Sprites/HumanHouse"] = "Assets/Sprites/Units/NPCs/Housing",
        ["Assets/Sprites/MonsterWithHouse"] = "Assets/Sprites/Units/NPCs/Buildings",
        ["Assets/Sprites/Builds/PortalAnimation"] = "Assets/Sprites/World/Buildings/PortalAnimation",
        ["Assets/Sprites/Builds"] = "Assets/Sprites/World/Buildings",
        ["Assets/Sprites/Tile"] = "Assets/Sprites/World/Tiles",
        ["Assets/Sprites/BG"] = "Assets/Sprites/World/Backgrounds",
        ["Assets/PixelPerfectOutline/Sprites"] = "Assets/Sprites/Libraries/PixelPerfectOutline",
        ["Assets/Prefabs/Base/Islands"] = "Assets/Sprites/UI/Islands"
    };

    static string Hash(string path)
    {
        using var algorithm = SHA256.Create();
        using var stream = File.OpenRead(path);
        return BitConverter.ToString(algorithm.ComputeHash(stream));
    }
    static IEnumerable<string> Dependencies(string path) => AssetDatabase.GetDependencies(path, true)
        .Select(AssetDatabase.AssetPathToGUID).Where(g => !string.IsNullOrEmpty(g)).Distinct().OrderBy(g => g);
    static string Destination(string path)
    {
        if (path.Contains("/Resources/"))
            return "Assets/Sprites/Resources/" + path.Substring(path.LastIndexOf("/Resources/", StringComparison.Ordinal) + 11);
        if (path == "Assets/Sprites/Logo.png") return "Assets/Sprites/Branding/Logo.png";
        if (path == "Assets/Sprites/Square.png") return "Assets/Sprites/UI/Common/Square.png";
        if (path == "Assets/Art/BaseScene/Construction Icon.png") return "Assets/Sprites/UI/Icons/Construction Icon.png";
        if (path.StartsWith("Assets/Sprites/Builds/Hex", StringComparison.Ordinal))
            return "Assets/Sprites/World/Hexes/" + Path.GetFileName(path);
        if (path.StartsWith("Assets/Sprites/Resources/Flag", StringComparison.Ordinal))
            return "Assets/Sprites/UI/HexIndicators/" + Path.GetFileName(path);
        if (path == "Assets/Sprites/Resources/LBM.png" || path == "Assets/Sprites/Resources/RBM.png")
            return "Assets/Sprites/UI/Controls/" + Path.GetFileName(path);
        foreach (var folder in Folders.OrderByDescending(p => p.Key.Length))
            if (path.StartsWith(folder.Key + "/", StringComparison.OrdinalIgnoreCase))
                return folder.Value + path.Substring(folder.Key.Length);
        return "Assets/Sprites/Libraries/" + path.Substring(7);
    }
    static void Folder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        Folder(Path.GetDirectoryName(folder).Replace('\\', '/'));
        string error = AssetDatabase.CreateFolder(Path.GetDirectoryName(folder).Replace('\\', '/'), Path.GetFileName(folder));
        if (string.IsNullOrEmpty(error)) throw new IOException("Cannot create " + folder);
    }
    static string[] LiveSpriteGuids()
    {
        var roots = new List<UnityEngine.Object>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
            roots.AddRange(SceneManager.GetSceneAt(i).GetRootGameObjects());
        return EditorUtility.CollectDependencies(roots.ToArray()).OfType<Sprite>()
            .Select(s => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(s)))
            .Where(g => !string.IsNullOrEmpty(g)).Distinct().OrderBy(g => g).ToArray();
    }
    public static string Analyze()
    {
        if (File.Exists(Journal)) throw new IOException("An organization journal already exists. Preserve it for validation and recovery; do not analyze the moved assets again.");
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        var paths = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" })
            .Concat(AssetDatabase.FindAssets("t:Sprite", new[] { "Assets" }))
            .Select(AssetDatabase.GUIDToAssetPath).Distinct().OrderBy(p => p)
            .Where(p => (AssetImporter.GetAtPath(p) is TextureImporter importer && importer.textureType == TextureImporterType.Sprite) ||
                AssetDatabase.GetMainAssetTypeAtPath(p) == typeof(Sprite)).ToArray();
        var sprites = new HashSet<string>(paths);
        var roots = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/", StringComparison.Ordinal) &&
            !sprites.Contains(p) && new[] { ".prefab", ".unity", ".asset", ".mat", ".anim", ".controller", ".overrideController", ".spriteatlas", ".spriteatlasv2" }
                .Contains(Path.GetExtension(p).ToLowerInvariant())).ToArray();
        var records = roots.Select(p => new Root { path = p, guid = AssetDatabase.AssetPathToGUID(p), dependencies = Dependencies(p).ToArray() }).ToArray();
        var used = new HashSet<string>(records.SelectMany(r => r.dependencies));
        var live = LiveSpriteGuids();
        used.UnionWith(live);
        // Resources paths may be assembled dynamically. Keep their relative load paths intact.
        foreach (string path in paths.Where(p => p.Contains("/Resources/"))) used.Add(AssetDatabase.AssetPathToGUID(path));
        var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var items = new List<Item>();
        foreach (string path in paths)
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            bool active = used.Contains(guid);
            string destination = active ? Destination(path) : "Assets/SpritesOld/" +
                (path.StartsWith("Assets/Sprites/", StringComparison.OrdinalIgnoreCase) ? path.Substring(15) : path.Substring(7));
            if (!targets.Add(destination) || (path != destination && File.Exists(destination)))
            {
                destination = Path.GetDirectoryName(destination).Replace('\\', '/') + "/" + Path.GetFileNameWithoutExtension(destination) + "_" + guid.Substring(0, 8) + Path.GetExtension(destination);
                if (!targets.Add(destination) || File.Exists(destination)) throw new IOException("Destination collision: " + destination);
            }
            var ids = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Select(s =>
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(s, out string _, out long id); return id;
            }).OrderBy(id => id).ToArray();
            items.Add(new Item { guid = guid, oldPath = path, newPath = destination, used = active,
                hash = Hash(path), metaHash = Hash(path + ".meta"), spriteIds = ids });
        }
        Directory.CreateDirectory("Temp");
        File.WriteAllText(Journal, JsonUtility.ToJson(new Plan { items = items.ToArray(), roots = records, liveSpriteGuids = live }, true));
        var summary = items.GroupBy(i => string.Join("/", i.newPath.Split('/').Take(4)))
            .OrderBy(g => g.Key).Select(g => g.Key + ": " + g.Count());
        return "Total=" + items.Count + "; used=" + items.Count(i => i.used) + "; archive=" + items.Count(i => !i.used) + "\n" + string.Join("\n", summary);
    }
    static Plan Read() => JsonUtility.FromJson<Plan>(File.ReadAllText(Journal));
    public static string MoveSupport()
    {
        var plan = Read();
        var support = new Dictionary<string, string>
        {
            ["Assets/Sprites/UI Kit/README.md"] = "Assets/Sprites/UI/Kit/README.md",
            ["Assets/Sprites/UI Kit/UI_Kit_Manifest.json"] = "Assets/Sprites/UI/Kit/UI_Kit_Manifest.json",
            ["Assets/Sprites/UI Kit/Components/Component_Source_Map.json"] = "Assets/Sprites/UI/Kit/Components/Component_Source_Map.json",
            ["Assets/Art/BaseScene/BaseSceneLayers.json"] = "Assets/Sprites/World/Base/BaseSceneLayers.json",
            ["Assets/Sprites/UI Kit/UI Kit Small Components.png"] = "Assets/SpritesOld/UI Kit/UI Kit Small Components.png",
            ["Assets/Sprites/Evolution adventure/Sprites/EvolutionAnimation_X90_Y90.png"] = "Assets/SpritesOld/Evolution adventure/Sprites/EvolutionAnimation_X90_Y90.png"
        };
        var records = new List<Item>();
        foreach (var pair in support)
        {
            Folder(Path.GetDirectoryName(pair.Value).Replace('\\', '/'));
            var item = new Item { oldPath = pair.Key, newPath = pair.Value, guid = AssetDatabase.AssetPathToGUID(pair.Key),
                hash = Hash(pair.Key), metaHash = Hash(pair.Key + ".meta") };
            string error = AssetDatabase.MoveAsset(pair.Key, pair.Value);
            if (!string.IsNullOrEmpty(error)) throw new IOException(error);
            records.Add(item);
        }
        // Give the current game logo its own discoverable category.
        var logo = plan.items.First(i => i.oldPath.EndsWith("/ThoseUnderHexLogo.png", StringComparison.Ordinal));
        string destination = "Assets/Sprites/Branding/ThoseUnderHexLogo.png";
        Folder("Assets/Sprites/Branding");
        string logoError = AssetDatabase.MoveAsset(logo.newPath, destination);
        if (!string.IsNullOrEmpty(logoError)) throw new IOException(logoError);
        logo.newPath = destination;
        plan.supportItems = records.ToArray();
        File.WriteAllText(Journal, JsonUtility.ToJson(plan, true));
        return "Moved source manifests, unused texture sheets and current logo";
    }
    public static string MoveBatch(int offset, int count)
    {
        var plan = Read();
        foreach (var folder in plan.items.Skip(offset).Take(count).Select(i => Path.GetDirectoryName(i.newPath).Replace('\\', '/')).Distinct()) Folder(folder);
        int moved = 0;
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var item in plan.items.Skip(offset).Take(count))
            {
                string current = AssetDatabase.GUIDToAssetPath(item.guid);
                if (current == item.newPath) continue;
                if (current != item.oldPath) throw new IOException("Unexpected source " + current);
                string error = AssetDatabase.MoveAsset(item.oldPath, item.newPath);
                if (!string.IsNullOrEmpty(error)) throw new IOException(error);
                moved++;
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }
        return "Processed " + offset + ".." + Math.Min(offset + count, plan.items.Length) + "/" + plan.items.Length + "; moved=" + moved;
    }
    public static string RewritePaths()
    {
        var plan = Read();
        var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in plan.items) if (item.oldPath != item.newPath) replacements[item.oldPath] = item.newPath;
        foreach (var item in plan.supportItems) replacements[item.oldPath] = item.newPath;
        foreach (var folder in Folders)
            if (!folder.Key.StartsWith("Assets/Prefabs/", StringComparison.Ordinal)) replacements[folder.Key] = folder.Value;
        // These source generators use a folder string without the trailing slash.
        replacements["Assets/Art/ResourcePanel"] = "Assets/Sprites/UI/ResourcePanel";
        replacements["Assets/Art/BaseScene/Layers"] = "Assets/Sprites/World/Base/Layers";
        var pattern = new Regex(string.Join("|", replacements.Keys.OrderByDescending(k => k.Length).Select(Regex.Escape)), RegexOptions.IgnoreCase);
        var extensions = new HashSet<string> { ".cs", ".json", ".py", ".ps1", ".md", ".txt", ".shader", ".uss", ".uxml" };
        var files = new List<string>();
        foreach (string folder in new[] { "Assets", "Tools", "Docs" }) if (Directory.Exists(folder))
            files.AddRange(Directory.GetFiles(folder, "*", SearchOption.AllDirectories).Where(p => extensions.Contains(Path.GetExtension(p).ToLowerInvariant())));
        int changed = 0;
        foreach (string file in files)
        {
            // The journal must retain original paths for validation and recovery.
            if (file.Replace('\\', '/').EndsWith("SpriteOrganizationTool.cs", StringComparison.Ordinal)) continue;
            string old = File.ReadAllText(file);
            string updated = pattern.Replace(old, match => replacements[match.Value]);
            if (updated == old) continue;
            byte[] bytes = File.ReadAllBytes(file);
            bool bom = bytes.Length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf;
            File.WriteAllText(file, updated, new UTF8Encoding(bom));
            changed++;
        }
        AssetDatabase.Refresh();
        return "Updated path references in " + changed + " source/manifest/document files";
    }
    public static string RemoveEmptyFolders()
    {
        int removed = 0;
        foreach (string path in AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/Sprites/", StringComparison.Ordinal) ||
            p.StartsWith("Assets/Art/", StringComparison.Ordinal) || p.StartsWith("Assets/PixelPerfectOutline/Sprites", StringComparison.Ordinal) ||
            p.StartsWith("Assets/Resources/LegacyFindIt/UI", StringComparison.Ordinal)).OrderByDescending(p => p.Length))
            if (AssetDatabase.IsValidFolder(path) && !Directory.EnumerateFileSystemEntries(path).Any())
            {
                if (!AssetDatabase.DeleteAsset(path)) throw new IOException("Cannot remove empty folder " + path);
                removed++;
            }
        return "Removed " + removed + " empty source folders";
    }
    public static string Validate()
    {
        var plan = Read();
        foreach (var item in plan.items)
        {
            if (!string.Equals(AssetDatabase.GUIDToAssetPath(item.guid), item.newPath, StringComparison.OrdinalIgnoreCase) || Hash(item.newPath) != item.hash || Hash(item.newPath + ".meta") != item.metaHash)
                throw new IOException("Sprite or import settings changed: " + item.oldPath);
            var ids = AssetDatabase.LoadAllAssetsAtPath(item.newPath).OfType<Sprite>().Select(s =>
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(s, out string _, out long id); return id;
            }).OrderBy(id => id).ToArray();
            if (!ids.SequenceEqual(item.spriteIds)) throw new IOException("Sprite subasset IDs changed: " + item.newPath);
        }
        foreach (var root in plan.roots)
            if (!Dependencies(AssetDatabase.GUIDToAssetPath(root.guid)).SequenceEqual(root.dependencies))
                throw new IOException("Dependency graph changed: " + root.path);
        if (!LiveSpriteGuids().SequenceEqual(plan.liveSpriteGuids)) throw new IOException("Open scene sprite references changed");
        foreach (var item in plan.supportItems)
            if (!string.Equals(AssetDatabase.GUIDToAssetPath(item.guid), item.newPath, StringComparison.OrdinalIgnoreCase) || Hash(item.newPath + ".meta") != item.metaHash)
                throw new IOException("Support asset reference or metadata changed: " + item.oldPath);
        Directory.CreateDirectory("Docs/AssetOrganization");
        File.WriteAllText("Docs/AssetOrganization/SpriteMoves.json", JsonUtility.ToJson(plan, true));
        return "PASS: " + plan.items.Length + " files, identical pixel bytes, metadata, GUIDs and sprite IDs; " + plan.roots.Length + " dependency graphs and open scene unchanged";
    }
}
#endif
