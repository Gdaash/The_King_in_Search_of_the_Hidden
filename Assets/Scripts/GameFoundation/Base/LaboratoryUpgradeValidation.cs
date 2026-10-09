#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using GameFoundation.Saves;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    /// <summary>Explicit Play Mode checks. Requires the existing disposable-slot validation harness.</summary>
    public static class LaboratoryUpgradeValidation
    {
        private static void Check(bool condition, string message)
        { if (!condition) throw new InvalidOperationException("Laboratory validation: " + message); }

        public static void Run()
        {
            Check(Application.isPlaying && SaveSlotPrefs.SelectedSlot == 3 &&
                  UnityEditor.SessionState.GetString("BuildingUpgradeValidation.Backup", "") != "", "Start BuildingUpgradeValidation.Begin first");
            var ui = UnityEngine.Object.FindObjectsByType<BaseUIController>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(c => c.transform.root.gameObject.activeSelf);
            ui.OpenLaboratory();
            var view = UnityEngine.Object.FindFirstObjectByType<LaboratoryUpgradeList>();
            var stats = view.stats;
            var entries = stats.UpgradeTable.entries;
            Check(stats.PurchasedUpgradeCount == 0, "fresh test save expected");
            Check(view.content.childCount == entries.Select(e => e.GroupId).Distinct().Count(), "one authored row per group");
            Check(view.GetComponentsInChildren<LaboratoryPanZoom>(true).Length == 0, "tree navigation removed");
            var manager = GlobalResourceManager.Instance;
            foreach (var e in entries) manager.SetResourceAmount(e.costResource, 0);
            var first = entries.First(e => e.requiredPurchases == 0);
            Check(!view.Purchase(first.id) && !stats.HasUpgrade(first.id), "unaffordable purchase is inert");
            foreach (var e in entries) manager.SetResourceAmount(e.costResource, 1000);
            var locked = entries.First(e => e.requiredPurchases > 0);
            Check(!view.Purchase(locked.id), "locked purchase rejected even with resources");

            while (stats.PurchasedUpgradeCount < entries.Count)
            {
                // Buy the highest available gate first to exercise a different order from the UI list.
                var next = entries.Where(stats.CanPurchaseUpgrade).OrderByDescending(e => e.requiredPurchases).FirstOrDefault();
                Check(next != null, "no softlock at " + stats.PurchasedUpgradeCount + " purchases");
                int count = stats.PurchasedUpgradeCount, balance = manager.GetResourceAmount(next.costResource);
                var row = view.content.GetComponentsInChildren<LaboratoryUpgradeRow>().Single(r => r.Entry.id == next.id);
                ExecuteEvents.Execute<IPointerEnterHandler>(row.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
                Check(view.detailTitle.text == view.Title(next), "hover description follows the row");
                row.purchaseButton.onClick.Invoke();
                Check(stats.PurchasedUpgradeCount == count + 1 && stats.HasUpgrade(next.id), "each purchased level counts once");
                Check(manager.GetResourceAmount(next.costResource) == balance - next.cost, "exact resource cost");
                Check(!view.Purchase(next.id) && manager.GetResourceAmount(next.costResource) == balance - next.cost, "no repeat charge");
                var gates = view.content.GetComponentsInChildren<LaboratoryUpgradeRow>().Select(r => r.Entry.requiredPurchases).ToArray();
                Check(gates.SequenceEqual(gates.OrderBy(x => x)), "rows remain sorted after level changes");
            }
            Check(stats.AvailableFlashlightCount == 6, "all five light upgrades unlock six beams");
            Check(view.content.GetComponentsInChildren<LaboratoryUpgradeRow>().All(r => r.IsComplete && !r.purchaseButton.interactable && r.price.activeSelf), "all rows have purchased state and a visible price");
            GameSaveService.ResetCache();
            Check(stats.PurchasedUpgradeCount == entries.Count && entries.All(e => stats.HasUpgrade(e.id)), "all levels reload from the save envelope");

            view.scroll.verticalNormalizedPosition = 1;
            view.scroll.OnScroll(new PointerEventData(EventSystem.current) { scrollDelta = new Vector2(0, -4) });
            Check(view.content.anchoredPosition.y > 0 && view.content.localScale == Vector3.one, "wheel scrolls without zoom");
            var close = view.transform.Find("Window/Close").GetComponent<Button>();
            close.onClick.Invoke(); Check(!view.gameObject.activeSelf, "close callback works");
            ui.OpenLaboratory(); Check(view.stats.PurchasedUpgradeCount == entries.Count, "reopen preserves progression");
            Debug.Log("PASS Laboratory: all levels, resources, unlocks, sorting, hover, save reload, effect values, scrolling and close button.");
        }

        /// <summary>Run after the opened popup has rendered a frame: GraphicRaycaster uses rendered depth.</summary>
        public static void VerifyClosePointer()
        {
            var view = UnityEngine.Object.FindFirstObjectByType<LaboratoryUpgradeList>();
            Check(Application.isPlaying && view != null, "open the laboratory and allow a frame before pointer checks");
            var close = view.transform.Find("Window/Close").GetComponent<Button>();
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, close.transform.position) }, hits);
            Check(hits.Count > 0 && hits[0].gameObject == close.gameObject, "shared close button receives pointer input");
            Debug.Log("PASS Laboratory close pointer hit test.");
        }
    }
}
#endif
