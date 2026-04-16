using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [Header("Настройки полета")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifetime = 4f;

    [Header("Настройки урона")]
    [SerializeField] private int damage = 10;
    [SerializeField] private DamageType damageType = DamageType.Physical;

    [Header("Слои препятствий")]
    [SerializeField] private LayerMask obstacleLayers;

    private Vector3 _direction;
    private string _targetTag = "Player";

    public void Setup(Vector3 dir, string targetTag) 
    {
        _direction = dir;
        _targetTag = targetTag;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        Destroy(gameObject, lifetime);
    }

    void Update() 
    {
        transform.position += _direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision) => ProcessHit(collision.gameObject);
    private void OnCollisionEnter2D(Collision2D collision) => ProcessHit(collision.gameObject);

    private void ProcessHit(GameObject target)
    {
        if (target.CompareTag(_targetTag)) 
        {
            if (target.TryGetComponent<Health>(out var health)) 
            {
                // ПЕРЕДАЕМ transform пули как нападавшего
                health.TakeDamage(damage, damageType, transform);
            }
            else 
            {
                // Ищем здоровье в родителе и тоже передаем transform
                target.GetComponentInParent<Health>()?.TakeDamage(damage, damageType, transform);
            }
            
            Destroy(gameObject);
            return;
        }
        
        if (((1 << target.layer) & obstacleLayers) != 0) 
        {
            Destroy(gameObject);
        }
    }
}