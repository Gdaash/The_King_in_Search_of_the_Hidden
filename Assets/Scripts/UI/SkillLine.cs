using UnityEngine;
using UnityEngine.UI;

public class SkillLine : MonoBehaviour
{
    [SerializeField] private SkillButton parentSkill; // Кнопка, от которой идет линия
    [SerializeField] private Image lineImage;

    [Header("Цвета линии")]
    public Color lockedColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
    public Color unlockedColor = Color.white;
    public Color purchasedColor = Color.yellow;

    private void Start()
    {
        if (lineImage == null) lineImage = GetComponent<Image>();
        UpdateLineColor();
    }

    private void Update()
    {
        // Для динамического обновления (можно перенести в события для оптимизации)
        UpdateLineColor();
    }

    private void UpdateLineColor()
    {
        if (parentSkill == null || lineImage == null) return;

        if (parentSkill.isPurchased)
            lineImage.color = purchasedColor;
        else if (parentSkill.isUnlocked)
            lineImage.color = unlockedColor;
        else
            lineImage.color = lockedColor;
    }
}
