using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.MetaProgression
{
    public sealed class MilitaryStarsView : MonoBehaviour
    {
        [SerializeField] private Image[] stars;
        [SerializeField] private Text overflowCount;
        [SerializeField] private bool alwaysCompact;

        public void SetStars(int count, int maximum = 10)
        {
            count = Mathf.Clamp(count, 0, maximum);
            bool compact = alwaysCompact || count > 3;
            for (int i = 0; i < stars.Length; i++) stars[i].gameObject.SetActive(compact ? i == 0 : i < count);
            if (overflowCount != null)
            {
                overflowCount.gameObject.SetActive(compact);
                overflowCount.text = compact ? count.ToString() : string.Empty;
            }
        }
    }
}
