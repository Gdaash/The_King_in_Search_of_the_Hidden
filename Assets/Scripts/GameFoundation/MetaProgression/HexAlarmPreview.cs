using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.MetaProgression
{
    /// <summary>Uses the alarm system's actual orb grouping and threshold calculations.</summary>
    public sealed class HexAlarmPreview : MonoBehaviour
    {
        [SerializeField] private Image orbTemplate;
        [SerializeField, Min(.01f)] private float orbScale = 1f;
        [SerializeField, Min(0f)] private float gap = 2f;
        [SerializeField, Min(1)] private int columns = 5;
        [SerializeField, Min(0f)] private float rowGap = 4f;
        private readonly List<Image> orbs = new();
        private AlarmSystem alarm;

        public void Show(float amount, float reservedForAction = 0f)
        {
            if (alarm == null) alarm = AlarmSystem.Instance ?? FindFirstObjectByType<AlarmSystem>();
            int count = alarm != null ? alarm.PreviewOrbCount(amount, reservedForAction) : 0;
            while (orbs.Count < count)
            {
                var orb = Instantiate(orbTemplate, transform);
                orbs.Add(orb);
            }
            orbTemplate.gameObject.SetActive(false);
            for (int i = 0; i < orbs.Count; i++)
            {
                var orb = orbs[i];
                orb.gameObject.SetActive(i < count);
                if (i >= count) continue;
                orb.sprite = alarm.PreviewOrbRaisesLevel(amount, i, reservedForAction) ? alarm.SkullSprite : alarm.InactiveSkullSprite;
                orb.color = Color.white;
                Vector2 size = (orb.sprite != null ? orb.sprite.rect.size * 2f : new Vector2(24, 24)) * orbScale;
                orb.rectTransform.sizeDelta = size;
                int row = i / columns;
                int rowCount = Mathf.Min(columns, count - row * columns);
                orb.rectTransform.anchoredPosition = new Vector2(
                    (i % columns - (rowCount - 1) * .5f) * (size.x + gap),
                    row * (size.y + rowGap));
            }
        }
    }
}
