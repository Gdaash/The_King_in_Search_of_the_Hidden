#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using GameFoundation.Combat;
using GameFoundation.MetaProgression;
using GameFoundation.UI;
using GameFoundation.Localization;
using UnityEngine;
using UnityEditor;
using Object=UnityEngine.Object;

public static class CombatBalanceSetup
{
    public const string Folder="Assets/Resources/Combat";
    static T Asset<T>(string path) where T:ScriptableObject
    {
        var asset=AssetDatabase.LoadAssetAtPath<T>(path);
        if(asset==null){asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,path);}
        return asset;
    }
    static CombatDefenseProfile Defense(string name,float physical=1,float magic=1,float electric=1)
    {
        var p=Asset<CombatDefenseProfile>(Folder+"/Defense/"+name+".asset");
        p.physicalMultiplier=physical;p.magicalMultiplier=magic;p.electricMultiplier=electric;
        p.fireMultiplier=p.iceMultiplier=1;EditorUtility.SetDirty(p);return p;
    }
    public static CombatWeapon Weapon(string name,string ru,string en,DamageType type,float damage,float interval,float range,float radius=.1f)
    {
        var w=Asset<CombatWeapon>(Folder+"/Weapons/"+name+".asset");
        w.title=ru;w.englishTitle=en;w.damageType=type;w.damage=damage;w.interval=interval;w.range=range;w.radius=radius;
        w.count=1;w.expansionSpeed=3;EditorUtility.SetDirty(w);return w;
    }
    [MenuItem("Tools/Game Foundation/Combat/Install Combat Balance")]
    public static void Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode first");
        System.IO.Directory.CreateDirectory(Folder+"/Weapons");System.IO.Directory.CreateDirectory(Folder+"/Defense");AssetDatabase.Refresh();
        var normal=Defense("Unarmored");var armored=Defense("Armored",electric:1.5f);var mage=Defense("Mage",1.25f,.5f);
        var weapons=new Dictionary<string,CombatWeapon>();
        weapons["RedArcher"]=Weapon("Archer Bow","Лук лучника","Archer bow",DamageType.Physical,8,.4f,8);
        weapons["RedSwordsman"]=Weapon("Swordsman Sword","Меч воина","Warrior sword",DamageType.Physical,40,2,.5f);
        weapons["RedSniper"]=Weapon("Sniper Bow","Лук снайпера","Sniper bow",DamageType.Physical,60,3,10);
        weapons["Magic Tower"]=Weapon("Magic Tower","Магическая башня","Magic tower",DamageType.Magic,40,2,8);
        weapons["Arrow Tower"]=Weapon("Arrow Tower","Стрелковая башня","Arrow tower",DamageType.Physical,4,.2f,9);
        weapons["Stone Tower"]=Weapon("Stone Tower","Камнеметательная башня","Stone tower",DamageType.Physical,80,4,4);
        var portalWeapons=new Dictionary<PortalTowerBalance.Weapon,CombatWeapon>{
            [PortalTowerBalance.Weapon.Bolts]=Weapon("Portal Bolts","Магические снаряды","Magic bolts",DamageType.Magic,40,2,8),
            [PortalTowerBalance.Weapon.Beam]=Weapon("Portal Beam","Выжигающий луч","Burning beam",DamageType.Fire,5,.25f,6,.75f),
            [PortalTowerBalance.Weapon.Rings]=Weapon("Portal Rings","Кольца силы","Force rings",DamageType.Magic,30,1.5f,6,.22f),
            [PortalTowerBalance.Weapon.Lightning]=Weapon("Portal Lightning","Случайная молния","Random lightning",DamageType.Electric,40,2,8)
        };
        var balance=AssetDatabase.LoadAssetAtPath<PortalTowerBalance>("Assets/Resources/World/PortalTowerBalance.asset");
        foreach(var d in balance.weapons)d.combat=portalWeapons[d.weapon];
        void Describe(PortalTowerBalance.Weapon kind,string ru,string en)
        {
            var d=balance.FindWeapon(kind);d.description=ru;d.englishDescription=en;
            var u=balance.Find("weapon_"+kind);if(u!=null){u.description=ru;u.englishDescription=en;}
        }
        Describe(PortalTowerBalance.Weapon.Bolts,"Сильный магический снаряд: 40 урона раз в 2 секунды. Эффективен против брони.","Heavy magic bolt: 40 damage every 2 seconds. Effective against armor.");
        Describe(PortalTowerBalance.Weapon.Beam,"Автоматически выжигает ближайшую цель: 5 урона огнём каждые 0,25 секунды. Пятно поражает всех врагов внутри.","Automatically burns the nearest target: 5 fire damage every 0.25 seconds. The spot hits all enemies inside.");
        Describe(PortalTowerBalance.Weapon.Rings,"Кольцо каждые 1,5 секунды: 30 магического урона каждому пересечённому врагу.","A ring every 1.5 seconds: 30 magic damage to every enemy it crosses.");
        Describe(PortalTowerBalance.Weapon.Lightning,"40 электрического урона раз в 2 секунды случайной цели. Игнорирует броню; бронированные враги получают на 50% больше.","40 electric damage every 2 seconds to a random target. Ignores armor; armored enemies take 50% more.");
        EditorUtility.SetDirty(balance);
        int configured=0;
        var paths=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs/Units"}).Select(AssetDatabase.GUIDToAssetPath).ToList();
        paths.AddRange(new[]{"Assets/Prefabs/Buildings/PortalTower.prefab","Assets/Prefabs/Buildings/Defense Towers/Magic Tower.prefab","Assets/Prefabs/Buildings/Defense Towers/Arrow Tower.prefab","Assets/Prefabs/Buildings/Defense Towers/Stone Tower.prefab"});
        foreach(var path in paths)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);var health=asset.GetComponent<Health>();if(health==null || health.Stats==null)continue;
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var stats=root.GetComponent<Health>().Stats;
                var c=root.GetComponent<Combatant>();bool added=c==null;if(added)c=root.AddComponent<Combatant>();
                if(added){c.baseHealth=stats.baseMaxHealth;c.baseSpeed=stats.baseSpeed;}
                c.modifiers=stats;c.defense=normal;c.baseArmor=0;
                switch(root.name)
                {
                    case "Octopus":c.baseHealth=20;c.baseSpeed=2.2f;break;
                    case "OctopusRaider":c.baseHealth=35;c.baseSpeed=3;break;
                    case "OctopusWarrior":c.baseHealth=300;c.baseSpeed=.75f;break;
                    case "OctopusGuardian":c.baseHealth=80;c.baseSpeed=1;c.baseArmor=3;c.defense=armored;break;
                    case "CursedKnight":c.baseHealth=100;c.baseSpeed=1.2f;c.baseArmor=3;c.defense=armored;break;
                    case "CursedMage":c.baseHealth=60;c.baseSpeed=1.5f;c.defense=mage;break;
                }
                if(root.name=="PortalTower")c.weapon=portalWeapons[PortalTowerBalance.Weapon.Bolts];
                else if(weapons.TryGetValue(root.name,out var weapon))c.weapon=weapon;
                else if(root.GetComponent<EnemyAI>()!=null || root.GetComponent<EnemyAI_Ranged>()!=null)
                {
                    var melee=root.GetComponent<EnemyAI>();
                    var type=root.name=="CursedMage"?DamageType.Magic:stats.damageSettings.FirstOrDefault()?.type??DamageType.Physical;
                    float damage=stats.damageSettings.Sum(d=>d.baseDamage);
                    float range=melee!=null?new SerializedObject(melee).FindProperty("attackRange").floatValue:stats.baseAttackRange;
                    float interval=melee!=null?new SerializedObject(melee).FindProperty("baseAttackCooldown").floatValue:stats.baseAttackCooldown;
                    c.weapon=Weapon(root.name+" Attack",root.name+" — атака",root.name+" attack",type,damage,interval,range);
                }
                stats.usesPrefabCombatBases=true;
                foreach(DamageType type in Enum.GetValues(typeof(DamageType)))
                {
                    if(!stats.damageSettings.Exists(d=>d.type==type))stats.damageSettings.Add(new(){type=type});
                    if(!stats.resistances.Exists(r=>r.type==type))stats.resistances.Add(new(){type=type,baseMult=1});
                }
                EditorUtility.SetDirty(stats);
                foreach(var ai in root.GetComponents<EnemyAI>()){var s=new SerializedObject(ai);s.FindProperty("cooldownVariation").floatValue=0;s.ApplyModifiedPropertiesWithoutUndo();}
                foreach(var ai in root.GetComponents<EnemyAI_Ranged>()){var s=new SerializedObject(ai);s.FindProperty("cooldownVariation").floatValue=0;s.ApplyModifiedPropertiesWithoutUndo();}
                foreach(var ai in root.GetComponents<ArcherTower>()){var s=new SerializedObject(ai);s.FindProperty("cooldownVariation").floatValue=0;s.ApplyModifiedPropertiesWithoutUndo();}
                var movement=root.GetComponent<EnemyMovement>();if(movement!=null){var s=new SerializedObject(movement);s.FindProperty("speedVariation").floatValue=c.baseSpeed*.1f;s.ApplyModifiedPropertiesWithoutUndo();}
                PrefabUtility.SaveAsPrefabAsset(root,path);configured++;
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        ConfigureCards();Localize();AssetDatabase.SaveAssets();
        Debug.Log("Combat balance installed: "+configured+" prefabs; 20 base DPS for player weapons.");
    }
    static Sprite Icon(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ui/UnitStats/"+name+".png");
    static void AddStats(UnitDescriptionView view)
    {
        var so=new SerializedObject(view);var list=so.FindProperty("stats");
        void Add(UnitStat stat,string key,string label,Sprite icon,bool hide)
        {
            if(Enumerable.Range(0,list.arraySize).Any(i=>list.GetArrayElementAtIndex(i).FindPropertyRelative("stat").enumValueIndex==(int)stat))return;
            int index=list.arraySize++;var p=list.GetArrayElementAtIndex(index);
            p.FindPropertyRelative("stat").enumValueIndex=(int)stat;p.FindPropertyRelative("labelKey").stringValue=key;
            p.FindPropertyRelative("fallbackLabel").stringValue=label;p.FindPropertyRelative("icon").objectReferenceValue=icon;
            p.FindPropertyRelative("hideWhenZero").boolValue=hide;
        }
        Add(UnitStat.Armor,"unit.details.armor","Броня за попадание",Icon("Shield"),false);
        Add(UnitStat.Dps,"unit.details.dps","Урон в секунду",Icon("Sword"),false);
        Add(UnitStat.ElectricDamage,"unit.details.electric_damage","Электрический урон",Icon("Magic"),true);
        Add(UnitStat.ElectricResistance,"unit.details.electric_resistance","Защита от электричества",Icon("Magic"),true);
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    static void ConfigureCards()
    {
        foreach(var id in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs/UI","Assets/Prefabs/Base"}))
        {
            var path=AssetDatabase.GUIDToAssetPath(id);var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(asset.GetComponentInChildren<UnitDescriptionView>(true)==null)continue;
            var root=PrefabUtility.LoadPrefabContents(path);
            try{foreach(var view in root.GetComponentsInChildren<UnitDescriptionView>(true))AddStats(view);PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        foreach(var view in Object.FindObjectsByType<UnitDescriptionView>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {AddStats(view);PrefabUtility.RecordPrefabInstancePropertyModifications(view);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(view.gameObject.scene);}
    }
    static void Localize()
    {
        var table=AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
        void Text(string key,string ru,string en)
        {
            var e=table.entries.Find(x=>x.key==key);if(e==null){e=new(){key=key};table.entries.Add(e);}
            while(e.values.Count<table.languages.Count)e.values.Add("");e.values[table.languages.IndexOf("ru")]=ru;e.values[table.languages.IndexOf("en")]=en;
        }
        Text("unit.details.armor","Броня за попадание","Armor per hit");Text("unit.details.dps","Урон в секунду","Damage per second");
        Text("unit.details.electric_damage","Электрический урон","Electric damage");Text("unit.details.electric_resistance","Защита от электричества","Electric resistance");
        Text("unit.enemy.octopus.description","Быстрый враг с небольшим запасом здоровья. Частые атаки эффективно останавливают таких врагов.","A fast enemy with little health. Rapid attacks stop these enemies efficiently.");
        Text("unit.enemy.octopusraider.description","Очень быстрый враг с небольшим запасом здоровья и без брони.","A very fast enemy with little health and no armor.");
        Text("unit.enemy.octopusguardian.description","Броня вычитает 3 урона из каждого попадания на первом уровне. Электричество игнорирует броню и наносит на 50% больше урона.","Armor subtracts 3 damage from each hit at level one. Electricity ignores armor and deals 50% more damage.");
        Text("unit.enemy.octopuswarrior.description","Медленный гигант с большим запасом здоровья, без брони. Уязвим к непрерывному урону.","A slow giant with a large health pool and no armor. Vulnerable to sustained damage.");
        Text("unit.enemy.cursedknight.description","Броня вычитает урон из каждого попадания. Сильные удары эффективны; электричество игнорирует броню и наносит на 50% больше урона.","Armor subtracts damage from every hit. Heavy hits are effective; electricity ignores armor and deals 50% more damage.");
        Text("unit.enemy.cursedmage.description","Сопротивляется магии, огню, льду и электричеству на 50%. Получает на 25% больше физического урона.","Resists magic, fire, ice and electricity by 50%. Takes 25% more physical damage.");
        EditorUtility.SetDirty(table);
    }
}
#endif
