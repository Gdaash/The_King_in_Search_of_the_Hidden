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
    [SerializeField] private LayerMask obstacleLayers; // В инспекторе выберите Buildings и Obstacles

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

    // Этот метод сработает, если коллайдеры — Триггеры
    private void OnTriggerEnter2D(Collider2D collision) => ProcessHit(collision.gameObject);

    // Этот метод сработает, если коллайдеры — Твердые (не триггеры)
    private void OnCollisionEnter2D(Collision2D collision) => ProcessHit(collision.gameObject);

    private void ProcessHit(GameObject target)
    {
        // 1. Проверка по тегу (здание или юнит)
        if (target.CompareTag(_targetTag)) 
        {
            if (target.TryGetComponent<Health>(out var health)) 
            {
                health.TakeDamage(damage, damageType);
            }
            else 
            {
                // Если Health нет на самом объекте, ищем в родителе (часто для зданий)
                target.GetComponentInParent<Health>()?.TakeDamage(damage, damageType);
            }
            
            Destroy(gameObject);
            return;
        }
        
        // 2. Проверка по слою (если попали в стену/здание другого тега)
        if (((1 << target.layer) & obstacleLayers) != 0) 
        {
            Destroy(gameObject);
        }
    }
}