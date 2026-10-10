using UnityEngine;
using System.Collections.Generic;

public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifetime = 4f;
    [SerializeField] private LayerMask obstacleLayers;

    private Vector3 _direction;
    private string _targetTag;
    private List<GlobalStats.DamageInfo> _damageData;
    private Transform _owner;
    private float _damageMultiplier = 1f;
    private bool _hasHit;
    private float _speedMultiplier = 1f;

    public void Setup(Vector3 dir, string targetTag, List<GlobalStats.DamageInfo> damageData, Transform owner = null, float damageMultiplier = 1f, float speedMultiplier = 1f, float minimumLifetime = 0f)
    {
        _direction = dir;
        _hasHit = false;
        _targetTag = targetTag;
        _damageData = damageData;
        _owner = owner;
        _damageMultiplier = Mathf.Max(0f, damageMultiplier);
        _speedMultiplier = Mathf.Max(.01f,speedMultiplier);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        Destroy(gameObject, Mathf.Max(lifetime,minimumLifetime));
    }

    void Update() => transform.position += _direction * speed * _speedMultiplier * Time.deltaTime;

    private void OnTriggerEnter2D(Collider2D collision) => ProcessHit(collision.gameObject);

    private void ProcessHit(GameObject target)
    {
        if (_hasHit) return;
        if (target.CompareTag(_targetTag)) 
        {
            _hasHit = true;
            var h = target.GetComponentInParent<Health>();
            if (h != null && _damageData != null) {
                foreach (var dmg in _damageData) h.TakeDamage(dmg.TotalDamage * _damageMultiplier, dmg.type, _owner != null ? _owner : transform);
            }
            CombatImpactBurst.Spawn(transform.position, new Color(1f, 0.88f, 0.55f, 1f));
            Destroy(gameObject);
        }
        else if (((1 << target.layer) & obstacleLayers) != 0) { _hasHit = true; Destroy(gameObject); }
    }
}
