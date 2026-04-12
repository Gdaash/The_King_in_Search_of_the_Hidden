using UnityEngine;

// Этот атрибут позволит создавать новые типы ресурсов через меню: 
// ПКМ -> Create -> Resources -> New Resource Type
[CreateAssetMenu(fileName = "NewResourceType", menuName = "Resources/Resource Type")]
public class ResourceType : ScriptableObject
{
    public string resourceName;
    public Sprite defaultCarrySprite; // Спрайт для отображения в руках
}
