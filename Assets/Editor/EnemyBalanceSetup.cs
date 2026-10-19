#if UNITY_EDITOR
using System.Linq;
using GameFoundation.Bestiary;
using GameFoundation.Combat;
using GameFoundation.Localization;
using GameFoundation.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using Object=UnityEngine.Object;

/// <summary>Authors enemy roles and their explicit danger order without changing wave counts.</summary>
public static class EnemyBalanceSetup
{
    public static readonly string[] Order={"Octopus","Bat","SkeletonSlime","OctopusRaider","FlyingEye","Skeleton","CursedMage","OctopusGuardian","CursedKnight","OctopusWarrior","Demon","ToxicDemon"};
    static readonly float[] Health={20,16,140,55,42,100,65,180,240,800,650,720};
    static readonly float[] Speed={2,4.5f,.32f,3.5f,.9f,1.7f,1.4f,2.6f,1.5f,.65f,1.8f,2.3f};
    static readonly float[] Damage={4,4,8,9,10,14,30,26,55,130,70,90};
    static readonly float[] Interval={1.5f,1.1f,2.5f,1.25f,1.5f,1.6f,2,1.8f,3,6,1.5f,1.4f};
    static readonly float[] Armor={0,0,0,0,0,1,0,4,7,5,3,4};
    static readonly float[] Range={.4f,1.75f,.45f,.4f,5.5f,.45f,5,.6f,.6f,.7f,.65f,.65f};
    static readonly int[] Wave={0,1,1,2,2,2,3,3,3,4,4,4};
    static readonly string[] Ru={
        "Самый слабый наземный враг со средней скоростью. Атакует в ближнем бою.",
        "Самый быстрый летающий враг с небольшим запасом здоровья. Стреляет с короткой дистанции.",
        "Крайне медленный, но живучий скелет, поглощённый слизью. Атакует в ближнем бою.",
        "Быстрый наземный налётчик. Уступает в скорости только летучей мыши.",
        "Медленный летающий враг. Обстреливает цель с большой дистанции.",
        "Наземный воин со средними здоровьем, скоростью и силой атаки.",
        "Хрупкий дальнобойный маг. Компенсирует малое здоровье высоким уроном.",
        "Бронированный страж со скоростью выше средней и сильными атаками.",
        "Тяжело бронированный рыцарь. Медленнее стража, но наносит редкие мощные удары.",
        "Самый живучий враг. Медленно движется и наносит очень редкие сокрушительные удары.",
        "Здоровья меньше, чем у воина-осьминога, но сильные частые атаки делают демона опаснее.",
        "Самый опасный враг. Быстрее обычного демона и наносит ещё больше урона."
    };
    static readonly string[] En={
        "The weakest ground enemy, with moderate movement speed. Attacks in melee.",
        "The fastest flying enemy, with little health. Fires at short range.",
        "An extremely slow but resilient skeleton engulfed in slime. Attacks in melee.",
        "A fast ground raider. Only the bat moves faster.",
        "A slow flying enemy that fires at targets from long range.",
        "A ground warrior with moderate health, movement speed and attack damage.",
        "A fragile ranged mage that compensates for low health with high damage.",
        "An armored guardian with above-average speed and strong attacks.",
        "A heavily armored knight. Slower than the guardian, with rare powerful strikes.",
        "The enemy with the most health. Moves slowly and delivers very rare devastating strikes.",
        "Has less health than the octopus warrior, but strong frequent attacks make it more dangerous.",
        "The most dangerous enemy. Faster than the ordinary demon and deals even more damage."
    };

    public static void Run()
    {
        var table=AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
        for(int i=0;i<Order.Length;i++)
        {
            string name=Order[i],path="Assets/Prefabs/Units/"+name+".prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var combat=root.GetComponent<Combatant>();var stats=combat.modifiers;
                combat.baseHealth=Health[i];combat.baseSpeed=Speed[i];combat.baseArmor=Armor[i];
                combat.weapon.damage=Damage[i];combat.weapon.interval=Interval[i];combat.weapon.range=Range[i];
                if(name=="FlyingEye")combat.weapon.damageType=DamageType.Magic;
                stats.baseMaxHealth=Health[i];stats.baseSpeed=Speed[i];stats.baseAttackCooldown=Interval[i];stats.baseAttackRange=Range[i];
                foreach(var d in stats.damageSettings)d.baseDamage=d.type==combat.weapon.damageType?Damage[i]:0;
                if(!stats.damageSettings.Any(d=>d.type==combat.weapon.damageType))stats.damageSettings.Add(new GlobalStats.DamageInfo{type=combat.weapon.damageType,baseDamage=Damage[i]});
                EditorUtility.SetDirty(stats);EditorUtility.SetDirty(combat.weapon);
                var identity=new SerializedObject(root.GetComponent<EnemyIdentity>());identity.FindProperty("dangerRank").intValue=i+1;identity.ApplyModifiedPropertiesWithoutUndo();
                if(name=="FlyingEye"||name=="Bat")ConfigureRanged(root,stats,Range[i]);
                var movement=new SerializedObject(root.GetComponent<EnemyMovement>());movement.FindProperty("speedVariation").floatValue=Speed[i]*.08f;movement.ApplyModifiedPropertiesWithoutUndo();
                if(root.TryGetComponent<EnemyAI>(out var melee)){var so=new SerializedObject(melee);so.FindProperty("cooldownVariation").floatValue=0;so.ApplyModifiedPropertiesWithoutUndo();}
                if(root.TryGetComponent<EnemyAI_Ranged>(out var ranged)){var so=new SerializedObject(ranged);so.FindProperty("cooldownVariation").floatValue=0;so.ApplyModifiedPropertiesWithoutUndo();}
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            string key="unit.enemy."+name.ToLowerInvariant()+".description";
            var entry=table.entries.FirstOrDefault(e=>e.key==key);
            if(entry==null){entry=new LocalizationTable.Entry{key=key};table.entries.Add(entry);}
            while(entry.values.Count<table.languages.Count)entry.values.Add("");
            entry.values[table.languages.IndexOf("ru")]=Ru[i];entry.values[table.languages.IndexOf("en")]=En[i];
            var description=AssetDatabase.LoadAssetAtPath<UnitDescriptionDefinition>("Assets/Resources/UI/Unit Details/Enemy "+name+" Description.asset");description.fallbackDescription=Ru[i];EditorUtility.SetDirty(description);
        }
        EditorUtility.SetDirty(table);
        var catalog=AssetDatabase.LoadAssetAtPath<UnitDescriptionCatalog>(EnemyRosterSetup.CatalogPath);
        catalog.units=catalog.units.OrderBy(d=>d.unitPrefab.GetComponent<EnemyIdentity>().DangerRank).ToArray();EditorUtility.SetDirty(catalog);
        var waves=AssetDatabase.LoadAssetAtPath<AlarmDifficultyTable>("Assets/Resources/World/AlarmDifficultyTable.asset");
        foreach(var difficulty in waves.difficulties)for(int wave=0;wave<difficulty.thresholds.Count;wave++)
        {
            var entries=difficulty.thresholds[wave].enemies;
            // Retain authored weights for eligible types, add missing types and remove premature ones.
            for(int i=0;i<Order.Length;i++)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Units/"+Order[i]+".prefab");
                if(Wave[i]>wave)entries.RemoveAll(e=>e.prefab==prefab);
                else if(!entries.Any(e=>e.prefab==prefab))entries.Add(new AlarmEnemy{prefab=prefab,weight=i>=9?1:2});
            }
            entries.Sort((a,b)=>a.prefab.GetComponent<EnemyIdentity>().DangerRank.CompareTo(b.prefab.GetComponent<EnemyIdentity>().DangerRank));
        }
        EditorUtility.SetDirty(waves);AssetDatabase.SaveAssets();EnemyResistanceSetup.Run();Debug.Log("Enemy danger order and roles updated: 12 types.");
    }

    static void ConfigureRanged(GameObject root,GlobalStats stats,float range)
    {
        var template=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Units/CursedMage.prefab");
        if(root.TryGetComponent<EnemyAI>(out var melee))Object.DestroyImmediate(melee);
        if(root.TryGetComponent<EnemyVisuals>(out var meleeVisual))Object.DestroyImmediate(meleeVisual);
        var ai=root.GetComponent<EnemyAI_Ranged>();if(ai==null){ai=root.AddComponent<EnemyAI_Ranged>();EditorUtility.CopySerialized(template.GetComponent<EnemyAI_Ranged>(),ai);}
        var visual=root.GetComponent<EnemyVisuals_Ranged>();if(visual==null){visual=root.AddComponent<EnemyVisuals_Ranged>();EditorUtility.CopySerialized(template.GetComponent<EnemyVisuals_Ranged>(),visual);}
        var graphics=root.GetComponentInChildren<SpriteRenderer>(true).transform;
        var shoot=graphics.Find("Shoot Point");if(shoot==null){shoot=new GameObject("Shoot Point").transform;shoot.SetParent(graphics,false);}
        var a=new SerializedObject(ai);a.FindProperty("stats").objectReferenceValue=stats;a.FindProperty("detectionRange").floatValue=50;a.FindProperty("defaultAttackRange").floatValue=range;a.FindProperty("defaultStopRange").floatValue=range*.75f;a.ApplyModifiedPropertiesWithoutUndo();
        var v=new SerializedObject(visual);v.FindProperty("spriteParent").objectReferenceValue=graphics;v.FindProperty("shootPoint").objectReferenceValue=shoot;v.FindProperty("ai").objectReferenceValue=ai;v.FindProperty("bounceHeight").floatValue=.05f;v.ApplyModifiedPropertiesWithoutUndo();
        ai.OnMove=new UnityEvent();ai.OnStop=new UnityEvent();ai.OnAttack=new UnityEvent();
        UnityEventTools.AddBoolPersistentListener(ai.OnMove,root.GetComponent<EnemyMovement>().SetMove,true);
        UnityEventTools.AddBoolPersistentListener(ai.OnMove,visual.SetMoving,true);
        UnityEventTools.AddBoolPersistentListener(ai.OnStop,root.GetComponent<EnemyMovement>().SetMove,false);
        UnityEventTools.AddBoolPersistentListener(ai.OnStop,visual.SetMoving,false);
        UnityEventTools.AddPersistentListener(ai.OnAttack,visual.StartShoot);
    }
}
#endif
