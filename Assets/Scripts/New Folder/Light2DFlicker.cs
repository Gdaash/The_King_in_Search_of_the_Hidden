using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Создаёт эффект «живого» 2D-источника света:
/// дрожание позиции по X/Y и пульсация outer-radius
/// на основе многооктавного шума Перлина.
/// </summary>
[RequireComponent(typeof(Light2D))]
public class Light2DFlicker : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Настройки дрожания позиции
    // ─────────────────────────────────────────────
    [Header("Position Jitter")]
    [Tooltip("Амплитуда дрожания по X (в мировых единицах)")]
    [SerializeField] private float positionJitterX = 0.03f;

    [Tooltip("Амплитуда дрожания по Y (в мировых единицах)")]
    [SerializeField] private float positionJitterY = 0.03f;

    // ─────────────────────────────────────────────
    //  Настройки дрожания радиуса
    // ─────────────────────────────────────────────
    [Header("Radius Flicker")]
    [Tooltip("Амплитуда пульсации outer-radius (в мировых единицах)")]
    [SerializeField] private float radiusFlickerAmount = 0.15f;

    // ─────────────────────────────────────────────
    //  Общие параметры шума
    // ─────────────────────────────────────────────
    [Header("Noise")]
    [Tooltip("Общая скорость мерцания")]
    [SerializeField] private float speed = 3f;

    [Tooltip("Количество октав шума (больше = детальнее, но дороже)")]
    [Range(1, 5)]
    [SerializeField] private int octaves = 3;

    [Tooltip("Уменьшение амплитуды каждой следующей октавы")]
    [Range(0.1f, 1f)]
    [SerializeField] private float persistence = 0.5f;

    [Tooltip("Увеличение частоты каждой следующей октавы")]
    [Range(1f, 4f)]
    [SerializeField] private float lacunarity = 2f;

    // ─────────────────────────────────────────────
    //  Опционально: случайное затухание / всплески
    // ─────────────────────────────────────────────
    [Header("Random Bursts (optional)")]
    [Tooltip("Вероятность резкого всплеска в кадре (0 = выкл.)")]
    [Range(0f, 0.05f)]
    [SerializeField] private float burstChance = 0.005f;

    [Tooltip("Множитель амплитуды во время всплеска")]
    [SerializeField] private float burstMultiplier = 3f;

    [Tooltip("Длительность всплеска (секунды)")]
    [SerializeField] private float burstDuration = 0.12f;

    // ─────────────────────────────────────────────
    //  Внутреннее состояние
    // ─────────────────────────────────────────────
    private Light2D _light;
    private Vector3 _originLocalPos;
    private float   _baseOuterRadius;

    // Уникальные смещения для каждой оси,
    // чтобы X, Y и Radius не двигались синхронно
    private float _seedX;
    private float _seedY;
    private float _seedR;

    // Состояние всплеска
    private float _burstTimer;
    private float _currentBurstMul = 1f;

    // ─────────────────────────────────────────────

    private void Awake()
    {
        _light = GetComponent<Light2D>();
    }

    private void Start()
    {
        _originLocalPos  = transform.localPosition;
        _baseOuterRadius = _light.pointLightOuterRadius;

        // Случайные семена, чтобы каждый экземпляр мерцал по-своему
        _seedX = Random.Range(0f, 1000f);
        _seedY = Random.Range(0f, 1000f);
        _seedR = Random.Range(0f, 1000f);
    }

    private void Update()
    {
        float t = Time.time * speed;

        // ── Всплеск ──
        UpdateBurst();

        // ── Шум для каждой оси ──
        float noiseX = FractalNoise(t, _seedX) * positionJitterX * _currentBurstMul;
        float noiseY = FractalNoise(t, _seedY) * positionJitterY * _currentBurstMul;
        float noiseR = FractalNoise(t, _seedR) * radiusFlickerAmount * _currentBurstMul;

        // ── Применяем ──
        transform.localPosition = _originLocalPos + new Vector3(noiseX, noiseY, 0f);

        float newRadius = Mathf.Max(0.01f, _baseOuterRadius + noiseR);
        _light.pointLightOuterRadius = newRadius;
    }

    // ─────────────────────────────────────────────
    //  Многооктавный (фрактальный) шум Перлина
    //  Возвращает значение примерно в диапазоне [-1, 1]
    // ─────────────────────────────────────────────
    private float FractalNoise(float t, float seed)
    {
        float value     = 0f;
        float amplitude = 1f;
        float frequency = 1f;
        float maxAmp    = 0f;

        for (int i = 0; i < octaves; i++)
        {
            // Mathf.PerlinNoise возвращает [0, 1] → сдвигаем в [-1, 1]
            value  += (Mathf.PerlinNoise(seed + t * frequency, seed * 0.731f) * 2f - 1f) * amplitude;
            maxAmp += amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return value / maxAmp;   // нормализация
    }

    // ─────────────────────────────────────────────
    //  Логика случайных всплесков
    // ─────────────────────────────────────────────
    private void UpdateBurst()
    {
        if (_burstTimer > 0f)
        {
            _burstTimer -= Time.deltaTime;
            // Плавное затухание всплеска
            _currentBurstMul = Mathf.Lerp(1f, burstMultiplier,
                                          Mathf.Clamp01(_burstTimer / burstDuration));
        }
        else
        {
            _currentBurstMul = 1f;

            if (burstChance > 0f && Random.value < burstChance)
            {
                _burstTimer = burstDuration;
            }
        }
    }

    // ─────────────────────────────────────────────
    //  Публичные методы для runtime-настройки
    // ─────────────────────────────────────────────

    /// <summary>Перезапоминает текущую позицию как «центр» дрожания.</summary>
    public void ResetOrigin() => _originLocalPos = transform.localPosition;

    /// <summary>Перезапоминает текущий outer-radius как базовый.</summary>
    public void ResetBaseRadius() => _baseOuterRadius = _light.pointLightOuterRadius;

#if UNITY_EDITOR
    // Визуализация базового радиуса в Scene-view
    private void OnDrawGizmosSelected()
    {
        if (_light == null) _light = GetComponent<Light2D>();
        Gizmos.color = new Color(1f, 0.9f, 0.3f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, _light.pointLightOuterRadius);
    }
#endif
}