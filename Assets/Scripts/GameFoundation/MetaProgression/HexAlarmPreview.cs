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
        private readonly List<Image> skulls = new();
        private AlarmSystem alarm;

        public void Show(float amount)
        {
            if (alarm == null) alarm = AlarmSystem.Instance ?? FindFirstObjectByType<AlarmSystem>();
            int count = alarm != null ? alarm.PreviewOrbCount(amount) : 0;
            while (orbs.Count < count)
            {
                var orb = Instantiate(orbTemplate, transform);
                var skull = orb.transform.GetChild(0).GetComponent<Image>();
                orbs.Add(orb);
                skulls.Add(skull);
            }
            orbTemplate.gameObject.SetActive(false);
            for (int i = 0; i < orbs.Count; i++)
            {
                var orb = orbs[i];
                orb.gameObject.SetActive(i < count);
                if (i >= count) continue;
                orb.sprite = alarm.OrbSprite;
                orb.color = alarm.OrbColor;
                Vector2 size = (orb.sprite != null ? orb.sprite.rect.size * 2f : new Vector2(24, 24)) * orbScale;
                orb.rectTransform.sizeDelta = size;
                int row = i / columns;
                int rowCount = Mathf.Min(columns, count - row * columns);
                orb.rectTransform.anchoredPosition = new Vector2(
                    (i % columns - (rowCount - 1) * .5f) * (size.x + gap),
                    row * (size.y + rowGap + (alarm.SkullSprite != null ? alarm.SkullSprite.rect.height * 2f : 0)));
                var skull = skulls[i];
                skull.sprite = alarm.SkullSprite;
                skull.color = Color.white;
                skull.rectTransform.sizeDelta = skull.sprite != null ? skull.sprite.rect.size * 2f : Vector2.zero;
                skull.rectTransform.anchoredPosition = new Vector2(0, size.y * .5f + skull.rectTransform.sizeDelta.y * .5f + 4);
                skull.gameObject.SetActive(alarm.PreviewOrbRaisesLevel(amount, i) && skull.sprite != null);
            }
        }
    }
}
