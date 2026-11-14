using PlayerPrefs = GameFoundation.Saves.SaveSlotPrefs;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameFoundation.MetaProgression
{
    [Serializable]
    public class PortalSite
    {
        public string locationId;
        public int difficulty;
        public bool active;
        [HideInInspector] public float x;
        [HideInInspector] public float y;
    }
    [DefaultExecutionOrder(-100)]
    public sealed class DayCycleService : MonoBehaviour
    {
        [SerializeField] private GameFoundation.Base.ForestForagingSettings forestForaging;
        private void Update()
        {
            GameFoundation.Base.ForestForagingService.CompleteIfReady(forestForaging);
            GameFoundation.Base.ForagingInputLock.SetLocked(GameFoundation.Base.ForestForagingService.IsPending);
        }
        public readonly struct FoodForecast
        {
            public readonly int Humans;
            public readonly int Swordsmen;
            public readonly int Archers;
            public readonly int Spearmen;
            public readonly int Slingers;
            public readonly int Residents;
            public readonly int Berries;
            public readonly int Crowns;
            public int Food => Berries;
            public int Starving => Mathf.Max(0, Residents - Food);
            public int CrownDelta => Starving > 0 ? (Crowns > 0 ? -1 : 0) : 1;

            public FoodForecast(int humans, int swordsmen, int archers, int berries, int crowns, int spearmen = 0, int slingers = 0, int otherWarriors = 0)
            {
                Humans = humans;
                Swordsmen = swordsmen;
                Archers = archers;
                Spearmen = spearmen;
                Slingers = slingers;
                Residents = humans + swordsmen + archers + spearmen + slingers + otherWarriors;
                Berries = berries;
                Crowns = crowns;
            }
        }
        public static DayCycleService Instance { get; private set; }
        public static int CurrentPortalDifficulty => Instance != null ? Instance.SelectedPortalDifficulty : 1;
        public static string CurrentPortalLocationId => Instance != null ? Instance.SelectedPortalLocationId : "forest";
        public int Day { get; private set; } = 1; public bool SearchedToday { get; private set; } public bool EnteredToday { get; private set; }
        public int SelectedPortalDifficulty { get; private set; } = 1;
        public string SelectedPortalLocationId { get; private set; } = "forest";
        public int RefugeesAvailable { get; private set; } = 2;
        public List<PortalSite> Portals { get; private set; } = new(); public event Action Changed;
        public const int MaxPortals = 6;
        private const string Key="foundation.daycycle";
        private int totalPortalEntries;
        private bool firstPortalRun;
        private bool magicOreOpened;
        public static bool NeedsThirdRunMagicOre => Instance != null && Instance.totalPortalEntries == 3 && !Instance.magicOreOpened;
        // After the third expedition, repeat help every other expedition until ore is opened.
        public static int GuaranteedMagicOreRing => Instance == null ? 0 :
            MagicOreHelpRing(Instance.totalPortalEntries, Instance.magicOreOpened);
        public static int MagicOreHelpRing(int portalEntries, bool oreOpened) => oreOpened ? 0 :
            portalEntries == 3 ? 2 : portalEntries >= 4 && portalEntries % 2 == 0 ? 3 : 0;
        public static void RecordMagicOreOpened()
        {
            if (Instance == null || Instance.magicOreOpened) return;
            Instance.magicOreOpened = true;
            Instance.Save();
        }
        public static bool IsFirstPortalRun => !PlaytestSettings.TutorialsAndQuestsDisabled && (Instance != null ? Instance.firstPortalRun || Instance.totalPortalEntries == 0 :
            (JsonUtility.FromJson<Data>(PlayerPrefs.GetString(Key, "{}")) ?? new Data()).totalPortalEntries == 0);
        private void OnEnable()
        {
            // Awake does not run again after a Play Mode script reload.
            if (Instance == null) Instance = this;
            PlayerPrefs.SelectionChanged -= ReloadSelectedSlot;
            PlayerPrefs.SelectionChanged += ReloadSelectedSlot;
        }
        private void OnDisable() => PlayerPrefs.SelectionChanged -= ReloadSelectedSlot;
        private void ReloadSelectedSlot()
        {
            if (Instance != this) return;
            // This persistent service survives the menu; an empty slot has no saved fields to overwrite it.
            Day = 1; SearchedToday = EnteredToday = false;
            totalPortalEntries = 0; firstPortalRun = magicOreOpened = false;
            RefugeesAvailable = 2; SelectedPortalDifficulty = 1; SelectedPortalLocationId = "forest";
            Portals = new List<PortalSite>();
            Load();
            EnsureFixedPortals();
            PortalProgression.MigrateLegacyAccess(Portals);
            Changed?.Invoke();
        }
        private void OnDestroy()
        {
            GameFoundation.Base.ForagingInputLock.SetLocked(false);
            if (Instance == this) Instance = null;
        }
        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
            EnsureFixedPortals();
            PortalProgression.MigrateLegacyAccess(Portals);
            Save();
        }

        public static IReadOnlyList<PortalLocationDefinition> GetPortalLocations() =>
            (ProjectReferences.Instance != null ? ProjectReferences.Instance.portalLocations : Array.Empty<PortalLocationDefinition>())
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.LocationId))
                .OrderBy(item => item.RequiredPortalLevel)
                .ToArray();

        public static PortalLocationDefinition GetPortalLocation(string locationId) =>
            GetPortalLocations().FirstOrDefault(item => item.LocationId == locationId);

        public void Search() { }

        public bool Activate(PortalSite site)
        {
            if (site == null || !Portals.Contains(site) || site.active) return false;
            PortalLocationDefinition location = GetPortalLocation(site.locationId);
            if (!PortalProgression.IsUnlocked(location)) return false;
            site.active = true;
            Save();
            Changed?.Invoke();
            return true;
        }

        public const int MinimumExpeditionResidents = 2;
        public bool HasExpeditionResidents =>
            GameFoundation.Base.CraftProduction.FreeResidents(ResourceCatalog.Find("Human")) >= MinimumExpeditionResidents;

        public bool Enter(PortalSite site)
        {
            if (GameFoundation.Base.ForestForagingService.IsPending) return false;
            if (site == null || !Portals.Contains(site) || EnteredToday || !HasExpeditionResidents) return false;
            PortalLocationDefinition location = GetPortalLocation(site.locationId);
            if (!PortalProgression.IsUnlocked(location)) return false;
            PlayerPrefs.BeginRunCheckpoint();
            site.active = true;
            EnteredToday = true;
            firstPortalRun = totalPortalEntries == 0;
            totalPortalEntries++;
            SelectedPortalLocationId = location.LocationId;
            SelectedPortalDifficulty = location.Difficulty;
            Save();
            Changed?.Invoke();
            return true;
        }

        public void DeactivateSelectedPortal()
        {
            firstPortalRun = false;
            PortalSite selected = Portals.FirstOrDefault(item => item.locationId == SelectedPortalLocationId);
            if (selected == null || !selected.active) return;
            selected.active = false;
            Save();
            Changed?.Invoke();
        }
        /// <summary>Debug calendar shift shared by every day-based quest; skips the daily economy.</summary>
        public void ShiftQuestDays(int delta)
        {
            int target = (int)Math.Max(1L,Math.Min(int.MaxValue,(long)Day+delta));
            if (target == Day) return;
            Day = target; Save(); Changed?.Invoke();
            GameFoundation.Quests.QuestProgress.NotifyChanged();
        }
        public void NextDay()
        {
            if (GameFoundation.Base.ForestForagingService.IsPending) return;
            using var save = GameFoundation.Saves.SaveSlotPrefs.Batch();
            DayResourceLedger.EnterShelter();
            GameFoundation.Base.HousingDistrict.Produce();
            int starved = ConsumeFood();
            ApplyColdDeaths();
            GameFoundation.Base.CraftProduction.ReconcileWorkers();
            MilitaryExperienceService.HealAll();
            UpdateCrowns(starved);
            DayResourceLedger.FinishDay(Day);
            Day++;
            SearchedToday = false;
            EnteredToday = false;
            firstPortalRun = false;
            RefugeesAvailable = PlayerPrefs.GetInt("foundation.refugees.firstArrival", 0) == 0 ? 2 : UnityEngine.Random.Range(1, 4);
            Save();
            DayResourceLedger.StartNextDay(Day);
            GameFoundation.Base.BuildingUpgradeService.NotifyChanged();
            Changed?.Invoke();
        }
        // Only a settled expedition advances workshop time. The marker is part of
        // the run checkpoint, so abandoning a run cannot consume a working day.
        public void CompleteProductionDay()
        {
            const string key = "foundation.craft.completedDay";
            if (!EnteredToday || PlayerPrefs.GetInt(key, 0) >= Day) return;
            using var save = PlayerPrefs.Batch();
            PlayerPrefs.SetInt(key, Day);
            ResolvingProduction = true;
            try { GameFoundation.Base.CraftProduction.AdvanceDay(); }
            finally { ResolvingProduction = false; }
            PlayerPrefs.Save();
        }
        internal bool ResolvingProduction { get; private set; }
        public bool AdmitRefugee(){if(RefugeesAvailable<=0 || !GameFoundation.Base.BuildingUpgradeService.CanAdmitResident)return false;RefugeesAvailable--;Save();Changed?.Invoke();return true;}
        public void EnsureFirstRefugees()
        {
            if (PlayerPrefs.GetInt("foundation.refugees.firstArrival", 0) != 0) return;
            RefugeesAvailable = 2;
            PlayerPrefs.SetInt("foundation.refugees.firstArrival", 1);
            GameFoundation.Audio.GameAudioController.PlayUI(GameFoundation.Audio.GameAudioCue.RefugeesArrival, .7f);
            Save(); Changed?.Invoke();
        }
        public FoodForecast GetFoodForecast(bool includeProduction = true)
        {
            var resources = GlobalResourceManager.Instance;
            if (resources == null) return new FoodForecast(0, 0, 0, 0, 0);
            var humans = Find("Human");
            var swordsmen = Find("Swordsman");
            var archers = Find("Archer");
            var berries = Find("Berry");
            var crowns = Find("Crown");
            return new FoodForecast(
                humans == null ? 0 : resources.GetResourceAmount(humans),
                swordsmen == null ? 0 : resources.GetResourceAmount(swordsmen),
                archers == null ? 0 : resources.GetResourceAmount(archers),
                berries == null ? 0 : resources.GetResourceAmount(berries) +
                    (includeProduction ? GameFoundation.Base.HousingDistrict.Forecast().Food : 0),
                crowns == null ? 0 : resources.GetResourceAmount(crowns),
                Find("Spearman") is ResourceType spearmen ? resources.GetResourceAmount(spearmen) : 0,
                Find("Slinger") is ResourceType slingers ? resources.GetResourceAmount(slingers) : 0,
                ResourceCatalog.All.Where(r => r.isMilitaryUnit && r.Id != "Swordsman" && r.Id != "Archer" && r.Id != "Spearman" && r.Id != "Slinger").Sum(resources.GetResourceAmount));
        }

        private int ConsumeFood()
        {
            var resources = GlobalResourceManager.Instance;
            var humans = Find("Human");
            if (resources == null || humans == null) return 0;
            var forecast = GetFoodForecast(false);
            var need = forecast.Residents;
            var berries = Find("Berry");
            if (berries != null)
            {
                var take = Mathf.Min(need, forecast.Berries);
                if (take > 0 && resources.TrySpendResource(berries, take)) need -= take;
            }
            if (need > 0)
            {
                int deaths = KillRandomResidents(resources, forecast, need);
                DayResourceLedger.RecordStarvation(deaths);
                return deaths;
            }
            return 0;
        }

        private void ApplyColdDeaths()
        {
            var resources = GlobalResourceManager.Instance;
            var humans = Find("Human");
            if (resources == null || humans == null) return;
            int homeless = Mathf.Max(0, resources.GetResourceAmount(humans) - GameFoundation.Base.BuildingUpgradeService.Capacity("housing"));
            int deaths = 0;
            for (int i = 0; i < homeless; i++)
                if (UnityEngine.Random.value < .5f) deaths++;
            if (deaths == 0) return;
            resources.TrySpendResource(humans, deaths);
            DayResourceLedger.RecordColdDeaths(deaths);
        }

        private static void UpdateCrowns(int starvationDeaths)
        {
            GlobalResourceManager resources = GlobalResourceManager.Instance;
            ResourceType crowns = Find("Crown");
            if (resources == null || crowns == null) return;
            if (starvationDeaths > 0)
                resources.TrySpendResource(crowns, 1);
            else
                resources.AddResource(crowns, 1);
        }

        private static int KillRandomResidents(GlobalResourceManager resources, FoodForecast forecast, int requestedDeaths)
        {
            var population = ResourceCatalog.All.Where(r => r.isHumanResource || r.isMilitaryUnit)
                .Select(r => (Type: r, Count: resources.GetResourceAmount(r))).Where(r => r.Count > 0).ToList();
            int deaths = Mathf.Min(requestedDeaths, population.Sum(r => r.Count));
            for (int i = 0; i < deaths; i++)
            {
                int roll = UnityEngine.Random.Range(0, population.Sum(r => r.Count));
                for (int j = 0; j < population.Count; j++)
                {
                    var entry = population[j];
                    if (roll >= entry.Count) { roll -= entry.Count; continue; }
                    resources.TrySpendResource(entry.Type, 1);
                    population[j] = (entry.Type, entry.Count - 1);
                    if (entry.Type.isMilitaryUnit)
                    {
                        MilitaryExperienceService.RemoveStored(entry.Type);
                        string weapon = entry.Type.Id switch { "Swordsman" => "Sword", "Archer" => "Bow", "Spearman" => "Spear", "Crossbowman" => "Crossbow", _ => null };
                        if (weapon != null) resources.AddResource(Find(weapon), 1);
                        string armor = entry.Type.Id switch { "Swordsman" or "Crossbowman" => "IronArmor", "Archer" => "LeatherArmor", _ => null };
                        if (armor != null) resources.AddResource(Find(armor), 1);
                    }
                    break;
                }
            }
            return deaths;
        }

        private static ResourceType Find(string name) => ResourceCatalog.Find(name);
        private void EnsureFixedPortals()
        {
            IReadOnlyList<PortalLocationDefinition> locations = GetPortalLocations();
            if (locations.Count == 0) return;
            List<PortalSite> previous = Portals ?? new List<PortalSite>();
            var fixedPortals = new List<PortalSite>(locations.Count);
            foreach (PortalLocationDefinition location in locations)
            {
                PortalSite saved = previous.FirstOrDefault(item => item.locationId == location.LocationId);
                if (saved == null)
                    saved = previous.FirstOrDefault(item => string.IsNullOrEmpty(item.locationId) && item.difficulty == location.Difficulty);
                fixedPortals.Add(new PortalSite
                {
                    locationId = location.LocationId,
                    difficulty = location.Difficulty,
                    active = saved != null && saved.active
                });
            }
            Portals = fixedPortals;
            SearchedToday = true;
        }

        private void Save(){PlayerPrefs.SetString(Key,JsonUtility.ToJson(new Data{magicOreOpened=magicOreOpened,totalPortalEntries=totalPortalEntries,firstPortalRun=firstPortalRun,day=Day,searched=SearchedToday,entered=EnteredToday,refugees=RefugeesAvailable,selectedDifficulty=SelectedPortalDifficulty,selectedLocationId=SelectedPortalLocationId,portals=Portals}));PlayerPrefs.Save();}
        private void Load(){if(!PlayerPrefs.HasKey(Key))return;var json=PlayerPrefs.GetString(Key);var d=JsonUtility.FromJson<Data>(json);if(d==null)return;totalPortalEntries=json.Contains("\"totalPortalEntries\"")?d.totalPortalEntries:(d.day>1||d.entered?1:0);magicOreOpened=d.magicOreOpened;firstPortalRun=d.firstPortalRun;Day=Mathf.Max(1,d.day);SearchedToday=d.searched;EnteredToday=d.entered;RefugeesAvailable=json.Contains("\"refugees\"")?d.refugees:2;SelectedPortalDifficulty=Mathf.Clamp(d.selectedDifficulty<=0?1:d.selectedDifficulty,1,MaxPortals);SelectedPortalLocationId=string.IsNullOrEmpty(d.selectedLocationId)?"forest":d.selectedLocationId;Portals=d.portals??new();}
        [Serializable] private class Data{public bool magicOreOpened;public int totalPortalEntries;public bool firstPortalRun;public int day=1;public bool searched,entered;public int refugees=2;public int selectedDifficulty=1;public string selectedLocationId="forest";public List<PortalSite> portals;}
    }
}


