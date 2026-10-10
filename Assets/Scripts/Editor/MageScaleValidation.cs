#if UNITY_EDITOR
using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class MageScaleValidation
{
    const string Key="MageScaleValidation";
    static MageScaleValidation()=>EditorApplication.playModeStateChanged+=State;
    public static void Run(){SessionState.SetBool(Key,true);BuildingUpgradeValidation.Begin();}
    static async void State(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Key,false)||state!=PlayModeStateChange.EnteredPlayMode)return;
        GameObject mage=null,target=null;
        try
        {
            await Task.Delay(300);GameSpeedControls.SetSimulationSpeed(1);
            mage=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Units/CursedMage.prefab"),new Vector3(100,100),Quaternion.identity);
            var ai=mage.GetComponent<EnemyAI_Ranged>();ai.enabled=false;
            mage.GetComponent<EnemyMovement>().enabled=false;
            var visual=mage.GetComponent<EnemyVisuals_Ranged>();var health=mage.GetComponent<Health>();
            target=new GameObject("Scale test target");target.transform.position=mage.transform.position+Vector3.right*3;
            typeof(EnemyAI_Ranged).GetField("_target",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(ai,target.transform);
            var graphics=(Transform)typeof(EnemyVisuals_Ranged).GetField("spriteParent",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(visual);
            var original=graphics.localScale;
            await Task.Delay(100);
            for(int round=0;round<4;round++)
            {
                visual.StartShoot();await Task.Delay(110);
                for(int hit=0;hit<4;hit++){health.TakeDamage(1,DamageType.Physical,null);await Task.Delay(40);}
                await Task.Delay(1200);
                if((graphics.localScale-original).sqrMagnitude>.0001f)throw new Exception("Mage retained stretched scale after shooting/hits: "+graphics.localScale);
            }
            mage.SetActive(false);mage.SetActive(true);
            if((graphics.localScale-original).sqrMagnitude>.0001f)throw new Exception("Scale changed on re-enable");
            SessionState.SetString(Key+".result","PASS: repeated hits during four shots and re-enable preserve mage scale");
        }
        catch(Exception e){SessionState.SetString(Key+".result","FAILED: "+e.Message);}
        finally{if(mage)Object.Destroy(mage);if(target)Object.Destroy(target);SessionState.SetBool(Key,false);Debug.Log(SessionState.GetString(Key+".result",""));EditorApplication.isPlaying=false;}
    }
}
#endif
