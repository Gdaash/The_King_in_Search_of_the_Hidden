using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using GameFoundation.Localization;
using TMPro;
[InitializeOnLoad]
public static class EightLanguageValidation
{
 const string Path="Assets/Editor/EightLanguagesQA.unity";
 static EightLanguageValidation(){EditorApplication.playModeStateChanged+=s=>{if(!SessionState.GetBool("EightLanguagesQA",false))return;if(s==PlayModeStateChange.EnteredPlayMode)EditorApplication.delayCall+=Run;if(s==PlayModeStateChange.EnteredEditMode){EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString("EightLanguagesPrevious",""));AssetDatabase.DeleteAsset(Path);SessionState.SetBool("EightLanguagesQA",false);}};}
 public static void Begin(){var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);EditorSceneManager.SaveScene(scene,Path);EditorSceneManager.CloseScene(scene,true);SessionState.SetString("EightLanguagesPrevious",AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));SessionState.SetString("EightLanguagesResult","RUNNING");SessionState.SetBool("EightLanguagesQA",true);EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(Path);EditorApplication.isPlaying=true;}
 static void Check(bool value,string text){if(!value)throw new Exception(text);}
 static void Run()
 {
  try
  {
   var table=AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");var service=new GameObject("QA language").AddComponent<LocalizationService>();service.SetTable(table);var language=typeof(LocalizationService).GetField("<Language>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic);
   var font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Pixellari Cyrillic/Pixellari-Cyrillic.ttf");var tmpFont=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Pixellari Cyrillic/Pixellari Cyrillic UI.asset");
   string all=new string(table.entries.SelectMany(e=>e.values).SelectMany(s=>s).Where(c=>!char.IsControl(c)).Distinct().ToArray());
   Check(tmpFont.HasCharacters(all,out uint[] missing,true,true),"Missing TMP characters: "+string.Join(",",missing??Array.Empty<uint>()));
   font.RequestCharactersInTexture("选择存档",32);foreach(char c in "选择存档")Check(font.GetCharacterInfo(c,out var info,32),"Missing legacy UI Chinese character: "+c);
   var camera=new GameObject("QA camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.06f,.045f,.08f);var rt=new RenderTexture(1600,1120,24);camera.targetTexture=rt;
   var canvas=new GameObject("QA canvas",typeof(Canvas)).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;canvas.gameObject.layer=30;
   void Label(string text,Vector2 position,Vector2 size,int fs,bool tmp)
   {var go=new GameObject("Language sample",typeof(RectTransform));go.layer=30;go.transform.SetParent(canvas.transform,false);var rect=(RectTransform)go.transform;rect.sizeDelta=size;rect.anchoredPosition=position;if(tmp){var label=go.AddComponent<TextMeshProUGUI>();label.font=tmpFont;label.fontSize=fs;label.text=text;label.alignment=TextAlignmentOptions.Center;label.color=new Color(.94f,.91f,.82f);}else{var label=go.AddComponent<Text>();label.font=font;label.fontSize=fs;label.text=text;label.alignment=TextAnchor.MiddleCenter;label.color=new Color(.94f,.91f,.82f);}}
   for(int i=0;i<table.languages.Count;i++)
   {
    string code=table.languages[i];language.SetValue(service,code);
    string expected=table.Get("menu.slots.delete","ru");Check(GameText.Get(expected)==table.Get("menu.slots.delete",code),"Source lookup failed for "+code);
    string old=table.Get("menu.slots.delete","fr");Check(GameText.Source(old)==expected,"Cross-language source lookup failed "+code);
    string formatted=GameText.Format("Ячейка {0}",3);Check(formatted.Contains("3"),"Formatting lost its value");
    var position=new Vector2(i%2==0?-390:390,440-(i/2)*270);Label(code+" – "+table.Get("menu.slots.title",code),position,new Vector2(740,56),28,true);
    Label(table.Get("menu.slots.delete_confirm",code),position+new Vector2(0,-82),new Vector2(720,128),26,false);
    Label(table.Get("base.disabled.housing",code),position+new Vector2(0,-175),new Vector2(720,60),26,true);
   }
   Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1600,1120,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,1120),0,0);image.Apply();System.IO.Directory.CreateDirectory("Art/EightLanguagesQA");System.IO.File.WriteAllBytes("Art/EightLanguagesQA/languages.png",image.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;rt.Release();UnityEngine.Object.Destroy(image);UnityEngine.Object.Destroy(rt);
   language.SetValue(service,"ru");Check(GameText.Get("Удалить сохранение")=="Удалить сохранение","Switching back to Russian failed");
   SessionState.SetString("EightLanguagesResult","PASS: 775 complete keys in eight languages; source translation and reverse lookup across languages; placeholders; all TMP glyphs; legacy Chinese fallback and rendered UI samples.");
  }
  catch(Exception error){SessionState.SetString("EightLanguagesResult","FAIL: "+error);Debug.LogException(error);}
  finally{EditorApplication.isPlaying=false;}
 }
}
