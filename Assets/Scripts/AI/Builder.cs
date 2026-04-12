using UnityEngine;
using System.Linq;
using System.Collections;

public class Builder : MonoBehaviour
{
    [Header("Настройки")]
    public float speed = 3f;
    public float stopDistance = 0.5f;

    private ConstructionSite _targetSite;
    private bool _isBuilding = false;

    void Update()
    {
        if (_isBuilding) return;

        // Ищем работу только если её сейчас нет
        if (_targetSite == null)
        {
            FindWork();
        }
        else
        {
            MoveAndBuild();
        }
    }

    private void FindWork()
    {
        // Ищем чертежи, где ресурсы уже собраны (isResourcesReady)
        var sites = Object.FindObjectsByType<ConstructionSite>(FindObjectsSortMode.None)
            .Where(s => s.isResourcesReady && !s.isConstructed && s.enabled)
            .OrderBy(s => Vector2.Distance(transform.position, s.transform.position))
            .ToList();

        if (sites.Count > 0)
        {
            _targetSite = sites[0];
        }
    }

    private void MoveAndBuild()
    {
        if (_targetSite == null || _targetSite.isConstructed)
        {
            _targetSite = null;
            return;
        }

        // Идем к ближайшей точке коллайдера чертежа
        Vector3 destination = _targetSite.transform.position;
        Collider2D col = _targetSite.GetComponent<Collider2D>();
        if (col != null)
        {
            destination = col.ClosestPoint(transform.position);
        }

        float dist = Vector2.Distance(transform.position, destination);

        if (dist > stopDistance)
        {
            transform.position = Vector2.MoveTowards(transform.position, destination, speed * Time.deltaTime);
            
            // Поворот спрайта (если есть SpriteRenderer на этом же объекте)
            if (TryGetComponent(out SpriteRenderer sr))
            {
                sr.flipX = (destination.x - transform.position.x) < 0;
            }
        }
        else
        {
            StartCoroutine(BuildRoutine());
        }
    }

    private IEnumerator BuildRoutine()
    {
        _isBuilding = true;
        
        // На всякий случай еще раз проверяем цель
        if (_targetSite != null)
        {
            Debug.Log($"Рабочий начал строить {_targetSite.gameObject.name}...");
            
            // Вызываем событие начала стройки на самом чертеже
            _targetSite.OnConstructionStarted?.Invoke();

            yield return new WaitForSeconds(_targetSite.buildTime);

            if (_targetSite != null)
            {
                _targetSite.FinishBuilding();
            }
        }
        
        _isBuilding = false;
        _targetSite = null; // После стройки ищем новую работу
    }
}