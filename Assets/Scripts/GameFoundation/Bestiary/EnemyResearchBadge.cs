using GameFoundation.Localization;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameFoundation.Bestiary
{
    /// <summary>Kill progress and the discovered weakness in a library entry.</summary>
    public sealed class EnemyResearchBadge : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public UnityEngine.UI.Text label;
        private EnemyIdentity identity;
        private BestiaryEntryView owner;
        private GameObject tooltip;

        public void Bind(EnemyIdentity enemy, BestiaryEntryView entry)
        {
            identity = enemy; owner = entry;
            bool studied = BestiaryService.IsStudied(enemy);
            label.text = studied ? "×1.2" : BestiaryService.Kills(enemy != null ? enemy.Id : null) + " / " + (enemy != null ? BestiaryService.RequiredKills(enemy.DangerRank) : 50);
            label.color = studied ? new Color(.6f, .85f, .47f) : new Color(.95f, .83f, .49f);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!BestiaryService.IsStudied(identity)) return;
            owner?.SetResearchHover(true, eventData);
            ShowTooltip();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (tooltip != null) tooltip.SetActive(false);
            owner?.SetResearchHover(false, eventData);
        }

        private void ShowTooltip()
        {
            var root = GetComponentInParent<Canvas>()?.rootCanvas;
            if (root == null) return;
            if (tooltip == null)
            {
                tooltip = new GameObject("Enemy weakness tooltip", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
                tooltip.transform.SetParent(root.transform, false);
                var rect = (RectTransform)tooltip.transform;
                rect.sizeDelta = new Vector2(300, 120); rect.pivot = new Vector2(1, 0);
                var image = tooltip.GetComponent<UnityEngine.UI.Image>();
                image.sprite = GetComponent<UnityEngine.UI.Image>()?.sprite;
                image.type = UnityEngine.UI.Image.Type.Sliced;
                image.color = new Color(.12f, .09f, .15f, .98f); image.raycastTarget = false;
                var textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Text));
                textObject.transform.SetParent(tooltip.transform, false);
                var textRect = (RectTransform)textObject.transform;
                textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(14, 10); textRect.offsetMax = new Vector2(-14, -10);
                var text = textObject.GetComponent<UnityEngine.UI.Text>();
                text.font = label.font; text.fontSize = 20; text.alignment = TextAnchor.MiddleCenter;
                text.color = new Color(.65f, .9f, .52f); text.raycastTarget = false;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
            bool en = LocalizationService.Instance?.Language == "en";
            string key = "bestiary.research.weakness";
            string content = LocalizationService.Instance?.Get(key);
            tooltip.GetComponentInChildren<UnityEngine.UI.Text>().text = !string.IsNullOrEmpty(content) && content != key ? content
                : en ? "Weakness discovered!\nAll damage taken by this monster is increased by 20%." : "Уязвимость найдена!\nВесь урон по этому монстру увеличен на 20%.";
            tooltip.SetActive(true); tooltip.transform.SetAsLastSibling();
            var corners = new Vector3[4]; ((RectTransform)transform).GetWorldCorners(corners);
            var rootRect = (RectTransform)root.transform;
            Vector3 p = rootRect.InverseTransformPoint(corners[2]);
            p.x = Mathf.Clamp(p.x, rootRect.rect.xMin + 308, rootRect.rect.xMax - 8);
            p.y = Mathf.Clamp(p.y + 8, rootRect.rect.yMin + 8, rootRect.rect.yMax - 128);
            tooltip.transform.position = rootRect.TransformPoint(p);
        }

        private void OnDisable() { if (tooltip != null) tooltip.SetActive(false); }
        private void OnDestroy() { if (tooltip != null) Destroy(tooltip); }
    }
}
