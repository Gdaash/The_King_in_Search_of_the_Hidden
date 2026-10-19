#if UNITY_EDITOR
using GameFoundation.Combat;
using UnityEditor;
using UnityEngine;

/// <summary>Independent defenses per enemy; existing shared ally profiles remain unchanged.</summary>
public static class EnemyResistanceSetup
{
    // Physical, Fire, Ice, Magic, Electric. Order follows EnemyBalanceSetup.Order.
    static readonly float[,] Multipliers={
        {1,.5f,1.5f,1,1}, // Octopus: cold / fire
        {1,1.5f,.5f,1,1}, // Bat: fire / cold
        {.5f,1.5f,1,1,1}, // Slime: fire / physical
        {1,1,1.5f,.5f,1}, // Raider: cold / magic
        {1,1,1,.5f,1.5f}, // Eye: electricity / magic
        {.5f,1,1,1.5f,1}, // Skeleton: magic / physical
        {1.5f,1,1,.5f,1}, // Mage: physical / magic
        {.5f,1,1,1,1.5f}, // Guardian: electricity / physical
        {.5f,1,1,1,2}, // Knight: electricity / physical
        {.5f,1,.5f,1,1.5f}, // Warrior: electricity / physical and cold
        {.5f,.5f,1.5f,1,1}, // Demon: cold / physical and fire
        {.5f,1.5f,1,.5f,.5f} // Toxic demon: fire / physical, magic and electricity
    };
    public static void Run()
    {
        for(int i=0;i<EnemyBalanceSetup.Order.Length;i++)
        {
            string name=EnemyBalanceSetup.Order[i],path="Assets/Resources/Combat/Defense/"+name+" Defense.asset";
            var profile=AssetDatabase.LoadAssetAtPath<CombatDefenseProfile>(path);
            if(profile==null){profile=ScriptableObject.CreateInstance<CombatDefenseProfile>();AssetDatabase.CreateAsset(profile,path);}
            profile.physicalMultiplier=Multipliers[i,0];profile.fireMultiplier=Multipliers[i,1];profile.iceMultiplier=Multipliers[i,2];
            profile.magicalMultiplier=1;profile.magicOnlyMultiplier=Multipliers[i,3];profile.electricMultiplier=Multipliers[i,4];EditorUtility.SetDirty(profile);
            string prefabPath="Assets/Prefabs/Units/"+name+".prefab";var root=PrefabUtility.LoadPrefabContents(prefabPath);
            try{root.GetComponent<Combatant>().defense=profile;PrefabUtility.SaveAsPrefabAsset(root,prefabPath);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();Debug.Log("Enemy resistances installed: 1 weakness each, 1-3 resistances per type.");
    }
}
#endif
