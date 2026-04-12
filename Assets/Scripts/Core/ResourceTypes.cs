using UnityEngine;

[CreateAssetMenu(fileName = "NewResourceType", menuName = "Resources/Resource Type")]
public class ResourceType : ScriptableObject
{
    [Header("Общие настройки")]
    public string resourceName; // Название, которое увидит игрок

    [Header("Визуал")]
    public Sprite resourceIcon;      // Для UI панели ресурсов (автоматически!)
    public Sprite defaultCarrySprite; // Для рук носильщика
}
