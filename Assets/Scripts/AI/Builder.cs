using UnityEngine;
using System.Linq;
using System.Collections;

public class Builder : MonoBehaviour
{
    [Header("Настройки")]
    public float speed = 3f;
    public float stopDistance = 0.5f;

    [Header("Визуал")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite buildingSprite;
    [SerializeField] private Transform hammerPivot;

    [Header("Молоток")]
    public float hammerSpeed = 10f;
    public float hammerAngle = 30f;

    private ConstructionSite _targetSite;
    private bool _isBuilding = false;

    void Awake()
    {
        if (bodyRenderer == null) bodyRenderer = GetComponent<SpriteRenderer>();
        if (hammerPivot != null) hammerPivot.gameObject.SetActive(false);
    }

    void Update()
    {
        if (_isBuilding) return;

        if (_targetSite == null) FindWork();
        else MoveAndBuild();
    }

    private void FindWork()
    {
        var sites = Object.FindObjectsByType<ConstructionSite>(FindObjectsSortMode.None)
            .Where(s => s.isResourcesReady && !s.isConstructed && s.enabled)
            .OrderBy(s => Vector2.Distance(transform.position, s.transform.position))
            .ToList();

        if (sites.Count > 0) _targetSite = sites[0];
    }

    private void MoveAndBuild()
    {
        if (_targetSite == null || _targetSite.isConstructed) { _targetSite = null; return; }

        Vector3 destination = _targetSite.transform.position;
        Collider2D col = _targetSite.GetComponent<Collider2D>();
        if (col != null) destination = col.ClosestPoint(transform.position);

        if (Vector2.Distance(transform.position, destination) > stopDistance)
        {
            transform.position = Vector2.MoveTowards(transform.position, destination, speed * Time.deltaTime);
            UpdateFacing(destination.x);
            if (bodyRenderer.sprite != normalSprite) bodyRenderer.sprite = normalSprite;
        }
        else
        {
            StartCoroutine(BuildRoutine());
        }
    }

    private IEnumerator BuildRoutine()
    {
        _isBuilding = true;
        
        if (_targetSite != null)
        {
            UpdateFacing(_targetSite.transform.position.x);
            if (bodyRenderer != null) bodyRenderer.sprite = buildingSprite;
            if (hammerPivot != null) hammerPivot.gameObject.SetActive(true);

            // Пока здание не достроено и оно существует
            while (_targetSite != null && !_targetSite.isConstructed)
            {
                // Двигаем таймер стройки в самом здании!
                _targetSite.AdvanceConstruction(Time.deltaTime);

                // Машем молотком
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

    private void UpdateFacing(float targetX)
    {
        if (bodyRenderer == null) return;
        bool lookLeft = targetX < transform.position.x;
        bodyRenderer.flipX = lookLeft;
        if (hammerPivot != null)
        {
            Vector3 scale = hammerPivot.localScale;
            scale.x = lookLeft ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            hammerPivot.localScale = scale;
        }
    }
}