#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class ObsoleteWeaponArtCleanup
{
 public static string Run()
 {
  foreach(string name in new[]{"PlayerTowerProjectile","Portal ElectricOrb","Portal FireShot"})
  {
   string path="Assets/Prefabs/Projectiles/"+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
   try{if(root.transform.Find("Weapon Icon Projectile")!=null){foreach(var renderer in root.GetComponentsInChildren<SpriteRenderer>(true).Where(s=>!s.enabled).ToArray())UnityEngine.Object.DestroyImmediate(renderer);PrefabUtility.SaveAsPrefabAsset(root,path);}}
   finally{PrefabUtility.UnloadPrefabContents(root);}
  }
  AssetDatabase.SaveAssets();
  var report=new System.Collections.Generic.List<string>();
  var serialized=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&new[]{".prefab",".unity",".asset",".mat",".controller",".anim",".spriteatlas",".spriteatlasv2"}.Contains(Path.GetExtension(p))).ToArray();
  foreach(string path in new[]{"Assets/Sprites/Ui/Portal Weapons/Magic Bolts.png","Assets/Sprites/Ui/Portal Weapons/Burning Beam.png","Assets/Sprites/Ui/Portal Weapons/Lightning.png","Assets/Sprites/Ui/Portal Weapons/Frost Seal.png","Assets/Sprites/Units/Projectiles/Portal Electric Orb.png"})
  {
   if(AssetDatabase.LoadMainAssetAtPath(path)==null)continue;
   var refs=serialized.Where(p=>AssetDatabase.GetDependencies(p,false).Contains(path)).ToArray();
   if(refs.Length==0){AssetDatabase.DeleteAsset(path);report.Add("Deleted: "+path);}else report.Add("Retained (referenced): "+path+" <- "+string.Join(",",refs));
  }
  Directory.CreateDirectory(".ai/damage-palette");File.WriteAllLines(".ai/damage-palette/cleanup.txt",report);AssetDatabase.SaveAssets();return string.Join("\n",report);
 }
}
#endif
