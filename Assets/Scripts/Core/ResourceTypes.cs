using UnityEngine;

[CreateAssetMenu(fileName = "NewResourceType", menuName = "Resources/Resource Type")]
public class ResourceType : ScriptableObject
{
    [SerializeField, Tooltip("Постоянный ключ сохранений и локализации. Не меняется при переименовании ассета.")]
    private string persistentId;
    public string Id => persistentId;
    [Header("Общие настройки")]
    public string resourceName;

    [Header("Специальные настройки")]
    [Tooltip("Если отмечено, этот ресурс не носят портеры, а приходит самостоятельно (например, Люди)")]
    public bool isHumanResource = false; // <--- ДОБАВИТЬ ЭТУ СТРОКУ

    [Header("Визуал")]
    public Sprite resourceIcon;
    public Sprite defaultCarrySprite;
}
