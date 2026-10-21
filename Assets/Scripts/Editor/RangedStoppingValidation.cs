using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class RangedStoppingValidation
{
    public static void Run()
    {
        var target=new GameObject("Ranged QA portal");target.transform.position=new Vector3(100,100,0);
        foreach(string name in new[]{"Bat","FlyingEye","CursedMage","RedArcher","RedSniper"})
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Units/"+name+".prefab");
            var unit=Object.Instantiate(prefab);var ai=unit.GetComponent<EnemyAI_Ranged>();
            if(ai==null)throw new Exception("Missing ranged AI: "+name);
            var targetField=typeof(EnemyAI_Ranged).GetField("_target",BindingFlags.Instance|BindingFlags.NonPublic);targetField.SetValue(ai,target.transform);
            var handle=typeof(EnemyAI_Ranged).GetMethod("HandleCombat",BindingFlags.Instance|BindingFlags.NonPublic);
            var move=unit.GetComponent<EnemyMovement>();var moving=typeof(EnemyMovement).GetField("_canMove",BindingFlags.Instance|BindingFlags.NonPublic);
            unit.transform.position=target.transform.position+Vector3.right*(ai.CurrentAttackRange+1);
            handle.Invoke(ai,null);
            if(!(bool)moving.GetValue(move))throw new Exception(name+" must approach from outside firing range");
            unit.transform.position=target.transform.position+Vector3.right*(ai.CurrentAttackRange*.98f);
            handle.Invoke(ai,null);
            if((bool)moving.GetValue(move)||!ai.GetIsAttacking()||unit.GetComponent<Rigidbody2D>().linearVelocity.sqrMagnitude>.001f)
                throw new Exception(name+" must stop immediately at firing distance before shooting");
            unit.transform.position=target.transform.position+Vector3.right*(ai.CurrentAttackRange+1);
            handle.Invoke(ai,null);
            if((bool)moving.GetValue(move))throw new Exception(name+" must remain stopped during attack windup");
            ai.FinishAttack();handle.Invoke(ai,null);
            if(!(bool)moving.GetValue(move))throw new Exception(name+" must resume pursuit when target leaves range");
            unit.SetActive(false);Object.Destroy(unit);
        }
        Object.Destroy(target);SessionState.SetString("RangedStoppingQA","PASS: Bat, FlyingEye, CursedMage, RedArcher, RedSniper stop at range, remain stopped during attack and resume pursuit");
    }
}
