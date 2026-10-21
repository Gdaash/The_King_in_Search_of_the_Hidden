#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class DamagePaletteSetup
{
 static void Materials()
 {
  Directory.CreateDirectory("Assets/Resources/Effects/Damage Palettes");AssetDatabase.Refresh();
  foreach(var type in new[]{DamageType.Fire,DamageType.Ice,DamageType.Electric,DamageType.Magic})foreach(bool ui in new[]{false,true})
  {
   string path="Assets/Resources/Effects/Damage Palettes/"+type+(ui?" UI":"")+".mat";
   var material=AssetDatabase.LoadAssetAtPath<Material>(path);var shader=Shader.Find(ui?"ThoseUnderHex/UI Damage Palette":"ThoseUnderHex/Damage Palette");if(shader==null)throw new Exception("Damage palette shader did not compile");
   if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}else material.shader=shader;
   Color shadow,mid,highlight;
   switch(type){case DamageType.Fire:shadow=new Color(.08f,.012f,.006f);mid=new Color(.95f,.13f,.025f);highlight=new Color(1,.8f,.36f);break;
   case DamageType.Ice:shadow=new Color(.04f,.14f,.23f);mid=new Color(.4f,.8f,.95f);highlight=new Color(.9f,.99f,1);break;
   case DamageType.Electric:shadow=new Color(.09f,.065f,.012f);mid=new Color(1,.78f,.18f);highlight=new Color(1,1,.86f);break;
   default:shadow=new Color(.065f,.012f,.13f);mid=new Color(.55f,.12f,.9f);highlight=new Color(.95f,.82f,1);break;}
   material.SetColor("_Shadow",shadow);material.SetColor("_Mid",mid);material.SetColor("_Highlight",highlight);EditorUtility.SetDirty(material);
  }
 }
 public static void Apply()
 {
  Materials();
  var old=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Resources/Iron.png");
  var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Resources/UI/IronOre.png");
  var carry=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Resources/Carry/IronOre.png");
  var ground=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Resources/Ground/IronOre.png");
  var iron=AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/Iron.asset");iron.resourceIcon=icon;iron.defaultCarrySprite=carry;iron.groundSprite=ground;EditorUtility.SetDirty(iron);
  string oldPath=AssetDatabase.GetAssetPath(old);
  foreach(string path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs"}).Select(AssetDatabase.GUIDToAssetPath))
  {
   var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
   bool replace=old!=null&&AssetDatabase.GetDependencies(path).Contains(oldPath);
   bool palette=asset.GetComponentsInChildren<Image>(true).Any(i=>DamagePalette.IconMaterial(i.sprite)!=null);
   if(!replace&&!palette)continue;
   var root=PrefabUtility.LoadPrefabContents(path);
   try {
    if(replace)foreach(var component in root.GetComponentsInChildren<Component>(true))
    {if(component==null)continue;var so=new SerializedObject(component);var property=so.GetIterator();bool changed=false;while(property.Next(true))if(property.propertyType==SerializedPropertyType.ObjectReference&&property.objectReferenceValue==old){property.objectReferenceValue=property.name=="carrySprite"?carry:component is SpriteRenderer?ground:icon;changed=true;}if(changed)so.ApplyModifiedPropertiesWithoutUndo();}
    foreach(var image in root.GetComponentsInChildren<Image>(true)){var mat=DamagePalette.IconMaterial(image.sprite);if(mat!=null)image.material=mat;}
    PrefabUtility.SaveAsPrefabAsset(root,path);
   }finally{PrefabUtility.UnloadPrefabContents(root);}
  }
  AssetDatabase.SaveAssets();
  if(old!=null){var refs=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&p!=oldPath&&new[]{".prefab",".unity",".asset",".mat",".controller",".anim",".spriteatlas",".spriteatlasv2"}.Contains(Path.GetExtension(p))&&AssetDatabase.GetDependencies(p,false).Contains(oldPath)).ToArray();if(refs.Length>0)throw new Exception("Old iron still referenced: "+string.Join(",",refs));AssetDatabase.DeleteAsset(oldPath);}
  AssetDatabase.SaveAssets();
 }
}
#endif

