using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using GameFoundation.Base;

public static class ConstructionTooltipValidation
{
 public static string Run()
 {
  if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode first");
  var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
  var rt=new RenderTexture(800,500,24);var old=RenderTexture.active;
  try
  {
   UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
   var camera=new GameObject("Tooltip QA camera").AddComponent<Camera>();camera.orthographic=true;camera.transform.position=new Vector3(0,0,-10);camera.backgroundColor=new Color(.04f,.03f,.06f);camera.clearFlags=CameraClearFlags.SolidColor;camera.targetTexture=rt;camera.cullingMask=1<<30;
   var canvas=new GameObject("Tooltip QA canvas",typeof(Canvas)).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
   var tooltip=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Base/Building Construction Tooltip.prefab"),canvas.transform).GetComponent<BuildingConstructionTooltip>();
   foreach(var child in canvas.GetComponentsInChildren<Transform>(true))child.gameObject.layer=30;
   typeof(BuildingConstructionTooltip).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(tooltip,null);
   var wood=AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/Wood.asset");var stone=AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/Stone.asset");
   float Show(string file,string title,string description,int woodCost,int stoneCost)
   {
    tooltip.Show(title,description,"Стоимость",wood,woodCost,stone,stoneCost);var rect=(RectTransform)tooltip.transform;
    rect.anchoredPosition=new Vector2(-rect.rect.width*.5f,rect.rect.height*.5f);Canvas.ForceUpdateCanvases();
    camera.Render();RenderTexture.active=rt;var image=new Texture2D(800,500,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,800,500),0,0);image.Apply();Directory.CreateDirectory("Art/ConstructionTooltipQA");File.WriteAllBytes("Art/ConstructionTooltipQA/"+file+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
    return rect.rect.height;
   }
   float free=Show("free","Лагерь беженцев","Позволяет принимать новых жителей в поселение.",0,0);
   float paid=Show("resources","Лаборатория","Позволяет изучать новые технологии.",10,5);
   float longText=Show("long","Магическая библиотека и исследование тёмных земель","Позволяет изучать новых врагов и их особенности. Здесь жители записывают знания о тёмных землях, чтобы лучше защищать поселение и находить слабые стороны противников.",10,0);
   float shrink=Show("free-after-long","Лагерь беженцев","Позволяет принимать новых жителей в поселение.",0,0);
   if(free>=250||paid<=free||longText<=paid||Mathf.Abs(shrink-free)>.1f)throw new Exception($"Unexpected heights: free={free}, paid={paid}, long={longText}, shrink={shrink}");
   return $"PASS: free {free}, resources {paid}, long {longText}, short again {shrink} Canvas units";
  }
  finally{RenderTexture.active=old;UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
 }
}
