using System.Linq;
using GameFoundation.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    /// <summary>Presentation only. Purchases and save state remain in GlobalStats.</summary>
    public sealed class LaboratoryUpgradeList : MonoBehaviour
    {
        public GlobalStats stats;
        public RectTransform content;
        public LaboratoryUpgradeRow rowPrefab;
        public ScrollRect scroll;
        public Text totalLabel, detailTitle, detailLevel, detailDescription, detailRequirement, detailStatus, detailCost;
        public Image detailIcon, detailCostIcon;
        public GameObject detailPrice;
        private LaboratoryUpgradeRow[] rows;
        private LaboratoryUpgradeRow selected;
        private LocalizationService language;

        private void OnEnable()
        {
            GlobalResourceManager.OnResourceChanged += ResourceChanged;
            GameFoundation.Quests.ContentUnlocks.Changed += Refresh;
            if (stats != null) stats.OnStatsUpdated += Refresh;
            language = LocalizationService.Instance;
            if (language != null) language.LanguageChanged += Refresh;
            Refresh();
            if (scroll != null) scroll.verticalNormalizedPosition = 1;
        }
        private void OnDisable()
        {
            GlobalResourceManager.OnResourceChanged -= ResourceChanged;
            GameFoundation.Quests.ContentUnlocks.Changed -= Refresh;
            if (stats != null) stats.OnStatsUpdated -= Refresh;
            if (language != null) language.LanguageChanged -= Refresh;
            language = null;
        }
        private void ResourceChanged(ResourceType _, int __) => Refresh();
        public void Refresh()
        {
            if (stats == null || stats.UpgradeTable == null || content == null) return;
            rows ??= content.GetComponentsInChildren<LaboratoryUpgradeRow>(true);
            var groups = stats.UpgradeTable.entries.Where(e => e != null && e.IsAvailable).GroupBy(e => e.GroupId).ToArray();
            foreach (var row in rows) row.gameObject.SetActive(groups.Any(g => g.Key == row.groupId));
            foreach (var group in groups)
            {
                var row = rows.FirstOrDefault(r => r.groupId == group.Key);
                if (row == null && rowPrefab != null)
                {
                    row = Instantiate(rowPrefab, content);
                    row.name = group.Key;
                    row.groupId = group.Key;
                    rows = rows.Append(row).ToArray();
                }
                if (row == null) continue;
                var levels = group.OrderBy(e => e.level).ToArray();
                var next = levels.FirstOrDefault(e => !stats.HasUpgrade(e.id)) ?? levels.Last();
                row.gameObject.SetActive(true);
                row.Bind(this);
                row.Refresh(next, levels.Count(e => stats.HasUpgrade(e.id)), levels.Length, stats);
            }
            int index = 0;
            foreach (var row in rows.Where(r => r.gameObject.activeSelf && r.Entry != null)
                         .OrderBy(r => r.Entry.requiredPurchases).ThenBy(r => r.groupId, System.StringComparer.Ordinal))
                row.transform.SetSiblingIndex(index++);
            totalLabel.text = Tr("laboratory.total", "Изучено улучшений") + ": " + stats.PurchasedUpgradeCount + " / " + stats.UpgradeTable.entries.Count(e => e.IsAvailable);
            if (selected == null || !selected.gameObject.activeSelf)
                selected = rows.Where(r => r.gameObject.activeSelf).OrderBy(r => r.transform.GetSiblingIndex()).FirstOrDefault();
            if (selected != null) Select(selected);
            else
            {
                detailTitle.text = "Исследования пока не открыты";
                detailLevel.text = detailDescription.text = detailRequirement.text = detailStatus.text = "";
                detailPrice.SetActive(false); detailIcon.enabled = false;
            }
        }
        public string Title(ScientificUpgradeTable.Entry entry) => Tr("laboratory.group." + entry.GroupId,
            string.IsNullOrEmpty(entry.groupTitle) ? entry.title : entry.groupTitle);
        public void Select(LaboratoryUpgradeRow row)
        {
            if (row.Entry == null) return;
            selected = row;
            foreach (var item in rows) item.SetSelected(item == row);
            var entry = row.Entry;
            var levels = stats.UpgradeTable.entries.Where(e => e.GroupId == entry.GroupId && e.IsAvailable).ToArray();
            detailTitle.text = Title(entry);
            detailLevel.text = Tr("laboratory.learned_levels", "Изучено уровней") + ": " + levels.Count(e => stats.HasUpgrade(e.id)) + " / " + levels.Length;
            detailDescription.text = Tr("skill." + entry.id + ".description", entry.description);
            ResourceIconSizing.Apply(detailIcon, entry.icon);
            detailIcon.enabled = entry.icon != null;
            detailRequirement.text = Tr("laboratory.requirement", "Нужно изучить улучшений") + ": " + entry.requiredPurchases +
                "\n" + Tr("laboratory.already", "Уже изучено") + ": " + stats.PurchasedUpgradeCount;
            detailPrice.SetActive(true);
            ResourceIconSizing.Apply(detailCostIcon, entry.costResource != null ? entry.costResource.resourceIcon : null);
            detailCost.text = entry.cost.ToString();
            detailStatus.text = row.IsComplete ? Tr("laboratory.maximum", "Все уровни этого улучшения изучены") :
                !stats.IsUpgradeUnlocked(entry) ? Tr("laboratory.unlock_hint", "Изучайте другие улучшения, чтобы открыть этот уровень") :
                stats.CanPurchaseUpgrade(entry) ? Tr("laboratory.buy_hint", "Нажмите на улучшение слева, чтобы изучить") :
                Tr("laboratory.no_resources", "Не хватает ресурсов");
            detailStatus.color = row.statusLabel.color;
        }
        public bool Purchase(string id)
        {
            bool purchased = stats != null && stats.TryPurchaseUpgrade(id);
            Refresh();
            return purchased;
        }
        public static string Tr(string key, string fallback)
        {
            string value = LocalizationService.Instance != null ? LocalizationService.Instance.Get(key) : key;
            return value == key ? fallback : value;
        }
    }
}
