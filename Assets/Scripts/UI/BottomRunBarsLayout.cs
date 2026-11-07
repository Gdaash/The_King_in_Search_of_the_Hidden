using GameFoundation.MetaProgression;
using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways, DefaultExecutionOrder(200)]
public sealed class BottomRunBarsLayout : MonoBehaviour
{
    public RectTransform experience,alarm,controls,returnPanel,autoRepeat;
    private RectTransform resources;
    private PortalWeaponSlots battleSlots;
    private PortalTowerExperienceBar energyBar;
    private UnityEngine.UI.Image speedFrame;
    private UnityEngine.UI.Image returnFrame;
    // Day Panel has two transparent source pixels above its painted frame.
    private const float DayPanelTopPadding = 2f;
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
        if (!battleSlots && experience.parent != null)
            battleSlots = experience.parent.GetComponentInChildren<PortalWeaponSlots>(true);
        if (battleSlots && battleSlots.context == PortalWeaponSlots.Context.Battle && battleSlots.isActiveAndEnabled)
        {
            var slots = (RectTransform)battleSlots.transform;
            if (controls && returnPanel)
            {
                if (!energyBar) energyBar = experience.GetComponent<PortalTowerExperienceBar>();
                var left = BoundsInCanvas(controls, canvas);
                var right = BoundsInCanvas(returnPanel, canvas);
                if (!speedFrame) speedFrame = controls.GetComponent<UnityEngine.UI.Image>();
                if (!returnFrame) returnFrame = returnPanel.GetComponent<UnityEngine.UI.Image>();
                float visibleTop = left.yMax - FrameTopPadding(speedFrame, canvas);
                Move(returnPanel, canvas, 0, visibleTop - (right.yMax - FrameTopPadding(returnFrame, canvas)));
                var slotBounds = BoundsInCanvas(slots, canvas);
                var barBounds = BoundsInCanvas(experience, canvas);
                var crystal = energyBar ? energyBar.TopCrystal : null;
                float barTop = crystal ? Mathf.Max(barBounds.yMax, BoundsInCanvas(crystal, canvas).yMax) : barBounds.yMax;
                float gap = (right.xMin - left.xMax - slotBounds.width - barBounds.width) / 3f;
                if (gap >= 0)
                {
                    Move(slots, canvas, left.xMax + gap - slotBounds.xMin, visibleTop - slotBounds.yMax);
                    Move(experience, canvas, right.xMin - gap - barBounds.xMax, visibleTop - barTop);
                }
                else
                {
                    // Narrow viewports keep native slot sizes and reserve another row.
                    float fallbackTop = Mathf.Max(left.yMax, right.yMax) + Mathf.Max(slotBounds.height, barTop - barBounds.yMin) + 8;
                    Center(slots, canvas, fallbackTop - slotBounds.height * .5f, -(barBounds.width + 12) * .5f);
                    Center(experience, canvas, fallbackTop - (barTop - barBounds.center.y), (slotBounds.width + 12) * .5f);
                }
            }
        }
        else Center(experience, canvas, center.y);
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
    private static float FrameTopPadding(UnityEngine.UI.Image frame, RectTransform canvas)
    {
        if (!frame || !frame.sprite || frame.sprite.name != "Day Panel") return 0f;
        float pixelsPerUnit = frame.type == UnityEngine.UI.Image.Type.Sliced
            ? frame.pixelsPerUnit * frame.pixelsPerUnitMultiplier
            : frame.sprite.rect.height / frame.rectTransform.rect.height;
        var padding = frame.rectTransform.TransformVector(Vector3.up * (DayPanelTopPadding / pixelsPerUnit));
        return canvas.InverseTransformVector(padding).y;
    }

    private Rect BoundsInCanvas(RectTransform rect, RectTransform canvas)
    {
        rect.GetWorldCorners(corners);
        var min = canvas.InverseTransformPoint(corners[0]);
        var max = canvas.InverseTransformPoint(corners[2]);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private static void Move(RectTransform rect, RectTransform canvas, float x, float y)
    {
        rect.position += canvas.TransformVector(new Vector3(x, y));
    }

    static void Center(RectTransform rect, RectTransform canvas, float y, float x = 0)
    {
        var current = rect.TransformPoint(rect.rect.center);
        var local = canvas.InverseTransformPoint(current);
        var target = canvas.TransformPoint(new Vector3(canvas.rect.center.x + x, y, local.z));
        rect.position += target - current;
    }
}
