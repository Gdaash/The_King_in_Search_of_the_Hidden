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
            var alarmGroup = (CanvasGroup)view.FindPropertyRelative("alarmGroup").objectReferenceValue;
            if (alarmGroup == null) continue;
            var existing = alarmGroup.transform.Find("Action Alarm Preview");
            var row = existing != null ? existing.gameObject :
                new GameObject("Action Alarm Preview", typeof(RectTransform), typeof(HexAlarmPreview));
            if (existing == null) row.transform.SetParent(alarmGroup.transform, false);
            var rect = (RectTransform)row.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(282, 52);
            var orb = row.transform.Find("Orb Template");
            if (orb == null)
            {
                orb = new GameObject("Orb Template", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).transform;
                orb.SetParent(row.transform, false);
            }
            // The threshold state is now the row icon itself, not an extra skull above it.
            var oldMarker = orb.Find("Threshold Skull");
            if (oldMarker != null) Object.DestroyImmediate(oldMarker.gameObject);
            var image = orb.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            var preview = row.GetComponent<HexAlarmPreview>();
            var binding = new SerializedObject(preview);
            binding.FindProperty("orbScale").floatValue = 1f;
            var alarm = AssetDatabase.LoadAssetAtPath<GameObject>(AlarmBarPsdSetup.Bar).GetComponent<AlarmSystem>();
            if (alarm != null)
            {
                image.sprite = alarm.InactiveSkullSprite;
                image.color = Color.white;
                image.rectTransform.sizeDelta = (image.sprite != null ? image.sprite.rect.size * 2 : new Vector2(52, 52)) *
                    binding.FindProperty("orbScale").floatValue;
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
