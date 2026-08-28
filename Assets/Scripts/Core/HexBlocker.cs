using UnityEngine;
using UnityEngine.Events;
using TMPro;
using DG.Tweening;
using System.Collections;

public class HexBlocker : MonoBehaviour
{
    [Header("Настройки группы")]
    public int groupID; 

    [Header("Ссылки на объекты")]
    [SerializeField] private GameObject lockedVisual;   
    [SerializeField] private GameObject unlockedVisual; 
    
    [Header("Настройки опасности")]
    [SerializeField] private GameObject skullIcon; 
    [SerializeField] private TextMeshPro dangerText; 
    [SerializeField] private string dangerPrefix = "LVL "; 

    [Header("Настройки сетки")]
    [SerializeField] private float checkRadius = 1.1f; 
    [SerializeField] private LayerMask hexLayer;       

    [Header("События")]
    public UnityEvent OnHexUnlocked; 

    [Header("Настройки анимации исчезновения")]
    [SerializeField] private float destroyScale = 1.1f;
    [SerializeField] private float destroyDuration = 0.5f;

    [HideInInspector] public GameObject prefabToSpawn;
    [HideInInspector] public int assignedDangerLevel;
    [HideInInspector] public bool shouldAutoUnlock;

    private HexManager _hexManager;
    private bool _isRemoved = false;
    private bool _isCurrentlyUnlocked = false; 

    private void Awake()
    {
        _hexManager = Object.FindFirstObjectByType<HexManager>();
        
        // УБРАНО: выключение skullIcon и dangerText отсюда
        // Они будут выключены в Start() после проверки опасности
    }

    private void Start()
    {
        // Сначала гарантированно выключаем визуал опасности
        if (skullIcon != null) skullIcon.SetActive(false);
        if (dangerText != null) dangerText.gameObject.SetActive(false);
        
        if (shouldAutoUnlock)
        {
            RemoveHex(); 
            return;      
        }

        // Теперь проверяем опасность и включаем визуал если нужно
        if (assignedDangerLevel > 0)
        {
            UpdateDangerVisuals();  // Включаем череп и текст
            ForceUnlockAndStartTimer();
        }
        
        CheckStatus(true);
    }

    public void AssignContent(GameObject prefab, int dangerLevel, bool autoUnlock)
    {
        prefabToSpawn = prefab;
        assignedDangerLevel = dangerLevel;
        shouldAutoUnlock = autoUnlock;
        // УБРАНО: UpdateDangerVisuals() отсюда, так как Awake гекса может выполниться позже
    }

    private void UpdateDangerVisuals()
    {
        bool hasDanger = assignedDangerLevel > 0;

        if (skullIcon != null) 
            skullIcon.SetActive(hasDanger);

        if (dangerText != null)
        {
            dangerText.gameObject.SetActive(hasDanger);
            if (hasDanger)
            {
                dangerText.text = dangerPrefix + assignedDangerLevel.ToString();
            }
        }
    }

    private void ForceUnlockAndStartTimer()
    {
        _isCurrentlyUnlocked = true;
        if (lockedVisual != null) lockedVisual.SetActive(false);
        if (unlockedVisual != null) unlockedVisual.SetActive(true);
        OnHexUnlocked?.Invoke();

        // Ищем таймер во всех дочерних объектах (включая вложенные)
        TimerController timer = GetComponentInChildren<TimerController>(true);
        if (timer != null)
        {
            float calculatedTime = assignedDangerLevel * GlobalSettings.DifficultyTimerMultiplier;
            timer.SetDurationAndStart(calculatedTime);
        }
        else
        {
            Debug.LogWarning($"[HexBlocker] TimerController не найден в детях {gameObject.name}!");
        }
    }

    public void RemoveHex()
    {
        if (_isRemoved) return;
        _isRemoved = true;

        if (prefabToSpawn != null)
        {
            GameObject spawned = Instantiate(prefabToSpawn, transform.position, Quaternion.identity);
            
            if (_hexManager != null)
            {
                _hexManager.PlayBumpAnimation(spawned);
            }
        }

        if (TryGetComponent(out Collider2D col)) col.enabled = false;
        
        if (skullIcon != null) skullIcon.SetActive(false);
        if (dangerText != null) dangerText.gameObject.SetActive(false);

        NotifyNeighbors();
        
        AnimateAndDestroy();
    }

    private void AnimateAndDestroy()
    {
        DOTween.Kill(transform);
        
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var r in renderers) DOTween.Kill(r);

        transform.DOScale(destroyScale, destroyDuration).SetEase(Ease.OutQuad);

        foreach (var r in renderers)
        {
            r.DOFade(0f, destroyDuration).SetEase(Ease.InQuad);
        }
        
        if (dangerText != null)
        {
            DOTween.Kill(dangerText.rectTransform);
            dangerText.DOFade(0f, destroyDuration);
        }

        DOVirtual.DelayedCall(destroyDuration, () => 
        {
            gameObject.SetActive(false);
        }, false);
    }

    public void CheckStatus() => CheckStatus(false);

    public void CheckStatus(bool silent)
    {
        if (_isRemoved || !gameObject.activeInHierarchy || _isCurrentlyUnlocked) return;

        int neighborCount = 0;
        Collider2D[] neighbors = Physics2D.OverlapCircleAll(transform.position, checkRadius, hexLayer);
        
        foreach (var col in neighbors)
        {
            if (col != null && col.gameObject != this.gameObject)
            {
                neighborCount++;
            }
        }

        bool canUnlock = (neighborCount <= 4);

        if (canUnlock && !_isCurrentlyUnlocked)
        {
            _isCurrentlyUnlocked = true;
            OnHexUnlocked?.Invoke(); 

            if (!silent && lockedVisual != null && lockedVisual.activeSelf)
                StartCoroutine(AnimateUnlock());
            else if (lockedVisual != null)
                lockedVisual.SetActive(false);
            
            if (unlockedVisual != null) unlockedVisual.SetActive(true);
        }
        else if (!canUnlock)
        {
            _isCurrentlyUnlocked = false;
            if (lockedVisual != null) lockedVisual.SetActive(true);
            if (unlockedVisual != null) unlockedVisual.SetActive(false);
        }
    }

    private IEnumerator AnimateUnlock()
    {
        if (lockedVisual == null) yield break;
        Transform lockedTr = lockedVisual.transform;
        Vector3 initialScale = lockedTr.localScale;
        Vector3 targetScale = initialScale * 1.5f;
        float elapsed = 0;
        
        while (elapsed < 0.4f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.4f;
            lockedTr.localScale = Vector3.Lerp(initialScale, targetScale, t);
            if (lockedVisual.TryGetComponent(out SpriteRenderer sr))
            {
                Color c = sr.color;
                c.a = Mathf.Lerp(1f, 0f, t);
                sr.color = c;
            }
            yield return null;
        }
        lockedVisual.SetActive(false);
        lockedTr.localScale = initialScale; 
    }

    private void NotifyNeighbors()
    {
        float notifyRadius = checkRadius * 1.2f;
        Collider2D[] neighbors = Physics2D.OverlapCircleAll(transform.position, notifyRadius, hexLayer);
        
        foreach (var col in neighbors)
        {
            if (col == null || col.gameObject == this.gameObject) continue;
            
            HexBlocker hex = col.GetComponent<HexBlocker>();
            if (hex != null) hex.Invoke(nameof(CheckStatus), 0.05f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; 
        Gizmos.DrawWireSphere(transform.position, checkRadius);
    }
}