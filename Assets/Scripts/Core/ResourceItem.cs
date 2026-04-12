using UnityEngine;

public class ResourceItem : MonoBehaviour
{
    public ResourceType type;
    public Sprite carrySprite;
    public float weight = 0.2f;

    [HideInInspector] public bool isReserved = false; // Кем-то уже выбран как цель
}