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

    public void Setup(Vector3 dir, string targetTag, List<GlobalStats.DamageInfo> damageData) 
    {
        _direction = dir;
        _targetTag = targetTag;
        _damageData = damageData;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        Destroy(gameObject, lifetime);
    }

    void Update() => transform.position += _direction * speed * Time.deltaTime;

    private void OnTriggerEnter2D(Collider2D collision) => ProcessHit(collision.gameObject);

    private void ProcessHit(GameObject target)
    {
        if (target.CompareTag(_targetTag)) 
        {
            var h = target.GetComponentInParent<Health>();
            if (h != null && _damageData != null) {
                foreach (var dmg in _damageData) h.TakeDamage(dmg.TotalDamage, dmg.type, transform);
            }
            Destroy(gameObject);
        }
        else if (((1 << target.layer) & obstacleLayers) != 0) Destroy(gameObject);
    }
}
