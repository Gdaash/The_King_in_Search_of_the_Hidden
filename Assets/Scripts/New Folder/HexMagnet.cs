using UnityEngine;
using System.Collections.Generic;

public class HexMagnet : MonoBehaviour
{
    // Статический список всех активных "магнитов" (открытых гексов)
    public static List<HexMagnet> ActiveInstances { get; } = new List<HexMagnet>();

    [Header("Настройки магнита")]
    [Tooltip("Радиус, при попадании в который маркер притягивается к центру")]
    [SerializeField] private float magnetRadius = 1.5f;

    private void OnEnable()
    {
        // Регистрируем магнит при включении объекта
        if (!ActiveInstances.Contains(this))
        {
            ActiveInstances.Add(this);
        }
    }

    private void OnDisable()
    {
        // Отменяем регистрацию при отключении
        ActiveInstances.Remove(this);
    }

    private void OnDestroy()
    {
        ActiveInstances.Remove(this);
    }

    /// <summary>
    /// Проверяет, находится ли точка в радиусе магнита
    /// </summary>
    public bool IsPointOverMagnet(Vector2 point)
    {
        float distance = Vector2.Distance(point, (Vector2)transform.position);
        return distance <= magnetRadius;
    }
}