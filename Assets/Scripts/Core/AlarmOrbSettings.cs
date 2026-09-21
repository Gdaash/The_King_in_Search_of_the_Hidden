using UnityEngine;
using UnityEngine.UI;

/// <summary>Визуальные параметры одного шарика тревоги. Настраиваются на префабе AlarmOrb.</summary>
public class AlarmOrbSettings : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField, Min(4f)] private float size = 36f;
    [SerializeField, Min(0.2f)] private float totalDuration = 1.7f;
    [SerializeField, Min(0.05f)] private float spreadDuration = 0.35f;
    [SerializeField, Min(0f)] private float launchDelayMax = 0.12f;
    [SerializeField, Min(0f)] private float clusterRadius = 28f;
    [SerializeField, Min(4f)] private float arcHeight = 80f;
    [Tooltip("Сколько единиц тревоги представляет один шарик.")]
    [SerializeField, Min(0.01f)] private float alarmUnitsPerOrb = 1f;
    [SerializeField, Min(1)] private int maximumOrbsPerAddition = 50;

    public Image Image => image != null ? image : GetComponent<Image>();
    public float Size => size;
    public float TotalDuration => totalDuration;
    public float SpreadDuration => spreadDuration;
    public float LaunchDelayMax => launchDelayMax;
    public float ClusterRadius => clusterRadius;
    public float ArcHeight => arcHeight;
    public float AlarmUnitsPerOrb => alarmUnitsPerOrb;
    public int MaximumOrbsPerAddition => maximumOrbsPerAddition;
}
