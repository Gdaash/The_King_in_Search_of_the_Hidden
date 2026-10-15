using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways, DefaultExecutionOrder(200)]
public sealed class BottomRunBarsLayout : MonoBehaviour
{
    public RectTransform experience,alarm,controls,returnPanel,autoRepeat;
    private RectTransform resources;
    readonly Vector3[] corners=new Vector3[4];
    readonly List<RectTransform> alarmParts = new();
    void LateUpdate()
    {
        if(!experience||!alarm)return;
        var owner = GetComponentInParent<Canvas>();
        if (!owner) return;
        var canvas=(RectTransform)owner.rootCanvas.transform;
        if (!resources)
            foreach (var row in owner.rootCanvas.GetComponentsInChildren<ResourceUI>(true))
                if (row.DisplaysGlobalResources) { resources = row.transform as RectTransform; break; }

        // Keep the experience bar at its authored height, independent of the side panels.
        var center = canvas.InverseTransformPoint(experience.TransformPoint(experience.rect.center));
        Center(experience, canvas, center.y);
        float bottom = canvas.rect.yMax - 64;
        if (resources) { resources.GetWorldCorners(corners); bottom = canvas.InverseTransformPoint(corners[0]).y; }

        // Include skulls and their labels so the whole danger bar clears the resource row.
        alarm.GetComponentsInChildren(false, alarmParts);
        float top = float.MinValue;
        foreach (var part in alarmParts)
        {
            part.GetWorldCorners(corners);
            foreach (var corner in corners) top = Mathf.Max(top, canvas.InverseTransformPoint(corner).y);
        }
        center = canvas.InverseTransformPoint(alarm.TransformPoint(alarm.rect.center));
        Center(alarm, canvas, bottom - 8 - (top - center.y));
    }
    static void Center(RectTransform rect, RectTransform canvas, float y)
    {
        var current = rect.TransformPoint(rect.rect.center);
        var local = canvas.InverseTransformPoint(current);
        var target = canvas.TransformPoint(new Vector3(canvas.rect.center.x, y, local.z));
        rect.position += target - current;
    }
}
