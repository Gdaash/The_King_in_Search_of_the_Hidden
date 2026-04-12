using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Health : MonoBehaviour
{
    [System.Serializable]
    public struct Resistance { public DamageType type; [Range(0,2)] public float mult; }

    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private List<Resistance> resistances;
    [SerializeField] private SpriteRenderer targetSprite;
    [SerializeField] private GameObject damageTextPrefab;

    public UnityEvent<float> OnHealthChanged;
    public UnityEvent OnDeath;

    private float _cur;
    private Color _orig;
    private bool _dead;

    // Ссылка на ИИ дальнего боя для механики отбегания
    private EnemyAI_Ranged _rangedAI;

    void Start() {
        _cur = maxHealth;
        if (!targetSprite) targetSprite = GetComponentInChildren<SpriteRenderer>();
        if (targetSprite) _orig = targetSprite.color;

        // Кэшируем ссылку на ИИ, если он есть на этом объекте
        _rangedAI = GetComponent<EnemyAI_Ranged>();
    }

    public void TakeDamage(float amt, DamageType type) {
        if (_dead) return;
        float final = amt * GetMult(type);
        _cur = Mathf.Clamp(_cur - final, 0, maxHealth);
        
        StartCoroutine(Flash(final > 0 ? Color.red : Color.green));
        SpawnText(final, type);
        OnHealthChanged?.Invoke(_cur / maxHealth);

        // --- НОВАЯ ЛОГИКА: ОТБЕГАНИЕ ---
        // Если урон > 0 и у нас есть скрипт дальнего боя, даем команду отбежать
        if (final > 0 && _rangedAI != null)
        {
            _rangedAI.OnTakeDamage();
        }
        // ------------------------------

        if (_cur <= 0) Die();
    }

    private float GetMult(DamageType t) 
    {
        var res = resistances.Find(r => r.type == t);
        // Исправлено: корректная проверка существования сопротивления
        // Если тип урона не найден в списке resistances, Find вернет дефолтную структуру с mult = 0
        return (res.mult != 0 || resistances.Any(r => r.type == t)) ? res.mult : 1f;
    }

    private void SpawnText(float val, DamageType t) {
        if (!damageTextPrefab) return;
        var go = Instantiate(damageTextPrefab, transform.position + Vector3.up, Quaternion.identity);
        go.GetComponent<DamageText>()?.Setup(Mathf.Abs(val).ToString("F0"), val > 0 ? Color.red : Color.green);
    }

    private IEnumerator Flash(Color c) {
        if (!targetSprite) yield break;
        targetSprite.color = c;
        yield return new WaitForSeconds(0.2f);
        targetSprite.color = _orig;
    }

    private void Die() { _dead = true; OnDeath?.Invoke(); gameObject.SetActive(false); }
}