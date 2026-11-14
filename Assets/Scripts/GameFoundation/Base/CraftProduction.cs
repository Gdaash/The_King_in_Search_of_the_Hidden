using System;
using System.Collections.Generic;
using GameFoundation.MetaProgression;
using GameFoundation.Saves;
using GameFoundation.UI;
using UnityEngine;

namespace GameFoundation.Base
{
    internal static class CraftProduction
    {
        internal const int Carpentry = 0, Smithy = 1, Tannery = 2, LeatherShop = 3, CharcoalPit = 4, WorkshopCount = 5;
        private static readonly string[] BuildingIds = { "blacksmith", "craft_smithy", CraftQuarterLayout.Tannery, "craft_leather_shop", CraftQuarterLayout.CharcoalPit };
        private const string SaveKey = "foundation.craft.production";
        internal static event Action Changed;
        private static bool mutating;
        private static string reservationSnapshot;
        private static int reservedResidents;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntime()
        {
            Changed = null;
            mutating = false;
            reservationSnapshot = null;
            reservedResidents = 0;
        }

        [Serializable] internal sealed class Order
        {
            public string resource;
            public int amount, days, firstWorkDay;
            public string input, fuel;
            public int price, fuelCost;
        }

        [Serializable] internal sealed class Workshop
        {
            public bool worker;
            public int startedDay;
            public List<Order> orders = new();
        }

        [Serializable] private sealed class State
        {
            public Workshop[] shops = { new(), new(), new(), new(), new() };
        }

        internal static bool Morning => DayCycleService.Instance?.EnteredToday != true;
        internal static int FreeResidents(ResourceType human)
        {
            if (human == null || GlobalResourceManager.Instance == null) return 0;
            int population = GlobalResourceManager.Instance.GetResourceAmount(human);
            if (!human.isHumanResource) return population;
            string json = SaveSlotPrefs.GetString(SaveKey);
            if (reservationSnapshot != json)
            {
                reservationSnapshot = json;
                reservedResidents = 0;
                foreach (var workshop in Parse(json).shops)
                    if (workshop.worker) reservedResidents++;
            }
            return Mathf.Max(0, population - reservedResidents);
        }

        internal static Workshop Read(int shop) => Load().shops[shop];
        private static State Load() => Parse(SaveSlotPrefs.GetString(SaveKey));
        private static State Parse(string json)
        {
            var state = string.IsNullOrEmpty(json) ? new State() : JsonUtility.FromJson<State>(json) ?? new State();
            if (state.shops.Length < WorkshopCount)
                Array.Resize(ref state.shops, WorkshopCount);
            state.shops[CharcoalPit] ??= new Workshop();
            var forge = state.shops[Smithy];
            state.shops[CharcoalPit].orders.InsertRange(0, forge.orders.FindAll(order => order.resource == "Coal"));
            forge.orders.RemoveAll(order => order.resource == "Coal");
            var tannery = state.shops[Tannery];
            var leather = state.shops[LeatherShop];
            if (leather.orders.Count > 0 && leather.orders[0].resource == "Leather")
            {
                tannery.worker |= leather.worker;
                leather.worker = false;
                tannery.startedDay = Mathf.Max(tannery.startedDay, leather.startedDay);
            }
            tannery.orders.InsertRange(0, leather.orders.FindAll(order => order.resource == "Leather"));
            leather.orders.RemoveAll(order => order.resource == "Leather");
            foreach (var workshop in state.shops)
                if (workshop.orders.Count == 0) workshop.worker = false;
            return state;
        }

        internal static bool CanQueue(int shop, ResourceType input, int amount, ResourceType fuel = null, int fuelCost = 0) =>
            BuildingUpgradeService.IsBuilt(BuildingIds[shop]) &&
            AvailableToday(Read(shop)) && FreeResidents(ResourceCatalog.Find("Human")) > 0 && input != null &&
            GlobalResourceManager.Instance != null && GlobalResourceManager.Instance.GetResourceAmount(input) >= amount &&
            (fuelCost == 0 || fuel != null && GlobalResourceManager.Instance.GetResourceAmount(fuel) >= fuelCost);

        internal static bool Queue(int shop, ResourceType input, int price, ResourceType output, int quantity, int days, ResourceType fuel = null, int fuelCost = 0)
        {
            if (mutating || output == null || !CanQueue(shop, input, price, fuel, fuelCost)) return false;
            var state = Load();
            using var batch = SaveSlotPrefs.Batch();
            using var feedback = GameNotifications.BeginAction();
            mutating = true;
            try
            {
                if (!GlobalResourceManager.Instance.TrySpendResource(input, price)) return false;
                if (fuelCost > 0 && !GlobalResourceManager.Instance.TrySpendResource(fuel, fuelCost))
                {
                    GlobalResourceManager.Instance.AddResource(input, price);
                    return false;
                }
                state.shops[shop].worker = true;
                state.shops[shop].startedDay = CurrentDay;
                state.shops[shop].orders.Add(new Order
                {
                    resource = output.Id, amount = quantity, days = days,
                    firstWorkDay = Morning ? CurrentDay : CurrentDay + 1,
                    input = input.Id, price = price, fuel = fuel?.Id, fuelCost = fuelCost
                });
                Save(state);
                GameNotifications.Post(GameFoundation.Localization.GameText.Get("Производство началось"), NotificationKind.Positive, output.resourceIcon);
                return true;
            }
            finally { mutating = false; }
        }

        private static int CurrentDay => DayCycleService.Instance?.Day ?? 1;
        private static bool AvailableToday(Workshop workshop) =>
            workshop.orders.Count == 0;

        internal static bool Cancel(int shop)
        {
            if (mutating) return false;
            var state = Load();
            var workshop = state.shops[shop];
            if (workshop.orders.Count == 0) return false;
            var stock = GlobalResourceManager.Instance;
            if (stock == null) return false;
            var order = workshop.orders[0];
            using var batch = SaveSlotPrefs.Batch();
            using var feedback = GameNotifications.BeginAction();
            mutating = true;
            try
            {
                workshop.orders.RemoveAt(0);
                workshop.worker = false;
                Save(state);
                if (order.price > 0 && ResourceCatalog.Find(order.input) is ResourceType material)
                    stock.AddResource(material, order.price);
                if (order.fuelCost > 0 && ResourceCatalog.Find(order.fuel) is ResourceType fuel)
                    stock.AddResource(fuel, order.fuelCost);
                return true;
            }
            finally { mutating = false; }
        }

        internal static string StartBlockReason(int shop)
        {
            var workshop = Read(shop);
            if (workshop.orders.Count > 0) return GameFoundation.Localization.GameText.Get("Мастерская занята");
            if (!AvailableToday(workshop)) return GameFoundation.Localization.GameText.Get("Сегодня заказ уже запускали");
            return FreeResidents(ResourceCatalog.Find("Human")) == 0
                ? GameFoundation.Localization.GameText.Get("Нет свободных людей для этой работы") : null;
        }

        internal static void AdvanceDay()
        {
            var stock = GlobalResourceManager.Instance;
            if (stock == null) return;
            var state = Load();
            bool changed = false;
            int free = FreeResidents(ResourceCatalog.Find("Human"));
            for (int i = 0; i < state.shops.Length; i++)
            {
                var workshop = state.shops[i];
                if (workshop.orders.Count == 0 || !BuildingUpgradeService.IsBuilt(BuildingIds[i])) continue;
                var order = workshop.orders[0];
                if (order.firstWorkDay > CurrentDay) continue;
                if (!workshop.worker)
                {
                    if (free == 0) continue;
                    workshop.worker = true;
                    free--;
                }
                changed = true;
                if (--order.days > 0) continue;
                var output = ResourceCatalog.Find(order.resource);
                if (output != null)
                {
                    stock.AddResource(output, order.amount);
                    GameNotifications.Post(GameFoundation.Localization.GameText.Get("Заказ готов"), NotificationKind.Positive, output.resourceIcon);
                    GameFoundation.Audio.GameAudioController.PlayUI(GameFoundation.Audio.GameAudioCue.ProductionComplete);
                }
                workshop.orders.RemoveAt(0);
                if (workshop.orders.Count == 0) workshop.worker = false;
            }
            if (changed) Save(state);
        }

        internal static void ReconcileWorkers()
        {
            var human = ResourceCatalog.Find("Human");
            if (human == null || GlobalResourceManager.Instance == null) return;
            int residents = GlobalResourceManager.Instance.GetResourceAmount(human);
            var state = Load();
            bool changed = false;
            for (int i = 0; i < state.shops.Length; i++)
            {
                var workshop = state.shops[i];
                if (!workshop.worker) continue;
                if (residents > 0 && BuildingUpgradeService.IsBuilt(BuildingIds[i])) { residents--; continue; }
                workshop.worker = false;
                changed = true;
            }
            for (int i = 0; i < state.shops.Length && residents > 0; i++)
            {
                var workshop = state.shops[i];
                if (workshop.worker || workshop.orders.Count == 0 || !BuildingUpgradeService.IsBuilt(BuildingIds[i])) continue;
                workshop.worker = true;
                residents--;
                changed = true;
            }
            if (changed) Save(state);
        }

        internal static void Reset()
        {
            SaveSlotPrefs.DeleteKey(SaveKey);
            Changed?.Invoke();
        }

        private static void Save(State state)
        {
            SaveSlotPrefs.SetString(SaveKey, JsonUtility.ToJson(state));
            SaveSlotPrefs.Save();
            Changed?.Invoke();
        }
    }
}
