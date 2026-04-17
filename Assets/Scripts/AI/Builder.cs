using UnityEngine;
using System.Linq;
using System.Collections;

public class Builder : MonoBehaviour, IEnemyAI
{
    [Header("Настройки")]
    public float stopDistance = 0.5f;

    [Header("Визуал (Без аниматора)")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private Sprite normalSprite;    
    [SerializeField] private Sprite buildingSprite;  
    [SerializeField] private Transform hammerPivot;  

    [Header("Настройки молотка")]
    [SerializeField] private float hammerSpeed = 10f; 
    [SerializeField] private float hammerAngle = 30f; 

    private EnemyMovement _movement;
    private ConstructionSite _targetSite;
    private bool _isBuilding = false;

    // --- Реализация интерфейса IEnemyAI ---
    
    public Transform GetTarget() => _targetSite != null ? _targetSite.transform : null;

    // Заглушка: строитель не атакует, но интерфейс требует этот метод
    public void FinishAttack() { } 

    // Реакция на урон (можно оставить пустой или добавить логику испуга)
    public void OnTakeDamage(Transform attacker) 
    { 
        // Если хотите, чтобы строитель бросал работу при ударе:
        // _targetSite = null; 
    }

    // ---------------------------------------

    void Awake()
    {
        _movement = GetComponent<EnemyMovement>();
        
        if (bodyRenderer == null) bodyRenderer = GetComponent<SpriteRenderer>();
        if (hammerPivot != null) hammerPivot.gameObject.SetActive(false);
    }

    void Update()
    {
        if (_isBuilding) return;

        if (_targetSite == null)
        {
            FindWork();
            if (_movement != null) _movement.SetMove(false);
        }
        else
        {
            if (_targetSite.isConstructed)
            {
                _targetSite = null;
                if (_movement != null) _movement.SetMove(false);
                return;
            }

            if (_movement != null) _movement.SetMove(true);
            
            float dist = Vector2.Distance(transform.position, _targetSite.transform.position);
            if (dist <= stopDistance)
            {
                if (_movement != null) _movement.SetMove(false);
                StartCoroutine(BuildRoutine());
            }
        }
    }

    private void FindWork()
    {
        var sites = Object.FindObjectsByType<ConstructionSite>(FindObjectsSortMode.None)
            .Where(s => s.isResourcesReady && !s.isConstructed && s.enabled)
            .OrderBy(s => Vector2.Distance(transform.position, s.transform.position))
            .ToList();

        if (sites.Count > 0)
        {
            _targetSite = sites[0];
        }
    }

    private IEnumerator BuildRoutine()
    {
        _isBuilding = true;
        
        if (_targetSite != null)
        {
            float diff = _targetSite.transform.position.x - transform.position.x;
            float initialScaleX = Mathf.Abs(transform.localScale.x);
            float newScaleX = diff > 0 ? initialScaleX : -initialScaleX;
            transform.localScale = new Vector3(newScaleX, transform.localScale.y, transform.localScale.z);

            if (bodyRenderer != null) bodyRenderer.sprite = buildingSprite;
            if (hammerPivot != null) hammerPivot.gameObject.SetActive(true);

            while (_targetSite != null && !_targetSite.isConstructed)
            {
                _targetSite.AdvanceConstruction(Time.deltaTime);

                if (hammerPivot != null)
                {
                    float rotation = Mathf.Sin(Time.time * hammerSpeed) * hammerAngle;
                    hammerPivot.localRotation = Quaternion.Euler(0, 0, rotation);
                }

                yield return null;
            }

            if (hammerPivot != null) hammerPivot.gameObject.SetActive(false);
            if (bodyRenderer != null) bodyRenderer.sprite = normalSprite;
        }
        
        _isBuilding = false;
        _targetSite = null; 
    }
}