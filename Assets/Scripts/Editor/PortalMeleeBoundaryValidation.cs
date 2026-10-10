using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

public static class PortalMeleeBoundaryValidation
{
    public static void Run()
    {
        var portal = GameObject.Find("PortalTower");
        var boundary = portal.GetComponent<PortalMeleeBoundary>();
        typeof(PortalMeleeBoundary).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(boundary, null);
        var go = new GameObject("Melee boundary validation");
        try
        {
            go.tag = "Enemy1";
            var ai = go.AddComponent<EnemyAI>();
            typeof(EnemyAI).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(ai, null);
            ai.OnStop = new UnityEvent(); ai.OnMove = new UnityEvent(); ai.OnAttack = new UnityEvent();
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            typeof(EnemyAI).GetField("_target", flags).SetValue(ai, portal.transform);
            var combat = typeof(EnemyAI).GetMethod("HandleCombat", flags);
            bool moving = false, attacked = false;
            ai.OnMove.AddListener(() => moving = true);
            ai.OnStop.AddListener(() => moving = false);
            ai.OnAttack.AddListener(() => attacked = true);
            for (int angle = 0; angle < 360; angle += 15)
            {
                Vector2 direction = Quaternion.Euler(0, 0, angle) * Vector2.right;
                go.transform.position = portal.transform.position + (Vector3)(direction * 4f);
                attacked = false;
                ai.FinishAttack();
                typeof(EnemyAI).GetField("_nextAttackTime", flags).SetValue(ai, -1f);
                for (int step = 0; step < 300 && !attacked; step++)
                {
                    combat.Invoke(ai, null);
                    if (moving) go.transform.position -= (Vector3)(direction * 0.03f);
                }
                float distance = Vector2.Distance(go.transform.position, portal.transform.position);
                if (!attacked || moving || distance < 1.45f || distance > 1.95f || ai.GetTarget() != portal.transform)
                    throw new Exception("Incorrect portal attack at angle " + angle + ", distance " + distance);
            }
            Debug.Log("PASS: melee stops and attacks the real portal at its hex boundary from 24 directions.");
        }
        finally
        {
            var ai = go.GetComponent<EnemyAI>();
            var field = typeof(EnemyAI).GetField("_offsetAnchor", BindingFlags.NonPublic | BindingFlags.Instance);
            var anchor = field.GetValue(ai) as GameObject;
            field.SetValue(ai, null);
            if (anchor != null) UnityEngine.Object.DestroyImmediate(anchor);
            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
