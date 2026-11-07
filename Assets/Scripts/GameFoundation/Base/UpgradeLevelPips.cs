using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    public sealed class UpgradeLevelPips : MonoBehaviour
    {
        [SerializeField] private Image[] squares = System.Array.Empty<Image>();
        public void Display(int current, int maximum)
        {
            if (squares.Length != maximum)
            {
                foreach (var square in squares) if (square != null) { square.gameObject.SetActive(false); if (Application.isPlaying) Destroy(square.gameObject); else DestroyImmediate(square.gameObject); }
                squares = new Image[Mathf.Max(0, maximum)];
                for (int i = 0; i < squares.Length; i++)
                {
                    var rect = new GameObject("Level " + (i + 1), typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    rect.SetParent(transform, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, .5f);
                    rect.anchoredPosition = new Vector2(i * 16, 0); rect.sizeDelta = new Vector2(12,12);
                    squares[i] = rect.GetComponent<Image>(); squares[i].raycastTarget = false;
                }
            }
            for (int i = 0; i < squares.Length; i++) squares[i].color = i < current ? new Color(.92f,.83f,.60f) : new Color(.42f,.36f,.48f);
        }
    }
}
