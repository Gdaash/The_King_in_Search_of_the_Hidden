using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEngine.TextCore.LowLevel;
using GameFoundation.Localization;
public static class EightLanguageSetup
{
 public static string Apply()
 {
  var table=AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
  var imported=ScriptableObject.CreateInstance<LocalizationTable>();JsonUtility.FromJsonOverwrite(File.ReadAllText(".ai/localization-eight-languages/translated.json"),imported);
  table.languages=imported.languages;
  foreach(var entry in imported.entries){var existing=table.entries.Find(e=>e.key==entry.key);if(existing==null)table.entries.Add(entry);else existing.values=entry.values;}
  UnityEngine.Object.DestroyImmediate(imported);EditorUtility.SetDirty(table);AssetDatabase.SaveAssetIfDirty(table);
  var unifont=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Pixel UI/Unifont-17.0.04.otf");var europe=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Pixel UI/KingPixelEurope.ttf");
  foreach(string guid in AssetDatabase.FindAssets("t:Font",new[]{"Assets/Fonts"}))
  {
   string path=AssetDatabase.GUIDToAssetPath(guid);var importer=AssetImporter.GetAtPath(path) as TrueTypeFontImporter;if(importer==null||path.Contains("/Chinese/")||path.EndsWith("Unifont-17.0.04.otf"))continue;
   var refs=new List<Font>();if(!path.EndsWith("KingPixelEurope.ttf"))refs.Add(europe);refs.Add(unifont);importer.fontReferences=refs.ToArray();
   var names=importer.fontNames.ToList();if(!path.EndsWith("KingPixelEurope.ttf")&&!names.Contains("King Pixel Europe"))names.Add("King Pixel Europe");if(!names.Contains("Unifont"))names.Add("Unifont");importer.fontNames=names.ToArray();importer.SaveAndReimport();
  }
  const string chinesePath="Assets/Fonts/Pixel UI/Chinese UI.asset";var chinese=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(chinesePath);
  if(chinese==null){chinese=TMP_FontAsset.CreateFontAsset(unifont,32,0,GlyphRenderMode.RASTER_HINTED,1024,1024,AtlasPopulationMode.Dynamic,true);chinese.name="Chinese UI";AssetDatabase.CreateAsset(chinese,chinesePath);AssetDatabase.AddObjectToAsset(chinese.material,chinese);foreach(var atlas in chinese.atlasTextures)AssetDatabase.AddObjectToAsset(atlas,chinese);}
  var european=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Pixel UI/King Pixel Europe UI.asset");var last=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Pixel UI/Unifont Last Resort.asset");
  foreach(string guid in AssetDatabase.FindAssets("t:TMP_FontAsset",new[]{"Assets/Fonts"}))
  {
   var asset=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));var so=new SerializedObject(asset);var clear=so.FindProperty("m_ClearDynamicDataOnBuild");if(clear!=null)clear.boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();
   if(asset!=chinese&&asset!=last){asset.fallbackFontAssetTable=new List<TMP_FontAsset>();if(asset!=european)asset.fallbackFontAssetTable.Add(european);asset.fallbackFontAssetTable.Add(chinese);asset.fallbackFontAssetTable.Add(last);}
   foreach(var atlas in asset.atlasTextures)if(atlas!=null)atlas.filterMode=FilterMode.Point;
   EditorUtility.SetDirty(asset);AssetDatabase.SaveAssetIfDirty(asset);
  }
  if(table.languages.Count!=8||table.entries.Any(e=>e.values.Count!=8||e.values.Any(string.IsNullOrWhiteSpace)))throw new Exception("Incomplete translation table");
  ChineseReadabilitySetup.Apply();
  return table.entries.Count+" keys in eight languages; European and Chinese fallbacks configured for legacy UI and TMP";
 }
}
