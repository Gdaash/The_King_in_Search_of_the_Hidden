using UnityEngine;

namespace GameFoundation.MetaProgression
{
    [CreateAssetMenu(menuName = "Game/Portal/First run layout")]
    public sealed class FirstPortalRunLayout : ScriptableObject
    {
        [Tooltip("Порядок завершённых открытий гексов. Пустая ссылка означает пустой гекс. После списка все гексы пустые.")]
        [SerializeField] private GameObject[] contents;

        public GameObject ContentAt(int openingIndex) => contents != null &&
            openingIndex >= 0 && openingIndex < contents.Length ? contents[openingIndex] : null;
    }
}
