#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using GameFoundation.MetaProgression;

public static class PortalWeaponPresentationSetup
{
    static Sprite Import(string path)
    {
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=32;importer.filterMode=FilterMode.Point;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;
        importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static void Configure(GameObject root,Sprite lightning)
    {
        foreach(var controller in root.GetComponentsInChildren<PortalWeaponController>(true))
        {controller.lightningSprite=lightning;EditorUtility.SetDirty(controller);PrefabUtility.RecordPrefabInstancePropertyModifications(controller);}
        foreach(var view in root.GetComponentsInChildren<PortalUpgradeCards>(true))
        {
            var canvas=view.GetComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingOrder=32760;
            EditorUtility.SetDirty(canvas);PrefabUtility.RecordPrefabInstancePropertyModifications(canvas);
            foreach(var c in view.cards)
            {
                c.description.fontSize=20;c.description.rectTransform.sizeDelta=new Vector2(302,190);
                c.description.rectTransform.anchoredPosition=new Vector2(0,-40);
                c.icon.rectTransform.sizeDelta=new Vector2(64,64);
                EditorUtility.SetDirty(c.description);EditorUtility.SetDirty(c.description.rectTransform);EditorUtility.SetDirty(c.icon.rectTransform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(c.description);
                PrefabUtility.RecordPrefabInstancePropertyModifications(c.description.rectTransform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(c.icon.rectTransform);
            }
        }
    }
    public static void Run()
    {
        Sprite bolt=Import("Assets/Sprites/Ui/Portal Weapons/Magic Bolts.png");
        Sprite beam=Import("Assets/Sprites/Ui/Portal Weapons/Burning Beam.png");
        Sprite lightning=Import("Assets/Sprites/Ui/Portal Weapons/Lightning.png");
        Sprite effect=Import("Assets/Sprites/Effects/Portal Pixel Lightning.png");
        var balance=AssetDatabase.LoadAssetAtPath<PortalTowerBalance>("Assets/Resources/World/PortalTowerBalance.asset");
        void Weapon(PortalTowerBalance.Weapon kind,Sprite icon,string ru,string en)
        {
            var w=balance.FindWeapon(kind);w.icon=icon;w.description=ru;w.englishDescription=en;
            foreach(var u in balance.upgrades)if(u.weapon==kind){u.icon=icon;if(u.effect==PortalTowerBalance.Effect.UnlockWeapon){u.description=ru;u.englishDescription=en;}}
        }
        Weapon(PortalTowerBalance.Weapon.Bolts,bolt,"Сильные магические снаряды по ближайшему врагу.","Heavy magic bolts fired at the nearest enemy.");
        Weapon(PortalTowerBalance.Weapon.Beam,beam,"Следует за ближайшим врагом. Огненное пятно поражает всех врагов внутри.","Tracks the nearest enemy. The fiery spot damages every enemy inside.");
        Weapon(PortalTowerBalance.Weapon.Lightning,lightning,"Бьёт случайного врага в радиусе 2,6. Игнорирует броню; против бронированных +50%.","Hits a random enemy within 2.6 units. Ignores armor; +50% damage against armored enemies.");
        var lightningWeapon=balance.FindWeapon(PortalTowerBalance.Weapon.Lightning).combat;
        lightningWeapon.range=2.6f;EditorUtility.SetDirty(lightningWeapon);EditorUtility.SetDirty(balance);
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs"}))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid);var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(asset.GetComponentsInChildren<PortalUpgradeCards>(true).Length+asset.GetComponentsInChildren<PortalWeaponController>(true).Length==0)continue;
            var root=PrefabUtility.LoadPrefabContents(path);
            try{Configure(root,effect);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach(var root in scene.GetRootGameObjects())Configure(root,effect);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Debug.Log("Portal weapon art, modal sorting and lightning range installed.");
    }
}
#endif
