using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using GameFoundation.MetaProgression;

public static class WeaponEffectsSetup
{
    static Sprite Pellet(Sprite icon)
    {
        string path=AssetDatabase.GetAssetPath(icon);var importer=AssetImporter.GetAtPath(path);
        var factories=new SpriteDataProviderFactories();factories.Init();var provider=factories.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
        var capability=provider.GetDataProvider<ISpriteFrameEditCapability>();
        if(capability==null||!capability.GetEditCapability().HasCapability(EEditCapability.CreateAndDeleteSprite))throw new System.Exception("Sprite importer does not support adding a projectile slice");
        var rects=provider.GetSpriteRects().ToList();var rect=rects.FirstOrDefault(r=>r.name=="projectile_FirePellet");
        if(rect==null)
        {
            rect=new SpriteRect{name="projectile_FirePellet",spriteID=GUID.Generate(),alignment=(int)SpriteAlignment.Center,pivot=new Vector2(.5f,.5f),rect=new Rect(icon.rect.x+8,icon.rect.y+1,8,22)};rects.Add(rect);
            provider.SetSpriteRects(rects.ToArray());var names=provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            var pairs=names.GetNameFileIdPairs().ToList();pairs.Add(new SpriteNameFileIdPair(rect.name,rect.spriteID));names.SetNameFileIdPairs(pairs);provider.Apply();importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First(s=>s.name=="projectile_FirePellet");
    }
    public static void Apply()
    {
        AssetDatabase.ImportAsset("Assets/Resources/Effects/Spawn Tentacle.png");
        var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/Resources/Effects/Spawn Tentacle.png");
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=32;
        importer.maxTextureSize=64;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.SaveAndReimport();
        var balance=AssetDatabase.LoadAssetAtPath<PortalTowerBalance>("Assets/Resources/World/PortalTowerBalance.asset");
        var pellet=Pellet(balance.FindWeapon(PortalTowerBalance.Weapon.FireShot).icon);
        const string materialPath="Assets/Resources/Effects/Pixel Projectile.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(material==null){material=new Material(Shader.Find("Sprites/Default"));AssetDatabase.CreateAsset(material,materialPath);}
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs"}))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var projectile=root.GetComponent<EnemyProjectile>();if(projectile==null)continue;
                bool orb=path.EndsWith("Portal ElectricOrb.prefab"),fire=path.EndsWith("Portal FireShot.prefab"),magic=path.EndsWith("PlayerTowerProjectile.prefab");
                var old=root.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r=>r.enabled);
                SpriteRenderer visual=old;
                if(orb||fire||magic)
                {
                    var child=root.transform.Find("Weapon Icon Projectile");if(child==null){child=new GameObject("Weapon Icon Projectile",typeof(SpriteRenderer)).transform;child.SetParent(root.transform,false);}
                    visual=child.GetComponent<SpriteRenderer>();
                    visual.sprite=orb?balance.FindWeapon(PortalTowerBalance.Weapon.ElectricOrb).icon:fire?pellet:balance.FindWeapon(PortalTowerBalance.Weapon.Bolts).icon;
                    visual.sharedMaterial=material;visual.sortingLayerName=old!=null?old.sortingLayerName:"Default";visual.sortingOrder=old!=null?old.sortingOrder:20;
                    child.localPosition=Vector3.zero;child.localScale=Vector3.one*(orb?.55f:magic?.55f:1);
                    child.localRotation=Quaternion.Euler(0,0,fire?-90:magic?-45:0);
                    foreach(var sprite in root.GetComponentsInChildren<SpriteRenderer>(true))if(sprite!=visual)sprite.enabled=false;
                }
                foreach(var sprite in root.GetComponentsInChildren<SpriteRenderer>(true)){var color=sprite.color;color.a=orb?.7f:fire||magic?.78f:.82f;sprite.color=color;}
                var so=new SerializedObject(projectile);so.FindProperty("animatedVisual").objectReferenceValue=visual!=null?visual.transform:root.transform;
                so.FindProperty("flightRotationSpeed").floatValue=orb?85:0;so.FindProperty("collapseOnImpact").boolValue=orb;so.FindProperty("deathDuration").floatValue=.2f;so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();
    }
}
