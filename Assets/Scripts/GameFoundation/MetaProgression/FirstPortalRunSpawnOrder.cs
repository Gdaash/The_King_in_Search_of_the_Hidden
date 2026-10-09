using UnityEngine;

namespace GameFoundation.MetaProgression
{
    [DisallowMultipleComponent]
    public sealed class FirstPortalRunSpawnOrder : MonoBehaviour
    {
        [Header("Порядок открытия стартовых гексов")]
        [Tooltip("Первый элемент — первое открытие. Пустая ссылка — пустой гекс. После списка все гексы пустые.")]
        [SerializeField] private GameObject[] buildings;

        public GameObject ContentAt(int openingIndex) => buildings != null &&
            openingIndex >= 0 && openingIndex < buildings.Length ? buildings[openingIndex] : null;
    }
}
