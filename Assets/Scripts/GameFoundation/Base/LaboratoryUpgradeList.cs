using System.Linq;
using GameFoundation.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    /// <summary>Presentation only. Purchases and save state remain in GlobalStats.</summary>
    public sealed class LaboratoryUpgradeList : MonoBehaviour
    {
        public GameFoundation.MetaProgression.PortalTowerProgression portalProgression;
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
            if (portalProgression != null) portalProgression.Changed += Refresh;
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
            if (portalProgression != null) portalProgression.Changed -= Refresh;
            if (language != null) language.LanguageChanged -= Refresh;
            language = null;
        }
        private void ResourceChanged(ResourceType _, int __) => Refresh();
        public void Refresh()
        {
            if (portalProgression != null) { RefreshPortal(); return; }
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
        public string Title(ScientificUpgradeTable.Entry entry) => portalProgression != null ? entry.title :
            Tr("laboratory.group." + entry.GroupId, string.IsNullOrEmpty(entry.groupTitle) ? entry.title : entry.groupTitle);
        public void Select(LaboratoryUpgradeRow row)
        {
            if (row.Entry == null) return;
            selected = row;
            foreach (var item in rows) item.SetSelected(item == row);
            if (portalProgression != null) { SelectPortal(row); return; }
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
            bool purchased = portalProgression != null ? portalProgression.Purchase(id) : stats != null && stats.TryPurchaseUpgrade(id);
            Refresh();
            return purchased;
        }
        public static string Tr(string key, string fallback)
        {
            string value = LocalizationService.Instance != null ? LocalizationService.Instance.Get(key) : key;
            return value == key ? fallback : value;
        }
        private bool English => LocalizationService.Instance != null && LocalizationService.Instance.Language == "en";
        private string Text(string ru, string en) => English ? en : ru;
        private void RefreshPortal()
        {
            if (content == null || portalProgression.Balance == null) return;
            rows ??= content.GetComponentsInChildren<LaboratoryUpgradeRow>(true);
            var upgrades = portalProgression.Balance.upgrades;
            foreach (var row in rows) row.gameObject.SetActive(upgrades.Any(u => u.id == row.groupId));
            int index = 0;
            foreach (var upgrade in upgrades)
            {
                var row = rows.FirstOrDefault(r => r.groupId == upgrade.id);
                if (row == null && rowPrefab != null)
                {
                    row = Instantiate(rowPrefab, content);
                    row.name = upgrade.id; row.groupId = upgrade.id;
                    rows = rows.Append(row).ToArray();
                }
                if (row == null) continue;
                row.gameObject.SetActive(true); row.transform.SetSiblingIndex(index++); row.Bind(this);
                int rank = portalProgression.Rank(upgrade.id);
                var entry = new ScientificUpgradeTable.Entry { id = upgrade.id, groupId = upgrade.id,
                    title = Text(upgrade.title, upgrade.englishTitle), description = Text(upgrade.description, upgrade.englishDescription),
                    icon = upgrade.icon, cost = 1, level = portalProgression.IsComplete(upgrade) ? rank : rank + 1 };
                row.Refresh(entry, rank, upgrade.maximumRank, null);
            }
            totalLabel.text = Text("Уровень башни: ", "Tower level: ") + portalProgression.Level +
                Text("  ·  Очки улучшений: ", "  ·  Upgrade points: ") + portalProgression.LevelPoints;
            var availableButtons = rows.Where(r => r.gameObject.activeSelf && r.purchaseButton.interactable)
                .OrderBy(r => r.transform.GetSiblingIndex()).Select(r => r.purchaseButton).ToArray();
            for (int i = 0; i < availableButtons.Length; i++)
            {
                availableButtons[i].navigation = new Navigation { mode = Navigation.Mode.Explicit,
                    selectOnUp = availableButtons[(i + availableButtons.Length - 1) % availableButtons.Length],
                    selectOnDown = availableButtons[(i + 1) % availableButtons.Length] };
            }
            if (selected == null || !selected.gameObject.activeSelf || selected.IsComplete)
                selected = rows.FirstOrDefault(r => r.gameObject.activeSelf && !r.IsComplete &&
                    portalProgression.IsUnlocked(portalProgression.Balance.Find(r.groupId))) ?? rows.FirstOrDefault(r => r.gameObject.activeSelf);
            if (selected != null) Select(selected);
        }
        private void SelectPortal(LaboratoryUpgradeRow row)
        {
            var upgrade = portalProgression.Balance.Find(row.Entry.id);
            int rank = portalProgression.Rank(upgrade.id);
            detailTitle.text = row.Entry.title;
            detailLevel.text = Text("Улучшений: ", "Upgrades: ") + rank +
                (upgrade.maximumRank > 0 ? " / " + upgrade.maximumRank : " / ∞");
            detailDescription.text = row.Entry.description;
            ResourceIconSizing.Apply(detailIcon, upgrade.icon); detailIcon.enabled = upgrade.icon != null;
            detailRequirement.text = Text("Очков уровня: ", "Level points: ") + portalProgression.LevelPoints +
                (upgrade.requiresAttack && !portalProgression.CanAttack ? Text("\nСначала откройте стрельбу башни.", "\nUnlock tower attacks first.") : "");
            detailPrice.SetActive(true);
            ResourceIconSizing.Apply(detailCostIcon, portalProgression.Balance.levelPointIcon);
            detailCost.text = "1";
            detailStatus.text = row.IsComplete ? Text("Все уровни улучшения получены", "Upgrade fully learned") :
                !portalProgression.IsUnlocked(upgrade) ? Text("Сначала откройте стрельбу башни", "Unlock tower attacks first") :
                Text("Выберите улучшение слева. Цена — одно очко уровня.", "Choose an upgrade on the left. Cost: one level point.");
            detailStatus.color = row.statusLabel.color;
        }
    }
}
