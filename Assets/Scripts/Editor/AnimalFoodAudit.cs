using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
public static class AnimalFoodAudit
{
 const string Report="Temp/AnimalFoodAudit.txt";
 static int checks;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;File.AppendAllText(Report,"PASS "+message+"\n");}
 public static async Task RunPlay()
 {
  checks=0;File.WriteAllText(Report,"Animal food Play Mode audit\n");
  foreach(var kind in new[]{"Chickens","Boars"})for(int cycles=1;cycles<=3;cycles++)
  {
   var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Buildings/Animals/{kind} {cycles}.prefab"),Vector3.zero,Quaternion.identity);root.SetActive(true);
   await Task.Delay(100);
   var walkers=root.GetComponentsInChildren<HexAnimalWander>();
   Check(walkers.Length==cycles*3,kind+" "+cycles+" initial population");
   foreach(var walker in walkers)
   {
    int moved=0,paused=0;bool inside=true;
    for(int step=0;step<2400;step++){
     var before=walker.transform.localPosition;walker.Advance(.025f);var after=walker.transform.localPosition;
     if((after-before).sqrMagnitude>.000001f)moved++;else paused++;
     inside &= Mathf.Abs(after.y)<=.901f && Mathf.Abs(after.x)<=1.1f*(1f-.5f*Mathf.Abs(after.y)/.9f)+.001f;
    }
    Check(inside&&moved>0&&paused>0,"bounded wandering with pauses");
    var pos=walker.transform.localPosition;walker.Advance(0);Check(walker.transform.localPosition==pos,"paused simulation stays still");
   }
   var timer=root.GetComponentsInChildren<TimerController>(true).First(t=>new SerializedObject(t).FindProperty("stats").objectReferenceValue!=null);
   for(int done=1;done<=cycles;done++)
   {
    timer.OnTimerEnd.Invoke();
    Check(root.GetComponentsInChildren<HexAnimalWander>().Length==(cycles-done)*3,kind+" remaining population after cycle "+done);
    var food=Object.FindObjectsByType<ResourceItem>(FindObjectsSortMode.None).Where(x=>x.type.Id=="Berry").ToArray();
    Check(food.Length==done*3,"food drops after cycle "+done);
   }
   await Task.Delay(800);
   Check(!root.activeSelf,"exhausted food hex shuts down");
   Object.Destroy(root);
   foreach(var item in Object.FindObjectsByType<ResourceItem>(FindObjectsSortMode.None))Object.Destroy(item.gameObject);
   foreach(var human in Object.FindObjectsByType<HumanUnit>(FindObjectsSortMode.None))Object.Destroy(human.gameObject);
   await Task.Delay(50);
  }
  File.AppendAllText(Report,"ALL PASSED: "+checks+" checks\n");
 }
 public static void Render()
 {
  var scene=EditorSceneManager.NewPreviewScene();RenderTexture rt=null;Texture2D image=null;var old=RenderTexture.active;Material material=null;
  try{
   material=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
   var camGo=new GameObject("Animals preview camera");SceneManager.MoveGameObjectToScene(camGo,scene);var cam=camGo.AddComponent<Camera>();cam.scene=scene;cam.orthographic=true;cam.orthographicSize=4;cam.transform.position=new Vector3(0,0,-10);cam.backgroundColor=new Color(.09f,.08f,.12f);cam.clearFlags=CameraClearFlags.SolidColor;
   for(int row=0;row<2;row++)for(int cycles=1;cycles<=3;cycles++){
    var kind=row==0?"Chickens":"Boars";var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Buildings/Animals/{kind} {cycles}.prefab"));SceneManager.MoveGameObjectToScene(go,scene);go.transform.position=new Vector3((cycles-2)*3.6f,row==0?1.8f:-1.8f,0);
    foreach(var canvas in go.GetComponentsInChildren<Canvas>(true))canvas.gameObject.SetActive(false);
    foreach(var sr in go.GetComponentsInChildren<SpriteRenderer>(true))sr.sharedMaterial=material;
   }
   rt=new RenderTexture(768,512,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;image=new Texture2D(768,512,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,768,512),0,0);image.Apply();File.WriteAllBytes("Temp/AnimalFoodPreview.png",image.EncodeToPNG());cam.targetTexture=null;
  }finally{RenderTexture.active=old;if(rt)Object.DestroyImmediate(rt);if(image)Object.DestroyImmediate(image);if(material)Object.DestroyImmediate(material);EditorSceneManager.ClosePreviewScene(scene);}
 }
}

