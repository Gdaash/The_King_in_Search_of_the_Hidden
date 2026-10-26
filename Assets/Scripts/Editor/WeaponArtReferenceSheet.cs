#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using GameFoundation.MetaProgression;
using Object=UnityEngine.Object;

public static class WeaponArtReferenceSheet
{
 const string Folder="Art/WeaponArtReview-20261020";
 static readonly List<string> manifest=new();
 static Font font;static Sprite pixel;static Sprite orb,pellet,hex;static Transform canvas;
 static int index;
 static RectTransform Rect(Transform parent,string name,Vector2 size,Vector2 pos)
 {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.sizeDelta=size;r.anchoredPosition=pos;r.gameObject.layer=30;return r;}
 static Text Text(Transform parent,string value,int size,Vector2 box,Vector2 pos,Color color)
 {var r=Rect(parent,value,box,pos);var t=r.gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.text=value;t.alignment=TextAnchor.MiddleCenter;t.color=color;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.raycastTarget=false;return t;}
 static Image Image(Transform parent,Sprite sprite,Vector2 size,Vector2 pos,Material material=null,Color? tint=null,float rotation=0)
 {var r=Rect(parent,sprite!=null?sprite.name:"shape",size,pos);r.localRotation=Quaternion.Euler(0,0,rotation);var im=r.gameObject.AddComponent<Image>();im.sprite=sprite;im.material=material;im.color=tint??Color.white;im.preserveAspect=true;im.raycastTarget=false;return im;}
 static Vector2 Fit(Sprite sprite,float width=210,float height=172)
 {if(sprite==null)return Vector2.zero;float s=Mathf.Min(width/sprite.rect.width,height/sprite.rect.height);if(s>=1)s=Mathf.Floor(s);return sprite.rect.size*s;}
 static RectTransform Cell(string id,string title,string source)
 {
  int col=index%8,row=index/8;index++;
  var r=Rect(canvas,id,new Vector2(286,278),new Vector2(-1050+col*300,735-row*292));
  var bg=r.gameObject.AddComponent<Image>();bg.color=new Color(.09f,.075f,.12f);bg.raycastTarget=false;
  Text(r,id+"  "+title,18,new Vector2(274,54),new Vector2(0,-103),new Color(.88f,.84f,.73f));
  manifest.Add(id+"\t"+title+"\t"+source);
  return r;
 }
 static void SpriteCell(string id,string title,Sprite sprite,DamageType? damage=null,float rotation=0,string source=null)
 {var r=Cell(id,title,source??AssetDatabase.GetAssetPath(sprite));Image(r,sprite,Fit(sprite),new Vector2(0,20),damage.HasValue?DamagePalette.MaterialFor(damage.Value,true):DamagePalette.IconMaterial(sprite),rotation:rotation);}
 static void Line(Transform p,Vector2 a,Vector2 b,float width,Color color)
 {var delta=b-a;var r=Rect(p,"pixel trail",new Vector2(delta.magnitude,width),(a+b)*.5f);r.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);var im=r.gameObject.AddComponent<Image>();im.color=color;im.raycastTarget=false;}
 static void Ring(Transform p,Vector2 center,float radius,Color color)
 {for(int j=0;j<40;j++){float a=j*Mathf.PI*2/40,b=(j+1)*Mathf.PI*2/40;Line(p,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a)) *radius,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,3,color);}}
 static void Impact(Transform p,Color color)
 {foreach(var d in new[]{new Vector2(-1,.35f),new Vector2(1,.35f),new Vector2(-.35f,1),new Vector2(.35f,-1)}){var a=d*24+Vector2.up*20;var b=d*65+Vector2.up*20;Line(p,a,b,6,color);Image(p,pixel,new Vector2(9,9),b,null,color);}}
 static void Cone(Transform parent,Color color)
 {
  // Point light is procedural in-game; stepped slices expose its footprint on the reference sheet.
  for(int j=0;j<20;j++){float t=j/19f;Image(parent,pixel,new Vector2(5+t*110,5),new Vector2(0,98-j*6),null,new Color(color.r,color.g,color.b,.2f+.5f*t)).preserveAspect=false;}
  Image(parent,hex,new Vector2(134,85),new Vector2(0,-24),DamagePalette.MaterialFor(color==DamagePalette.Fire?DamageType.Fire:DamageType.Electric,true),new Color(1,1,1,.65f));
 }
 public static string Export()
 {
  Directory.CreateDirectory(Folder);manifest.Clear();manifest.Add("ID\tLabel\tSource");index=0;
  var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
  RenderTexture rt=null;Texture2D texture=null;Texture2D white=null;var old=RenderTexture.active;
  try {
   var fontSource=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Base/Blacksmith Popup.prefab");font=fontSource.GetComponentsInChildren<Text>(true).First().font;
   white=new Texture2D(1,1,TextureFormat.RGBA32,false);white.SetPixel(0,0,Color.white);white.Apply();pixel=Sprite.Create(white,new UnityEngine.Rect(0,0,1,1),Vector2.one*.5f,32);
   var cg=new GameObject("Reference camera");var camera=cg.AddComponent<Camera>();camera.orthographic=true;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.025f,.05f);camera.cullingMask=1<<30;
   var canvasGo=new GameObject("Reference board",typeof(Canvas),typeof(CanvasScaler));canvasGo.layer=30;canvas=canvasGo.transform;var cv=canvasGo.GetComponent<Canvas>();cv.renderMode=RenderMode.ScreenSpaceCamera;cv.worldCamera=camera;cv.planeDistance=1;
   var scaler=canvasGo.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(2400,1920);scaler.matchWidthOrHeight=1;
   Text(canvas,"THOSE UNDERHEX  /  CURRENT WEAPON ART INVENTORY",32,new Vector2(2350,42),new Vector2(0,935),new Color(.95f,.84f,.58f));
   Text(canvas,"01-16 combat icons  |  P projectiles  |  T towers  |  E equipment  |  L laboratory  |  F effects",20,new Vector2(2350,30),new Vector2(0,895),new Color(.72f,.68f,.78f));
   var balance=AssetDatabase.LoadAssetAtPath<PortalTowerBalance>("Assets/Resources/World/PortalTowerBalance.asset");
   for(int i=0;i<balance.upgrades.Count;i++){var u=balance.upgrades[i];var w=balance.FindWeapon(u.weapon);bool weapon=u.effect==PortalTowerBalance.Effect.UnlockWeapon;SpriteCell("I"+(i+1).ToString("00"),u.englishTitle+(weapon&&w!=null&&w.disabled?" [inactive]":""),u.icon,weapon&&w!=null?w.damageType:(DamageType?)null);}
   var projectilePaths=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>AssetDatabase.LoadAssetAtPath<GameObject>(p).GetComponent<EnemyProjectile>()!=null).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
   for(int i=0;i<projectilePaths.Length;i++)
   {string path=projectilePaths[i];var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);var s=root.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.enabled&&r.sprite!=null);var type=path.Contains("ElectricOrb")?DamageType.Electric:path.Contains("FireShot")?DamageType.Fire:path.Contains("Mage")||path.Contains("Magic")||path.Contains("PlayerTower")?DamageType.Magic:DamageType.Physical;SpriteCell("P"+(i+1).ToString("00"),System.IO.Path.GetFileNameWithoutExtension(path),s.sprite,type,s.transform.localEulerAngles.z,path);}
   int n=0;foreach(string name in new[]{"Arrow","Stone","Magic"})SpriteCell("T"+(++n).ToString("00"),name+" defense tower",AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/World/Defense Towers/"+name+" Tower.png"));
   n=0;foreach(string name in new[]{"Sword","Bow","Cart"}){var resource=AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/"+name+".asset");SpriteCell("E"+(++n).ToString("00"),name+" / equipment upgrades",resource.resourceIcon);}
   n=0;foreach(var group in AssetDatabase.LoadAssetAtPath<ScientificUpgradeTable>("Assets/Resources/ScientificUpgradeTable.asset").entries.Where(e=>e.icon!=null&&e.icon.name!="lights").GroupBy(e=>e.icon))SpriteCell("L"+(++n).ToString("00"),group.Key.name+" / "+(group.Key.name=="Sword"?"gather speed":group.Key.name=="Eye"?"quiet gathering":group.Key.name=="Magic"?"crystal research":group.Key.name=="Healing"?"healing":group.Key.name=="Shield"?"tower health":"scout speed"),group.Key);
   orb=balance.FindWeapon(PortalTowerBalance.Weapon.ElectricOrb).icon;pellet=AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(orb)).OfType<Sprite>().First(s=>s.name=="projectile_FirePellet");
   hex=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/World/Hexes/Hex 3.png");
   var fx=Cell("F01","Burning beam + ground light","Portal Burning Beam.prefab / procedural Light2D");Cone(fx,DamagePalette.Fire);for(int j=0;j<4;j++)Line(fx,new Vector2(-65+j*42,-24),new Vector2(-65+j*42,5+j*11),3,new Color(1,.45f,.12f,.7f));
   SpriteCell("F02","Lightning strike",AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Effects/Portal Pixel Lightning.png"),DamageType.Electric);
   fx=Cell("F03","Frost seal + rising snow","Frost Seal.png / PortalWeaponController.Snow.cs");var frost=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/World/Hexes/Frost Seal.png");Image(fx,frost,Fit(frost),new Vector2(0,16),DamagePalette.MaterialFor(DamageType.Ice,true),new Color(1,1,1,.5f));foreach(var p in new[]{new Vector2(-55,25),new Vector2(20,65),new Vector2(65,22),new Vector2(-10,-15)}){Line(fx,p-Vector2.right*6,p+Vector2.right*6,3,DamagePalette.Ice);Line(fx,p-Vector2.up*6,p+Vector2.up*6,3,DamagePalette.Ice);}
   fx=Cell("F04","Force rings [inactive]","PortalWeaponController.Ring / procedural LineRenderer");Ring(fx,new Vector2(0,20),35,DamagePalette.Magic);Ring(fx,new Vector2(0,20),65,new Color(.7f,.4f,1,.7f));Ring(fx,new Vector2(0,20),87,new Color(.7f,.4f,1,.35f));
   fx=Cell("F05","Fire shot - 3 pellet fan","Portal FireShot.prefab");for(int j=-1;j<=1;j++)Image(fx,pellet,Fit(pellet,52,100),new Vector2(j*65,20+j*12),DamagePalette.MaterialFor(DamageType.Fire,true),new Color(1,1,1,.78f),-90+j*9);
   SpriteCell("F06","Electric orb - flight",orb,DamageType.Electric);
   fx=Cell("F07","Magic hit burst","CombatImpactBurst / 4 pixel shards");Impact(fx,DamagePalette.Magic);
   fx=Cell("F08","Fire hit burst","CombatImpactBurst / 4 pixel shards");Impact(fx,DamagePalette.Fire);
   fx=Cell("F09","Orb impact - expand / collapse","EnemyProjectile death / 1 -> 1.3 -> 0");for(int j=0;j<3;j++)Image(fx,orb,Vector2.one*(j==0?58:j==1?76:18),new Vector2(-80+j*80,20),DamagePalette.MaterialFor(DamageType.Electric,true),new Color(1,1,1,j==2?.4f:.78f),j*75);
   fx=Cell("F10","Working beacon light","Hex 3 light cookie / CrystalLightBeam");Cone(fx,DamagePalette.Electric);
   fx=Cell("F11","Spawn vortex - smoke tails","WaveSpawnTelegraph / Effects/Spawn Tentacle.png");var vortexGo=new GameObject("Vortex reference");var vortex=vortexGo.AddComponent<WaveSpawnTelegraph>();typeof(WaveSpawnTelegraph).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(vortex,null);foreach(var s in vortexGo.GetComponentsInChildren<SpriteRenderer>()){var r=Image(fx,s.sprite,s.sprite.bounds.size*100,new Vector2(s.transform.localPosition.x*100,s.transform.localPosition.y*100+20),null,s.color,s.transform.localEulerAngles.z).rectTransform;r.sizeDelta=Vector2.Scale(r.sizeDelta,new Vector2(s.transform.localScale.x,s.transform.localScale.y));}Object.DestroyImmediate(vortexGo);
   if(index!=48)throw new Exception("Inventory count changed, adjust sheet layout: "+index);
   rt=new RenderTexture(2400,1920,24);rt.antiAliasing=1;texture=new Texture2D(2400,1920,TextureFormat.RGB24,false);camera.targetTexture=rt;foreach(var graphic in canvasGo.GetComponentsInChildren<Graphic>())graphic.SetAllDirty();Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;texture.ReadPixels(new UnityEngine.Rect(0,0,2400,1920),0,0);texture.Apply();File.WriteAllBytes(Folder+"/reference-current.png",texture.EncodeToPNG());File.WriteAllLines(Folder+"/manifest.tsv",manifest);return "48 entries exported to "+Folder;
  }finally{RenderTexture.active=old;if(rt!=null){rt.Release();Object.DestroyImmediate(rt);}if(texture!=null)Object.DestroyImmediate(texture);if(pixel!=null)Object.DestroyImmediate(pixel);if(white!=null)Object.DestroyImmediate(white);EditorSceneManager.CloseScene(scene,true);UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);}
 }
}
#endif

