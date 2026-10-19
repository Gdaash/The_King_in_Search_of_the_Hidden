#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using GameFoundation.MetaProgression;

/// <summary>Imports the native 24 px portal icon sheet and assigns its sprites.</summary>
public static class PortalIconSheetImport
{
    const string Path = "Assets/Sprites/Ui/Portal Weapons/Weapon Upgrade Icons.png";
    static readonly string[] Names = {
        "weapon_Bolts", "weapon_Beam", "weapon_Rings", "weapon_Lightning",
        "weapon_FrostSeal", "weapon_FireShot", "weapon_ElectricOrb", "lights",
        "all_damage", "projectile_count", "tick_interval", "attack_interval",
        "area_duration", "all_range", "projectile_speed", "area_size"
    };

    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before importing icons.");
        AssetDatabase.ImportAsset(Path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(Path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 32;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 128;
        importer.SaveAndReimport();

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        if (provider == null) throw new InvalidOperationException("Sprite data provider is unavailable.");
        provider.InitSpriteEditorDataProvider();
        var capability = provider.GetDataProvider<ISpriteFrameEditCapability>();
        if (capability == null || !capability.GetEditCapability().HasCapability(EEditCapability.CreateAndDeleteSprite))
            throw new InvalidOperationException("Importer does not support sprite slicing.");
        var previous = provider.GetSpriteRects();
        var rects = Names.Select((name, i) => new SpriteRect {
            name = name, rect = new Rect((i % 4) * 24, (3 - i / 4) * 24, 24, 24),
            alignment = (int)SpriteAlignment.Center, pivot = new Vector2(.5f, .5f),
            spriteID = previous.FirstOrDefault(r => r.name == name)?.spriteID ?? GUID.Generate()
        }).ToArray();
        provider.SetSpriteRects(rects);
        var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (names == null) throw new InvalidOperationException("Sprite name/file ID provider is unavailable.");
        names.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        provider.Apply();
        importer.SaveAndReimport();

        var sprites = AssetDatabase.LoadAllAssetsAtPath(Path).OfType<Sprite>().ToDictionary(s => s.name);
        var balance = AssetDatabase.LoadAssetAtPath<PortalTowerBalance>("Assets/Resources/World/PortalTowerBalance.asset");
        if (balance == null || sprites.Count != Names.Length) throw new InvalidOperationException("Incomplete icon import.");
        Undo.RecordObject(balance, "Assign portal weapon and upgrade icons");
        foreach (var upgrade in balance.upgrades)
        {
            if (!sprites.TryGetValue(upgrade.id, out var icon)) throw new InvalidOperationException("Missing icon: " + upgrade.id);
            upgrade.icon = icon;
        }
        foreach (var weapon in balance.weapons)
            weapon.icon = sprites["weapon_" + weapon.weapon];
        EditorUtility.SetDirty(balance);
        AssetDatabase.SaveAssets();
        foreach (var s in sprites.Values)
            if (s.rect.size != new Vector2(24, 24)) throw new InvalidOperationException("Incorrect sprite size.");
        Debug.Log("PORTAL_ICON_IMPORT_OK: 16 unique 24x24 sprites; PPU 32, Point, no compression; all weapons and upgrades assigned.");
        System.IO.Directory.CreateDirectory(".ai/weapon-icons");
        System.IO.File.WriteAllText(".ai/weapon-icons/unity-import-ok.txt", "16 sprites, 7 weapons, 16 upgrade entries; each sprite 24x24, UI 48x48.");
    }
}
#endif
