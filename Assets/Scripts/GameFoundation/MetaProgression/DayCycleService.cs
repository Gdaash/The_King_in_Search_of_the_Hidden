using PlayerPrefs = GameFoundation.Saves.SaveSlotPrefs;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFoundation.MetaProgression
{
    [Serializable] public class PortalSite { public int difficulty; public float x; public float y; public bool active; }
    public sealed class DayCycleService : MonoBehaviour
    {
        public readonly struct FoodForecast
        {
            public readonly int Humans;
            public readonly int Swordsmen;
            public readonly int Archers;
            public readonly int Residents;
            public readonly int Berries;
            public int Food => Berries;
            public int Starving => Mathf.Max(0, Residents - Food);

            public FoodForecast(int humans, int swordsmen, int archers, int berries)
            {
                Humans = humans;
                Swordsmen = swordsmen;
                Archers = archers;
                Residents = humans + swordsmen + archers;
                Berries = berries;
            }
        }
        public static DayCycleService Instance { get; private set; }
        public static int CurrentPortalDifficulty => Instance != null ? Instance.SelectedPortalDifficulty : 1;
        public int Day { get; private set; } = 1; public bool SearchedToday { get; private set; } public bool EnteredToday { get; private set; }
        public int SelectedPortalDifficulty { get; private set; } = 1;
        public int RefugeesAvailable { get; private set; } = 2;
        public List<PortalSite> Portals { get; private set; } = new(); public event Action Changed;
        public const int MaxPortals = 5;
        private const string Key="foundation.daycycle";
        private void Awake(){if(Instance!=null){Destroy(gameObject);return;}Instance=this;DontDestroyOnLoad(gameObject);Load();}
        public void Search(){if(SearchedToday||Portals.Count>=MaxPortals)return;SearchedToday=true;int count=Mathf.Min(UnityEngine.Random.Range(3,6),MaxPortals-Portals.Count);for(int i=0;i<count;i++)Portals.Add(new PortalSite{difficulty=UnityEngine.Random.Range(1,6),x=UnityEngine.Random.Range(.12f,.88f),y=UnityEngine.Random.Range(.18f,.82f)});Save();Changed?.Invoke();}
        public bool Activate(PortalSite site){if(site==null||site.active)return false;var ore=Find("MagicOre");var cost=site.difficulty*5;if(ore==null||GlobalResourceManager.Instance==null||!GlobalResourceManager.Instance.TrySpendResource(ore,cost))return false;site.active=true;Save();Changed?.Invoke();return true;}
        public bool Enter(PortalSite site){if(site==null||!site.active||EnteredToday)return false;EnteredToday=true;SelectedPortalDifficulty=Mathf.Clamp(site.difficulty,1,5);Save();Changed?.Invoke();return true;}
        public void NextDay(){DayResourceLedger.EnsureDay(Day);ConsumeFood();DayResourceLedger.FinishDay(Day);Day++;SearchedToday=false;EnteredToday=false;RefugeesAvailable=UnityEngine.Random.Range(1,4);Save();DayResourceLedger.StartNextDay(Day);Changed?.Invoke();}
        public bool AdmitRefugee(){if(RefugeesAvailable<=0)return false;RefugeesAvailable--;Save();Changed?.Invoke();return true;}
        public FoodForecast GetFoodForecast()
        {
            var resources = GlobalResourceManager.Instance;
            if (resources == null) return new FoodForecast(0, 0, 0, 0);
            var humans = Find("Human");
            var swordsmen = Find("Swordsman");
            var archers = Find("Archer");
            var berries = Find("Berry");
            return new FoodForecast(
                humans == null ? 0 : resources.GetResourceAmount(humans),
                swordsmen == null ? 0 : resources.GetResourceAmount(swordsmen),
                archers == null ? 0 : resources.GetResourceAmount(archers),
                berries == null ? 0 : resources.GetResourceAmount(berries));
        }

        private void ConsumeFood()
        {
            var resources = GlobalResourceManager.Instance;
            var humans = Find("Human");
            if (resources == null || humans == null) return;
            var forecast = GetFoodForecast();
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
            }
        }

        private static int KillRandomResidents(GlobalResourceManager resources, FoodForecast forecast, int requestedDeaths)
        {
            ResourceType humans = Find("Human");
            ResourceType swordsmen = Find("Swordsman");
            ResourceType archers = Find("Archer");
            ResourceType swords = Find("Sword");
            ResourceType bows = Find("Bow");

            int humanCount = forecast.Humans;
            int swordsmanCount = forecast.Swordsmen;
            int archerCount = forecast.Archers;
            int humanDeaths = 0;
            int swordsmanDeaths = 0;
            int archerDeaths = 0;
            int deaths = Mathf.Min(requestedDeaths, humanCount + swordsmanCount + archerCount);

            for (int i = 0; i < deaths; i++)
            {
                int roll = UnityEngine.Random.Range(0, humanCount + swordsmanCount + archerCount);
                if (roll < humanCount)
                {
                    humanCount--;
                    humanDeaths++;
                }
                else if (roll < humanCount + swordsmanCount)
                {
                    swordsmanCount--;
                    swordsmanDeaths++;
                }
                else
                {
                    archerCount--;
                    archerDeaths++;
                }
            }

            if (humanDeaths > 0) resources.TrySpendResource(humans, humanDeaths);
            if (swordsmanDeaths > 0)
            {
                resources.TrySpendResource(swordsmen, swordsmanDeaths);
                if (swords != null) resources.AddResource(swords, swordsmanDeaths);
            }
            if (archerDeaths > 0)
            {
                resources.TrySpendResource(archers, archerDeaths);
                if (bows != null) resources.AddResource(bows, archerDeaths);
            }
            return deaths;
        }

        private static ResourceType Find(string name)
        {
            foreach (var type in Resources.FindObjectsOfTypeAll<ResourceType>())
                if (type.name == name || type.resourceName == name) return type;
            return null;
        }
        private void Save(){PlayerPrefs.SetString(Key,JsonUtility.ToJson(new Data{day=Day,searched=SearchedToday,entered=EnteredToday,refugees=RefugeesAvailable,selectedDifficulty=SelectedPortalDifficulty,portals=Portals}));PlayerPrefs.Save();}
        private void Load(){if(!PlayerPrefs.HasKey(Key))return;var json=PlayerPrefs.GetString(Key);var d=JsonUtility.FromJson<Data>(json);if(d==null)return;Day=d.day;SearchedToday=d.searched;EnteredToday=d.entered;RefugeesAvailable=json.Contains("\"refugees\"")?d.refugees:2;SelectedPortalDifficulty=Mathf.Clamp(d.selectedDifficulty<=0?1:d.selectedDifficulty,1,5);Portals=d.portals??new();}
        [Serializable] private class Data{public int day;public bool searched,entered;public int refugees=2;public int selectedDifficulty=1;public List<PortalSite> portals;}
    }
}


