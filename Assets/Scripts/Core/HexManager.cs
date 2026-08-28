using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

[DefaultExecutionOrder(-100)]
public class HexManager : MonoBehaviour
{
    [System.Serializable]
    public class HexPrefabData
    {
        public string contentID; 
        public GameObject prefab;
        public bool isMandatory; 
        public bool autoUnlockHex; 
        public bool startLocked; 
    }

    [System.Serializable]
    public class HexGroupSettings
    {
        public int groupID;
        public List<HexPrefabData> prefabsForGroup;
    }

    [Header("Глобальные настройки")]
    [SerializeField] private GlobalStats globalHexStats;

    [Header("Настройки групп префабов")]
    [SerializeField] private List<HexGroupSettings> groups;

    [Header("Настройки анимации появления (бамп)")]
    [SerializeField] private float bumpScaleUp = 1.1f;
    [SerializeField] private float bumpScaleDown = 0.95f;
    [SerializeField] private float bumpDurationPerPhase = 0.12f;

    private void Awake()
    {
        if (globalHexStats != null) 
        {
            globalHexStats.LoadStats();
        }
        AssignHexContents();
    }

    private void Start() { }

    private void AssignHexContents()
    {
        HexBlocker[] allHexes = Object.FindObjectsByType<HexBlocker>(FindObjectsSortMode.None);
        var groupedHexes = allHexes.GroupBy(h => h.groupID);

        foreach (var group in groupedHexes)
        {
            int currentID = group.Key;
            List<HexBlocker> hexesInGroup = group.ToList();
            HexGroupSettings settings = groups.Find(g => g.groupID == currentID);
            
            if (settings != null && settings.prefabsForGroup.Count > 0)
            {
                List<HexPrefabData> spawnPool = PrepareSpawnPool(settings.prefabsForGroup, hexesInGroup.Count);
                ShuffleList(hexesInGroup);

                for (int i = 0; i < hexesInGroup.Count; i++)
                {
                    if (i >= spawnPool.Count) break;

                    HexPrefabData selectedData = spawnPool[i];
                    GameObject chosenPrefab = selectedData.prefab;

                    int dangerLevel = 0;
                    if (chosenPrefab != null)
                    {
                        DangerSource dangerSourceRoot = chosenPrefab.GetComponent<DangerSource>();
                        DangerSource dangerSourceChild = chosenPrefab.GetComponentInChildren<DangerSource>(true);
                        
                        if (dangerSourceRoot != null)
                        {
                            dangerLevel = dangerSourceRoot.dangerLevel;
                        }
                        else if (dangerSourceChild != null)
                        {
                            dangerLevel = dangerSourceChild.dangerLevel;
                        }
                    }

                    hexesInGroup[i].AssignContent(chosenPrefab, dangerLevel, selectedData.autoUnlockHex);
                }
            }
        }
    }

    public void PlayBumpAnimation(GameObject spawnedObject)
    {
        if (spawnedObject == null) return;

        Transform tr = spawnedObject.transform;
        DOTween.Kill(tr);

        Vector3 baseScale = tr.localScale;
        Sequence seq = DOTween.Sequence();
        
        seq.Append(tr.DOScale(baseScale * bumpScaleUp, bumpDurationPerPhase).SetEase(Ease.OutQuad));
        seq.Append(tr.DOScale(baseScale * bumpScaleDown, bumpDurationPerPhase).SetEase(Ease.InOutQuad));
        seq.Append(tr.DOScale(baseScale, bumpDurationPerPhase).SetEase(Ease.OutQuad));
    }

    private List<HexPrefabData> PrepareSpawnPool(List<HexPrefabData> dataList, int hexCount)
    {
        List<HexPrefabData> pool = new List<HexPrefabData>();

        var allowedData = dataList.Where(d => 
        {
            if (d.startLocked)
            {
                return globalHexStats != null && globalHexStats.unlockedHexContentIDs.Contains(d.contentID);
            }
            return true;
        }).ToList();

        var mandatory = allowedData.Where(d => d.isMandatory).ToList();
        pool.AddRange(mandatory);

        var optional = allowedData.Where(d => !d.isMandatory).ToList();
        ShuffleList(optional);

        int remainingSlots = hexCount - pool.Count;
        for (int i = 0; i < remainingSlots && i < optional.Count; i++)
        {
            pool.Add(optional[i]);
        }

        return pool;
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int rnd = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[rnd];
            list[rnd] = temp;
        }
    }
}