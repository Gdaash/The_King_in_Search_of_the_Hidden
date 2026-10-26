using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using GameFoundation.MetaProgression;

// Extracts only icon cells from the approved review sheet. Gameplay art stays separate.
public static class ApprovedWeaponIconsSetup
{
 const string Folder="Assets/Sprites/Ui/Authored Icons";
 static readonly string[] Names={"weapon_Bolts","weapon_Beam","weapon_Rings","weapon_Lightning","lights","weapon_FrostSeal","weapon_FireShot","weapon_ElectricOrb","all_damage","projectile_count","tick_interval","attack_interval","area_duration","all_range","projectile_speed","area_size","Sword","Bow","Cart","Gathering","Quiet","Shield","CrystalResearch","ScoutSpeed","Healing"};
 static readonly int[] Cells={0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,28,29,30,31,32,33,34,35,36};
 public static string ValidateAndPreview()
 {
  var preview=new Texture2D(800,800,TextureFormat.RGBA32,false);var pixels=new Color[800*800];
  for(int p=0;p<pixels.Length;p++)pixels[p]=new Color(.07f,.055f,.09f);
  for(int i=0;i<Names.Length;i++)
  {
   string path=Folder+"/Authored_"+Names[i]+".png";var icon=AssetDatabase.LoadAssetAtPath<Sprite>(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
   if(icon==null||importer.spritePixelsPerUnit!=32||importer.filterMode!=FilterMode.Point||importer.textureCompression!=TextureImporterCompression.Uncompressed||DamagePalette.IconMaterial(icon)!=null)throw new Exception("Bad icon import: "+path);
   var raw=new Texture2D(2,2);raw.LoadImage(File.ReadAllBytes(path));var colors=raw.GetPixels();int mult=4,ox=(i%5)*160+(160-raw.width*mult)/2,oy=(4-i/5)*160+(160-raw.height*mult)/2;
   if(!colors.Any(c=>c.a==0)||!colors.Any(c=>c.a>.9f))throw new Exception("Missing transparency or artwork: "+path);
   for(int y=0;y<raw.height*mult;y++)for(int x=0;x<raw.width*mult;x++){var c=colors[(y/mult)*raw.width+x/mult];int p=(y+oy)*800+x+ox;pixels[p]=Color.Lerp(pixels[p],c,c.a);}
   UnityEngine.Object.DestroyImmediate(raw);
  }
  preview.SetPixels(pixels);preview.Apply();File.WriteAllBytes("Art/WeaponArtReview-20261020/installed-icons.png",preview.EncodeToPNG());UnityEngine.Object.DestroyImmediate(preview);
  var b=AssetDatabase.LoadAssetAtPath<PortalTowerBalance>("Assets/Resources/World/PortalTowerBalance.asset");
  if(b.upgrades.Any(u=>u.icon==null||!u.icon.name.StartsWith("Authored_"))||b.weapons.Any(w=>w.icon==null||!w.icon.name.StartsWith("Authored_")))throw new Exception("Catalog has old or missing icons");
  var table=AssetDatabase.LoadAssetAtPath<ScientificUpgradeTable>("Assets/Resources/ScientificUpgradeTable.asset");
  if(table.entries.Any(e=>e.icon==null||!e.icon.name.StartsWith("Authored_")))throw new Exception("Laboratory has old or missing icons");
  return "PASS: 25 transparent Point/uncompressed sprites at PPU32; all weapon, upgrade and laboratory references valid.";
 }
 public static string Run()
 {
  if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first");
  Directory.CreateDirectory(Folder);
  var source=new Texture2D(2,2,TextureFormat.RGBA32,false);
  source.LoadImage(File.ReadAllBytes("Art/WeaponArtReview-20261020/redesign-v1.png"));
  int[] tops={53,229,403,559,733,907};
  for(int i=0;i<Names.Length;i++)
  {
   int cell=Cells[i],col=cell%8,row=cell/8;
   int x=9+col*175,y=tops[row]+6,w=156,h=(row==2?108:122);
   var pixels=source.GetPixels(x,source.height-y-h,w,h);
   var outside=new bool[pixels.Length]; var queue=new Queue<int>();
   void Visit(int p){if(p<0||p>=pixels.Length||outside[p])return;var c=pixels[p];if(Mathf.Max(c.r,c.g,c.b)>.22f)return;outside[p]=true;queue.Enqueue(p);}
   for(int a=0;a<w;a++){Visit(a);Visit((h-1)*w+a);}for(int b=0;b<h;b++){Visit(b*w);Visit(b*w+w-1);}
   while(queue.Count>0){int p=queue.Dequeue();if(p%w>0)Visit(p-1);if(p%w<w-1)Visit(p+1);Visit(p-w);Visit(p+w);}
   int minX=w,minY=h,maxX=0,maxY=0;
   for(int p=0;p<pixels.Length;p++){if(outside[p]){pixels[p]=Color.clear;continue;}int a=p%w,b=p/w;minX=Math.Min(minX,a);maxX=Math.Max(maxX,a);minY=Math.Min(minY,b);maxY=Math.Max(maxY,b);}
   int size=i<16?32:24; var output=new Texture2D(size,size,TextureFormat.RGBA32,false);var result=new Color[size*size];
   float scale=(size-2f)/Mathf.Max(maxX-minX+1,maxY-minY+1);
   int dw=Mathf.Max(1,Mathf.RoundToInt((maxX-minX+1)*scale)),dh=Mathf.Max(1,Mathf.RoundToInt((maxY-minY+1)*scale));
   for(int b=0;b<dh;b++)for(int a=0;a<dw;a++){int sx=minX+Mathf.Min(maxX-minX,(int)((a+.5f)/scale));int sy=minY+Mathf.Min(maxY-minY,(int)((b+.5f)/scale));result[(b+(size-dh)/2)*size+a+(size-dw)/2]=pixels[sy*w+sx];}
   output.SetPixels(result);output.Apply();string path=Folder+"/Authored_"+Names[i]+".png";File.WriteAllBytes(path,output.EncodeToPNG());UnityEngine.Object.DestroyImmediate(output);
   AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
   var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.spritePixelsPerUnit=32;imp.filterMode=FilterMode.Point;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.mipmapEnabled=false;imp.alphaIsTransparency=true;imp.SaveAndReimport();
  }
  UnityEngine.Object.DestroyImmediate(source);
  Sprite Icon(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"/Authored_"+name+".png");
  var balance=AssetDatabase.LoadAssetAtPath<PortalTowerBalance>("Assets/Resources/World/PortalTowerBalance.asset");
  var replacements=new Dictionary<Sprite,Sprite>();
  foreach(var u in balance.upgrades){if(!Names.Contains(u.id))continue;if(u.icon!=null)replacements[u.icon]=Icon(u.id);u.icon=Icon(u.id);}
  foreach(var weapon in balance.weapons)weapon.icon=Icon("weapon_"+weapon.weapon);
  EditorUtility.SetDirty(balance);AssetDatabase.SaveAssetIfDirty(balance);
  var table=AssetDatabase.LoadAssetAtPath<ScientificUpgradeTable>("Assets/Resources/ScientificUpgradeTable.asset");
  var labs=new Dictionary<string,string>{{"Sword","Gathering"},{"Eye","Quiet"},{"Shield","Shield"},{"Magic","CrystalResearch"},{"Hourglass","ScoutSpeed"},{"Healing","Healing"}};
  foreach(var entry in table.entries){if(entry.icon==null)continue;if(replacements.TryGetValue(entry.icon,out var replacement))entry.icon=replacement;else if(labs.TryGetValue(entry.icon.name,out var name))entry.icon=Icon(name);}
  EditorUtility.SetDirty(table);AssetDatabase.SaveAssetIfDirty(table);
  // Resource art keeps its established style; generated equipment proposals are not installed.
  int images=0;
  foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs"}))
  {
   string path=AssetDatabase.GUIDToAssetPath(guid);var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
   if(!prefab.GetComponentsInChildren<Image>(true).Any(im=>im.sprite!=null&&replacements.ContainsKey(im.sprite)))continue;
   var root=PrefabUtility.LoadPrefabContents(path);
   try{foreach(var image in root.GetComponentsInChildren<Image>(true))if(image.sprite!=null&&replacements.TryGetValue(image.sprite,out var replacement)){image.sprite=replacement;image.material=DamagePalette.IconMaterial(replacement);images++;}PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
  }
  return "Updated weapon and laboratory icons and "+images+" prefab Images; resource icons preserved.";
 }
}
