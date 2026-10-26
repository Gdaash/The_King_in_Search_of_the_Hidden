using UnityEngine;

/// <summary>One color language for weapon art, lights, particles and impacts.</summary>
public static class DamagePalette
{
 public static Color Fire => new Color(1,.25f,.06f);
 public static Color Electric => new Color(1,.94f,.58f);
 public static Color Ice => new Color(.65f,.92f,1);
 public static Color Magic => new Color(.7f,.28f,1);
 private static readonly Material[] materials=new Material[5];
 private static readonly Material[] uiMaterials=new Material[5];
 public static Color For(DamageType type)=>type switch {DamageType.Fire=>Fire,DamageType.Electric=>Electric,DamageType.Ice=>Ice,DamageType.Magic=>Magic,_=>new Color(.7f,.67f,.61f)};
 public static Material IconMaterial(Sprite sprite)
 {
  if(sprite==null || sprite.name.StartsWith("Authored_",System.StringComparison.Ordinal))return null;
  DamageType type=sprite.name switch {
   "weapon_Bolts" or "weapon_Rings" or "Magic Bolts" or "Magic" or "MagicPlus"=>DamageType.Magic,
   "weapon_Beam" or "weapon_FireShot" or "projectile_FirePellet" or "Burning Beam" or "Fire"=>DamageType.Fire,
   "weapon_Lightning" or "weapon_ElectricOrb" or "Lightning" or "Electric"=>DamageType.Electric,
   "weapon_FrostSeal" or "Frost Seal" or "Ice" or "Cold"=>DamageType.Ice,
   _=>DamageType.Physical};
  return MaterialFor(type,true);
 }
 public static Material MaterialFor(DamageType type, bool ui=false)
 {
  if(type==DamageType.Physical)return null;
  int index=(int)type;
  var cache=ui?uiMaterials:materials;
  if(cache[index]==null)cache[index]=Resources.Load<Material>("Effects/Damage Palettes/"+type+(ui?" UI":""));
  return cache[index];
 }
}
