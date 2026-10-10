#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using GameFoundation.Combat;
[CustomEditor(typeof(CombatWeapon))]
public sealed class CombatWeaponEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();var w=(CombatWeapon)target;
        EditorGUILayout.HelpBox($"Базовый DPS по одной цели: {w.SingleTargetDps:0.##}. Для области — при попадании в цель каждого тика/кольца. Броня вычитается из каждого попадания; электричество игнорирует броню.",MessageType.Info);
    }
}
#endif
