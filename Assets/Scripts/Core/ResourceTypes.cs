using UnityEngine;

[CreateAssetMenu(fileName = "NewResourceType", menuName = "Resources/Resource Type")]
public class ResourceType : ScriptableObject
{
    [SerializeField, Tooltip("Постоянный ключ сохранений и локализации. Не меняется при переименовании ассета.")]
    private string persistentId;
    public string Id => persistentId;
    [Header("Общие настройки")]
    public string resourceName;
    public string DisplayName
    {
        get
        {
            string key = "resource." + Id + ".name";
            string value = GameFoundation.Localization.LocalizationService.Instance?.Get(key);
            return !string.IsNullOrEmpty(value) && value != key ? value : resourceName;
        }
    }

    [Header("Специальные настройки")]
    [Tooltip("Если отмечено, этот ресурс не носят портеры, а приходит самостоятельно (например, Люди)")]
    public bool isHumanResource = false; // <--- ДОБАВИТЬ ЭТУ СТРОКУ

    [Tooltip("Показывать изменения запаса в уведомлениях. Для воинов отключено: их гибель отображается отдельным событием.")]
    public bool notifyResourceChanges = true;

    [Header("Визуал")]
    [Tooltip("Иконка интерфейса без обводки. Canvas: размер спрайта × 2.")]
    public Sprite resourceIcon;
    [Tooltip("Предмет на земле с обводкой.")]
    public Sprite groundSprite;
    [Tooltip("Груз в телеге. Прозрачные поля задают положение относительно CarrySlot.")]
    public Sprite defaultCarrySprite;
}
