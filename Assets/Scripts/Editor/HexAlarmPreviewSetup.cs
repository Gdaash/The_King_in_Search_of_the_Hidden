#if UNITY_EDITOR
using GameFoundation.MetaProgression;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class HexAlarmPreviewSetup
{
    public static void Configure(GameObject root)
    {
        var hover = new SerializedObject(root.GetComponent<CrystalHexHover>());
        var views = hover.FindProperty("views");
        for (int i = 0; i < views.arraySize; i++)
        {
            var view = views.GetArrayElementAtIndex(i);
            var energy = (CanvasGroup)view.FindPropertyRelative("energyGroup").objectReferenceValue;
            if (energy == null) continue;
            var existing = energy.transform.Find("Action Alarm Preview");
            var row = existing != null ? existing.gameObject :
                new GameObject("Action Alarm Preview", typeof(RectTransform), typeof(HexAlarmPreview));
            if (existing == null) row.transform.SetParent(energy.transform, false);
            var rect = (RectTransform)row.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(0, 42);
            rect.sizeDelta = new Vector2(282, 52);
            var orb = row.transform.Find("Orb Template");
            if (orb == null)
            {
                orb = new GameObject("Orb Template", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).transform;
                orb.SetParent(row.transform, false);
                var skull = new GameObject("Threshold Skull", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                skull.transform.SetParent(orb, false);
                skull.GetComponent<Image>().raycastTarget = false;
                skull.GetComponent<Image>().preserveAspect = true;
            }
            var image = orb.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            var preview = row.GetComponent<HexAlarmPreview>();
            var binding = new SerializedObject(preview);
            binding.FindProperty("orbScale").floatValue = 1f;
            var alarm = Object.FindFirstObjectByType<AlarmSystem>();
            if (alarm != null)
            {
                image.sprite = alarm.OrbSprite;
                image.color = alarm.OrbColor;
                image.rectTransform.sizeDelta = (image.sprite != null ? image.sprite.rect.size * 2 : new Vector2(52, 52)) *
                    binding.FindProperty("orbScale").floatValue;
                var skull = orb.GetChild(0).GetComponent<Image>();
                skull.sprite = alarm.SkullSprite;
                skull.rectTransform.sizeDelta = skull.sprite != null ? skull.sprite.rect.size * 2 : Vector2.zero;
            }
            orb.gameObject.SetActive(false);
            binding.FindProperty("orbTemplate").objectReferenceValue = image;
            binding.ApplyModifiedPropertiesWithoutUndo();
            view.FindPropertyRelative("alarmPreview").objectReferenceValue = preview;
        }
        hover.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
