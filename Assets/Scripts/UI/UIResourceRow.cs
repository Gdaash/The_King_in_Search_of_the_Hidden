using UnityEngine;
using UnityEngine.UI;
using TMPro; // Убедись, что используешь TextMeshPro

public class UIResourceRow : MonoBehaviour
{
    public Image resourceIcon;
    public TextMeshProUGUI resourceNameText;
    public TextMeshProUGUI resourceCountText;

    public void UpdateRow(Sprite icon, string name, int count)
    {
        if (resourceIcon != null) resourceIcon.sprite = icon;
        if (resourceNameText != null) resourceNameText.text = name;
        if (resourceCountText != null) resourceCountText.text = count.ToString();
    }
}