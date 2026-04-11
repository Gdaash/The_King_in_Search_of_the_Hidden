using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [Header("Настройки полета")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifetime = 4f;

    [Header("Настройки урона")]
    [SerializeField] private int damage = 10;
    [SerializeField] private DamageType damageType = DamageType.Physical;

    private Vector3 _direction;
    private string _targetTag = "Player"; // Тэг, по которому будет наноситься урон

    /// <summary>
    /// Инициализация снаряда. Теперь принимает и направление, и тэг цели.
    /// </summary>
    public void Setup(Vector3 dir, string targetTag) 
    {
        _direction = dir;
        _targetTag = targetTag;

        // Поворачиваем снаряд по направлению полета
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        Destroy(gameObject, lifetime);
    }

    void Update() 
    {
        transform.position += _direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision) 
    {
        // 1. Проверяем, совпадает ли тэг объекта с целью (например, "Player")
        if (collision.CompareTag(_targetTag)) 
        {
            if (collision.TryGetComponent<Health>(out var health)) 
            {
                health.TakeDamage(damage, damageType);
                Destroy(gameObject);
            }
        }
        
        // 2. Уничтожение при столкновении с препятствиями
        if (((1 << collision.gameObject.layer) & LayerMask.GetMask("Obstacles")) != 0) 
        {
            Destroy(gameObject);
        }
    }
}