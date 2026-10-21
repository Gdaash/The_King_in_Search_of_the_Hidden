#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using GameFoundation.MetaProgression;
using Object=UnityEngine.Object;
[InitializeOnLoad] public static class WeaponEffectsValidation
{
 const string Path="Assets/Editor/WeaponEffectsQAScene.unity";
 static int step; static double stamp; static EnemyProjectile small,orb; static Transform sv,ov; static float ss,os; static ParticleSystem snow; static Camera cam;
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static WeaponEffectsValidation(){EditorApplication.playModeStateChanged+=s=>{if(!SessionState.GetBool("WeaponFXQA",false))return;if(s==PlayModeStateChange.EnteredPlayMode){step=0;stamp=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;}if(s==PlayModeStateChange.EnteredEditMode){EditorApplication.update-=Tick;CrystalAbundanceValidation.Cleanup();EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString("WeaponFXPrevious",""));AssetDatabase.DeleteAsset(Path);SessionState.SetBool("WeaponFXQA",false);}};}
 public static void Begin(){var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);EditorSceneManager.SaveScene(scene,Path);EditorSceneManager.CloseScene(scene,true);SessionState.SetString("WeaponFXPrevious",AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));SessionState.SetString("WeaponFXResult","RUNNING");SessionState.SetBool("WeaponFXQA",true);EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(Path);EditorApplication.isPlaying=true;}
 static void Check(bool x,string message){if(!x)throw new Exception(message);}
 static object Get(object o,string n)=>o.GetType().GetField(n,Flags).GetValue(o);
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,Flags).SetValue(o,v);
 static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,Flags).Invoke(o,args);
 static EnemyProjectile Spawn(string name,Vector3 pos){var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectiles/"+name+".prefab"),pos,Quaternion.identity);var p=go.GetComponent<EnemyProjectile>();p.SetupHit(Vector3.zero,"Enemy1",1,DamageType.Fire,null);return p;}
 static void Tick(){try{
 if(step==0){Time.timeScale=0;small=Spawn("Portal FireShot",new Vector3(-2,1));orb=Spawn("Portal ElectricOrb",new Vector3(0,1));sv=(Transform)Get(small,"animatedVisual");ov=(Transform)Get(orb,"animatedVisual");ss=sv.localScale.x;os=ov.localScale.x;
 Check(sv.GetComponent<SpriteRenderer>().sprite.name=="projectile_FirePellet","pellet slice");Check(ov.GetComponent<SpriteRenderer>().sprite.name=="weapon_ElectricOrb","orb icon");Check(ov.GetComponent<SpriteRenderer>().color.a<.8f,"transparency");
 var vortex=new GameObject("Vortex",typeof(WaveSpawnTelegraph));vortex.transform.position=new Vector3(2,0);Call(vortex.GetComponent<WaveSpawnTelegraph>(),"Update");Check(vortex.GetComponentsInChildren<SpriteRenderer>().Length==72&&vortex.GetComponentsInChildren<LineRenderer>().Length==0,"pixel vortex");
 var host=new GameObject("Snow test");var c=host.AddComponent<PortalWeaponController>();c.enabled=false;var tower=host.AddComponent<ArcherTower>();tower.enabled=false;Set(c,"tower",tower);Set(c,"effectMaterial",new Material(Shader.Find("Sprites/Default")));snow=(ParticleSystem)Call(c,"CreateFrostSnow",new Vector2(-1,-1),1f,.65f);snow.Simulate(.8f,true,true);Check(snow.particleCount>0,"snow emits");
 cam=new GameObject("QA camera",typeof(Camera)).GetComponent<Camera>();cam.transform.position=new Vector3(0,0,-10);cam.orthographic=true;cam.orthographicSize=2.4f;cam.backgroundColor=new Color(.07f,.06f,.1f);cam.clearFlags=CameraClearFlags.SolidColor;Capture();
 DamagePaletteValidation.Run();DepletionCrystalValidation.Prepare();small.BeginDeath();orb.BeginDeath();Check(small.GetComponentsInChildren<Collider2D>().All(x=>!x.enabled),"hit disables colliders");Set(small,"deathAge",.05f-Time.deltaTime);Call(small,"Update");Set(orb,"deathAge",.05f-Time.deltaTime);Call(orb,"Update");Check(Mathf.Abs(sv.localScale.x/ss-1.2f)<.01f,"small expansion");Check(Mathf.Abs(ov.localScale.x/os-1.3f)<.01f,"orb expansion");
 Set(small,"deathAge",.1f-Time.deltaTime);Call(small,"Update");Check(sv.GetComponent<SpriteRenderer>().color.a<.5f,"small fade");Set(orb,"deathAge",.17f-Time.deltaTime);Call(orb,"Update");Check(ov.localScale.x<os*.2f,"orb collapse");Check((float)Get(orb,"flightRotationSpeed")==85,"orb rotation configured");Set(small,"deathAge",.21f);Call(small,"Update");Set(orb,"deathAge",.21f);Call(orb,"Update");step=1;stamp=EditorApplication.timeSinceStartup;}
 else if(EditorApplication.timeSinceStartup-stamp>.3){DepletionCrystalValidation.Verify();UnitPresentationValidation.Run();Check(small==null&&orb==null,"projectiles removed");SessionState.SetString("WeaponFXResult","PASS: icon sprites, alpha, expansion, fade, collapse, collider disable, destruction, snow emission, 72 inward spiral tails");Time.timeScale=1;EditorApplication.isPlaying=false;EditorApplication.update-=Tick;}
 }catch(Exception e){SessionState.SetString("WeaponFXResult","FAIL: "+e);Debug.LogException(e);Time.timeScale=1;EditorApplication.isPlaying=false;EditorApplication.update-=Tick;}}
 static void Capture(){var rt=new RenderTexture(960,540,24);cam.targetTexture=rt;cam.Render();var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(960,540,TextureFormat.RGBA32,false);tex.ReadPixels(new Rect(0,0,960,540),0,0);tex.Apply();System.IO.Directory.CreateDirectory("Temp/WeaponFXQA");System.IO.File.WriteAllBytes("Temp/WeaponFXQA/preview.png",tex.EncodeToPNG());RenderTexture.active=old;cam.targetTexture=null;Object.Destroy(tex);Object.Destroy(rt);}
}
#endif






