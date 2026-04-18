using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

public class HexBlocker : MonoBehaviour
{
    [Header("Ссылки на объекты")]
    [SerializeField] private GameObject lockedVisual;   
    [SerializeField] private GameObject unlockedVisual; 
    [SerializeField] private GameObject[] skullIcons; 

    [Header("Настройки сетки и слоев")]
    [SerializeField] private float checkRadius = 1.1f; 
    [SerializeField] private float hiddenCheckRadius = 0.8f; // НОВОЕ: Радиус поиска скрытых объектов
    [SerializeField] private LayerMask hexLayer;       
    [SerializeField] private LayerMask hiddenLayers; 

    [Header("События")]
    public UnityEvent OnHexUnlocked; 

    private bool _isRemoved = false;
    private bool _isCurrentlyUnlocked = false; 
    private List<GameObject> _hiddenObjects = new List<GameObject>();
    private int _totalDanger = 0;

    private void Awake()
    {
        if (skullIcons != null)
        {
            foreach (var skull in skullIcons)
            {
                if (skull != null) skull.SetActive(false);
            }
        }
        
        FindAndHideObjects();
    }

    private void Start()
    {
        CheckStatus(true);
    }

    private void FindAndHideObjects()
    {
        // ИСПОЛЬЗУЕМ: hiddenCheckRadius вместо фиксированного 0.5f
        Collider2D[] overlays = Physics2D.OverlapCircleAll(transform.position, hiddenCheckRadius, hiddenLayers);
        
        foreach (var col in overlays)
        {
            if (col == null || col.gameObject == this.gameObject) continue;

            if (!_hiddenObjects.Contains(col.gameObject))
            {
                DangerSource danger = col.GetComponent<DangerSource>();
                if (danger != null)
                {
                    _totalDanger += danger.dangerLevel;
                }

                _hiddenObjects.Add(col.gameObject);
                col.gameObject.SetActive(false);
            }
        }

        if (skullIcons != null)
        {
            int skullsToShow = Mathf.Min(_totalDanger, skullIcons.Length);
            for (int i = 0; i < skullsToShow; i++)
            {
                if (skullIcons[i] != null) skullIcons[i].SetActive(true);
            }
        }
    }

    public void RemoveHex()
    {
        if (_isRemoved) return;
        _isRemoved = true;

        if (TryGetComponent(out Collider2D col)) col.enabled = false;
        
        foreach (var obj in _hiddenObjects)
        {
            if (obj != null) obj.SetActive(true);
        }

        if (skullIcons != null)
        {
            foreach (var skull in skullIcons)
            {
                if (skull != null) skull.SetActive(false);
            }
        }

        NotifyNeighbors();
        StartCoroutine(AnimateAndDestroy());
    }

    private IEnumerator AnimateAndDestroy()
    {
        Vector3 initialScale = transform.localScale;
        Quaternion initialRotation = transform.rotation;
        float elapsed = 0;
        Vector3 targetScale = initialScale * 1.1f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(initialScale, targetScale, elapsed / 0.2f);
            yield return null;
        }
        elapsed = 0;
        Vector3 startScaleArea = transform.localScale;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.3f;
            transform.localScale = Vector3.Lerp(startScaleArea, Vector3.zero, t);
            transform.rotation = initialRotation * Quaternion.Euler(0, 0, -t * 360f);
            yield return null;
        }
        gameObject.SetActive(false);
    }

    public void CheckStatus() => CheckStatus(false);

    public void CheckStatus(bool silent)
    {
        if (_isRemoved || !gameObject.activeInHierarchy) return;

        int neighborCount = 0;
        for (int i = 0; i < 6; i++)
        {
            float angle = i * 60f * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
            Collider2D hit = Physics2D.OverlapPoint((Vector2)transform.position + (Vector2)dir * checkRadius, hexLayer);
            if (hit != null && hit.gameObject != this.gameObject) neighborCount++;
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
        Collider2D[] neighbors = Physics2D.OverlapCircleAll(transform.position, checkRadius * 1.5f, hexLayer);
        foreach (var col in neighbors)
        {
            if (col == null || col.gameObject == this.gameObject) continue;
            HexBlocker hex = col.GetComponent<HexBlocker>();
            if (hex != null) hex.Invoke(nameof(CheckStatus), 0.05f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, checkRadius);
        // Рисуем красный радиус согласно новой переменной
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, hiddenCheckRadius);
    }
}