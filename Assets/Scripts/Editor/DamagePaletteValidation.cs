#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using GameFoundation.MetaProgression;
public static class DamagePaletteValidation
{
 static void Check(bool v,string message){if(!v)throw new Exception(message);}
 public static void Run()
 {
  Check(!System.IO.File.Exists("Assets/Sprites/Resources/Iron.png"),"Obsolete iron artwork still exists");
  var iron=AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/Iron.asset");
  Check(AssetDatabase.GetAssetPath(iron.resourceIcon)=="Assets/Sprites/Resources/UI/IronOre.png","Iron UI still uses old sprite");
  foreach(var type in new[]{DamageType.Fire,DamageType.Ice,DamageType.Electric,DamageType.Magic})
  {var world=DamagePalette.MaterialFor(type);var ui=DamagePalette.MaterialFor(type,true);Check(world!=null&&ui!=null&&!ShaderUtil.ShaderHasError(world.shader)&&!ShaderUtil.ShaderHasError(ui.shader),"Missing or broken palette shader "+type);Check(world.GetColor("_Mid")==ui.GetColor("_Mid")&&world.GetColor("_Highlight")==ui.GetColor("_Highlight"),"UI/world palettes differ "+type);}
  var balance=AssetDatabase.LoadAssetAtPath<PortalTowerBalance>("Assets/Resources/World/PortalTowerBalance.asset");
  var weapons=new[]{PortalTowerBalance.Weapon.Bolts,PortalTowerBalance.Weapon.Beam,PortalTowerBalance.Weapon.Lightning,PortalTowerBalance.Weapon.FrostSeal,PortalTowerBalance.Weapon.FireShot,PortalTowerBalance.Weapon.ElectricOrb};
  var root=new GameObject("Palette QA preview");
  var camera=new GameObject("Palette QA camera").AddComponent<Camera>();camera.transform.SetParent(root.transform);camera.orthographic=true;camera.orthographicSize=3.5f;camera.transform.position=new Vector3(0,0,-10);camera.cullingMask=1<<30;camera.backgroundColor=new Color(.06f,.045f,.08f);camera.clearFlags=CameraClearFlags.SolidColor;
  var canvasGo=new GameObject("Palette UI",typeof(Canvas),typeof(CanvasScaler));canvasGo.transform.SetParent(root.transform);canvasGo.layer=30;var canvas=canvasGo.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;var scaler=canvasGo.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(640,360);
  var font=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Base/Blacksmith Popup.prefab").GetComponentsInChildren<Text>(true).First().font;
  var names=new[]{"MAGIC","FIRE BEAM","LIGHTNING","FROST","FIRE SHOT","ELECTRIC ORB"};
  for(int i=0;i<weapons.Length;i++)
  {
   var def=balance.FindWeapon(weapons[i]);var mat=DamagePalette.IconMaterial(def.icon);Check(mat==DamagePalette.MaterialFor(def.damageType,true),"Weapon icon palette mismatch "+weapons[i]);
   var imageGo=new GameObject(names[i],typeof(RectTransform),typeof(Image));imageGo.transform.SetParent(canvasGo.transform,false);imageGo.layer=30;var image=imageGo.GetComponent<Image>();ResourceIconSizing.Apply(image,def.icon);image.rectTransform.anchorMin=image.rectTransform.anchorMax=new Vector2(.5f,.5f);image.rectTransform.anchoredPosition=new Vector2((i-2.5f)*100,80);
   var labelGo=new GameObject("Label",typeof(RectTransform),typeof(Text));labelGo.transform.SetParent(canvasGo.transform,false);labelGo.layer=30;var label=labelGo.GetComponent<Text>();label.font=font;label.text=names[i];label.fontSize=12;label.alignment=TextAnchor.MiddleCenter;label.color=new Color(.9f,.86f,.75f);label.rectTransform.sizeDelta=new Vector2(100,30);label.rectTransform.anchoredPosition=new Vector2((i-2.5f)*100,35);
   Sprite sprite=def.icon;
   if(weapons[i]==PortalTowerBalance.Weapon.Lightning)sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Effects/Portal Pixel Lightning.png");
   if(weapons[i]==PortalTowerBalance.Weapon.FrostSeal)sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/World/Hexes/Frost Seal.png");
   if(weapons[i]==PortalTowerBalance.Weapon.FireShot)sprite=AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(def.icon)).OfType<Sprite>().First(s=>s.name=="projectile_FirePellet");
   var go=new GameObject("World effect",typeof(SpriteRenderer));go.layer=30;go.transform.SetParent(root.transform);go.transform.position=new Vector3((i-2.5f)*1.8f,-1,0);var visual=go.GetComponent<SpriteRenderer>();visual.sprite=sprite;visual.sharedMaterial=DamagePalette.MaterialFor(def.damageType);float scale=1.2f/Mathf.Max(sprite.bounds.size.x,sprite.bounds.size.y);go.transform.localScale=Vector3.one*scale;visual.color=new Color(1,1,1,weapons[i]==PortalTowerBalance.Weapon.FrostSeal?.5f:.78f);
   if(weapons[i]==PortalTowerBalance.Weapon.Bolts||weapons[i]==PortalTowerBalance.Weapon.FireShot||weapons[i]==PortalTowerBalance.Weapon.ElectricOrb)
   {string path=weapons[i]==PortalTowerBalance.Weapon.Bolts?"PlayerTowerProjectile":weapons[i]==PortalTowerBalance.Weapon.FireShot?"Portal FireShot":"Portal ElectricOrb";var shot=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectiles/"+path+".prefab"));shot.GetComponent<EnemyProjectile>().SetupHit(Vector3.zero,"Enemy1",1,def.damageType,null);Check(shot.GetComponentsInChildren<SpriteRenderer>().Where(s=>s.enabled).All(s=>s.sharedMaterial==DamagePalette.MaterialFor(def.damageType)),"Runtime projectile palette mismatch");shot.SetActive(false);UnityEngine.Object.Destroy(shot);}
  }
  var rt=new RenderTexture(1280,720,24);var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);var previous=RenderTexture.active;
  camera.targetTexture=rt;foreach(var graphic in canvasGo.GetComponentsInChildren<Graphic>())graphic.SetAllDirty();Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();System.IO.Directory.CreateDirectory("Temp/DamagePaletteQA");System.IO.File.WriteAllBytes("Temp/DamagePaletteQA/palette.png",texture.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.Destroy(texture);UnityEngine.Object.Destroy(rt);root.SetActive(false);UnityEngine.Object.Destroy(root);
  SessionState.SetString("DamagePaletteQAResult","PASS: six matching weapon icons/effects, runtime projectile palettes, both shader variants, current iron sprite and removed obsolete asset");
 }
}
#endif
