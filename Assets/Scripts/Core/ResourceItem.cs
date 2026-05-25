using UnityEngine;

public class ResourceItem : MonoBehaviour
{
    public ResourceType type;
    public Sprite carrySprite;
    public float weight = 0.2f;

    [HideInInspector] public bool isReserved = false; // Кем-то уже выбран как цель

    private void OnDisable()
    {
        // Если ресурс исчезает (удален или выключен) и он был забронирован
        if (isReserved)
        {
            NotifySystemAboutLoss();
        }
    }

    private void NotifySystemAboutLoss()
    {
        // Ищем всех носильщиков в сцене
        Porter[] allPorters = Object.FindObjectsByType<Porter>(FindObjectsSortMode.None);
        foreach (var p in allPorters)
        {
            // Если носильщик направлялся именно к этому ресурсу
            // (Важно: у носильщика _currentTarget меняется на здание ТОЛЬКО после поднятия ресурса)
            if (p.GetTarget() == this.transform && !p.IsCarryingResource())
            {
                p.ResetTask();
            }
        }
        isReserved = false;
    }
}
