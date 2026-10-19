#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using GameFoundation.Bestiary;
using GameFoundation.Combat;
using GameFoundation.Localization;
using GameFoundation.UI;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Imports the six supplied monsters without replacing existing enemies or wave sizes.</summary>
public static class AdditionalEnemySetup
{
    const string Sheet = "Assets/Sprites/Units/Enemies/PortalMonsters.png";
    sealed class Entry
    {
        public string id, ru, en, description, englishDescription;
        public Rect rect;
        public bool flying;
        public float health, speed, damage, interval;
        public int wave;
        public Entry(string id, string ru, string en, Rect rect, bool flying, float health, float speed, float damage, float interval, int wave, string description, string englishDescription)
        { this.id=id; this.ru=ru; this.en=en; this.rect=rect; this.flying=flying; this.health=health; this.speed=speed; this.damage=damage; this.interval=interval; this.wave=wave; this.description=description; this.englishDescription=englishDescription; }
    }
    // Rectangles use the original 205x51 texture, bottom-left origin, with one transparent pixel of padding.
    static readonly Entry[] Entries = {
        new("SkeletonSlime", "Скелет-слизь", "Skeleton slime", new Rect(11,15,20,21), false, 28,1.8f,6,1.6f,0,"Скелет, поглощённый слизью. Наземный враг ближнего боя.","A skeleton engulfed in slime. A ground melee enemy."),
        new("FlyingEye", "Летающий глаз", "Flying eye", new Rect(38,16,20,18), true,24,2.5f,5,1.3f,1,"Быстрый летающий враг. Подлетает к цели и атакует вблизи.","A fast flying enemy that approaches its target to attack at close range."),
        new("Skeleton", "Скелет", "Skeleton", new Rect(66,14,17,23), false,50,1.7f,10,1.6f,2,"Наземный воин ближнего боя со средним запасом здоровья.","A ground melee warrior with a moderate amount of health."),
        new("Demon", "Демон", "Demon", new Rect(94,10,29,36), false,160,1.3f,25,1.8f,3,"Крупный наземный демон с мощной атакой ближнего боя.","A large ground demon with a powerful melee attack."),
        new("ToxicDemon", "Токсичный демон", "Toxic demon", new Rect(132,10,32,39), false,200,1.1f,30,2,4,"Живучий наземный демон. Атакует в ближнем бою.","A resilient ground demon that attacks at close range."),
        new("Bat", "Летучая мышь", "Bat", new Rect(174,15,23,20), true,16,3,4,1.1f,0,"Небольшой летающий враг с высокой скоростью движения.","A small flying enemy with high movement speed.")
    };

    public static void Run()
    {
        Slice();
        var sprites=AssetDatabase.LoadAllAssetsAtPath(Sheet).OfType<Sprite>().ToDictionary(s=>s.name);
        var catalog=AssetDatabase.LoadAssetAtPath<UnitDescriptionCatalog>(EnemyRosterSetup.CatalogPath);
        var descriptions=catalog.units.ToList();
        var table=AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
        var waves=AssetDatabase.LoadAssetAtPath<AlarmDifficultyTable>("Assets/Resources/World/AlarmDifficultyTable.asset");
        foreach(var entry in Entries)
        {
            var prefab=CreateEnemy(entry,sprites[entry.id]);
            string path="Assets/Resources/UI/Unit Details/Enemy "+entry.id+" Description.asset";
            var data=AssetDatabase.LoadAssetAtPath<UnitDescriptionDefinition>(path);
            if(data==null){data=ScriptableObject.CreateInstance<UnitDescriptionDefinition>();AssetDatabase.CreateAsset(data,path);}
            data.unitPrefab=prefab;data.isEnemy=true;data.portraitIcon=sprites[entry.id];data.fallbackTitle=entry.ru;
            string key="unit.enemy."+entry.id.ToLowerInvariant();
            data.titleKey=key+".title";data.descriptionKey=key+".description";data.fallbackDescription=entry.description;
            data.roleKey=entry.flying?"unit.enemy.role.flying":"unit.enemy.role.ground";data.fallbackRole=entry.flying?"ЛЕТАЮЩИЙ ВРАГ":"НАЗЕМНЫЙ ВРАГ";
            EditorUtility.SetDirty(data);if(!descriptions.Contains(data))descriptions.Add(data);
            Localize(table,key+".title",entry.ru,entry.en);Localize(table,key+".description",entry.description,entry.englishDescription);
            foreach(var difficulty in waves.difficulties)
                for(int i=entry.wave;i<difficulty.thresholds.Count;i++)
                {
                    var enemies=difficulty.thresholds[i].enemies;
                    if(!enemies.Any(e=>e.prefab==prefab))enemies.Add(new AlarmEnemy{prefab=prefab,weight=entry.id=="ToxicDemon"?1:2});
                }
        }
        catalog.units=descriptions.ToArray();EditorUtility.SetDirty(catalog);EditorUtility.SetDirty(waves);
        Localize(table,"unit.enemy.role.ground","НАЗЕМНЫЙ ВРАГ","GROUND ENEMY");
        Localize(table,"unit.enemy.role.flying","ЛЕТАЮЩИЙ ВРАГ","FLYING ENEMY");
        EditorUtility.SetDirty(table);AssetDatabase.SaveAssets();
        EnemyBalanceSetup.Run();
        Debug.Log("Added six enemies, movement types, localized bestiary entries and staged wave availability.");
    }

    static GameObject CreateEnemy(Entry entry,Sprite sprite)
    {
        string statsPath="Assets/Resources/Units/"+entry.id+".asset";
        var stats=AssetDatabase.LoadAssetAtPath<GlobalStats>(statsPath);
        if(stats==null){stats=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GlobalStats>("Assets/Resources/Units/Octopus.asset"));stats.name=entry.id;AssetDatabase.CreateAsset(stats,statsPath);}
        stats.unitTypeKey=entry.id;stats.baseMaxHealth=entry.health;stats.baseSpeed=entry.speed;
        stats.baseAttackCooldown=entry.interval;stats.baseAttackRange=.45f;
        stats.damageSettings=new List<GlobalStats.DamageInfo>{new(){type=DamageType.Physical,baseDamage=entry.damage}};
        EditorUtility.SetDirty(stats);
        string weaponPath="Assets/Resources/Combat/Weapons/"+entry.id+" Attack.asset";
        var weapon=AssetDatabase.LoadAssetAtPath<CombatWeapon>(weaponPath);
        if(weapon==null){weapon=Object.Instantiate(AssetDatabase.LoadAssetAtPath<CombatWeapon>("Assets/Resources/Combat/Weapons/Octopus Attack.asset"));weapon.name=entry.id+" Attack";AssetDatabase.CreateAsset(weapon,weaponPath);}
        weapon.title=entry.ru;weapon.englishTitle=entry.en;weapon.damage=entry.damage;weapon.interval=entry.interval;weapon.range=.45f;weapon.damageType=DamageType.Physical;
        EditorUtility.SetDirty(weapon);
        string prefabPath="Assets/Prefabs/Units/"+entry.id+".prefab";
        var root=PrefabUtility.LoadPrefabContents(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)!=null?prefabPath:"Assets/Prefabs/Units/Octopus.prefab");
        try
        {
            root.name=entry.id;root.transform.localPosition=Vector3.zero;root.transform.localScale=Vector3.one;
            foreach(var component in root.GetComponentsInChildren<Component>(true))
            {
                if(component==null)throw new InvalidOperationException("Missing script in "+entry.id);
                var so=new SerializedObject(component);var field=so.FindProperty("stats");
                if(field!=null&&field.propertyType==SerializedPropertyType.ObjectReference){field.objectReferenceValue=stats;so.ApplyModifiedPropertiesWithoutUndo();}
            }
            var combat=root.GetComponent<Combatant>();combat.modifiers=stats;combat.baseHealth=entry.health;combat.baseSpeed=entry.speed;combat.weapon=weapon;
            var identity=root.GetComponent<EnemyIdentity>();var id=new SerializedObject(identity);
            id.FindProperty("persistentId").stringValue=entry.id;id.FindProperty("movementType").enumValueIndex=(int)(entry.flying?EnemyMovementType.Flying:EnemyMovementType.Ground);id.ApplyModifiedPropertiesWithoutUndo();
            var renderer=root.GetComponentInChildren<SpriteRenderer>(true);renderer.sprite=sprite;renderer.transform.localScale=Vector3.one;
            renderer.transform.localPosition=new Vector3(0,entry.flying?.16f:0,0);
            var collider=root.GetComponent<BoxCollider2D>();
            if(collider!=null)collider.size=new Vector2(Mathf.Max(.18f,sprite.rect.width/32*.65f),Mathf.Max(.3f,sprite.rect.height/32*.75f));
            var bar=root.GetComponentInChildren<HealthBar>(true);
            if(bar!=null){var pos=bar.transform.localPosition;pos.y=renderer.transform.localPosition.y+sprite.rect.height/64+.13f;bar.transform.localPosition=pos;}
            if(entry.flying && root.GetComponent<EnemyVisuals>()!=null){var visual=new SerializedObject(root.GetComponent<EnemyVisuals>());visual.FindProperty("bounceHeight").floatValue=.05f;visual.ApplyModifiedPropertiesWithoutUndo();}
            return PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }

    static void Slice()
    {
        AssetDatabase.ImportAsset(Sheet);
        var importer=(TextureImporter)AssetImporter.GetAtPath(Sheet);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit=32;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.alphaIsTransparency=true;importer.SaveAndReimport();
        var factory=new SpriteDataProviderFactories();factory.Init();var provider=factory.GetSpriteEditorDataProviderFromObject(importer);
        if(provider==null)throw new InvalidOperationException("Sprite data provider unavailable");provider.InitSpriteEditorDataProvider();
        var capability=provider.GetDataProvider<ISpriteFrameEditCapability>();
        if(capability==null||!capability.GetEditCapability().HasCapability(EEditCapability.CreateAndDeleteSprite))throw new InvalidOperationException("Importer cannot slice sprites");
        var existing=provider.GetSpriteRects();
        var rects=Entries.Select(e=>new SpriteRect{name=e.id,rect=e.rect,pivot=new Vector2(.5f,.5f),alignment=SpriteAlignment.Center,spriteID=existing.FirstOrDefault(s=>s.name==e.id)?.spriteID??GUID.Generate()}).ToArray();
        provider.SetSpriteRects(rects);
        var names=provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if(names==null)throw new InvalidOperationException("Sprite name ID provider unavailable");
        names.SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));
        provider.Apply();importer.SaveAndReimport();
    }
    static void Localize(LocalizationTable table,string key,string ru,string en)
    {
        var entry=table.entries.Find(e=>e.key==key);if(entry==null){entry=new LocalizationTable.Entry{key=key};table.entries.Add(entry);}
        while(entry.values.Count<table.languages.Count)entry.values.Add("");
        int ri=table.languages.IndexOf("ru"),ei=table.languages.IndexOf("en");if(ri>=0)entry.values[ri]=ru;if(ei>=0)entry.values[ei]=en;
    }
}
#endif
