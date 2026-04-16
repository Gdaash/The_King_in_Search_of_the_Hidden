using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Health : MonoBehaviour
{
    [System.Serializable]
    public struct Resistance { public DamageType type; [Range(0, 2)] public float mult; }

    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private List<Resistance> resistances;
    [SerializeField] private SpriteRenderer targetSprite;
    [SerializeField] private GameObject damageTextPrefab;

    public UnityEvent<float> OnHealthChanged;
    public UnityEvent OnDeath;

    private float _cur;
    private Color _orig;
    private bool _dead;

    // Ссылки на оба типа ИИ
    private EnemyAI_Ranged _rangedAI;
    private EnemyAI _meleeAI;

    void Start() {
        _cur = maxHealth;
        if (!targetSprite) targetSprite = GetComponentInChildren<SpriteRenderer>();
        if (targetSprite) _orig = targetSprite.color;

        // Кэшируем ссылки на компоненты ИИ
        _rangedAI = GetComponent<EnemyAI_Ranged>();
        _meleeAI = GetComponent<EnemyAI>();
    }

    // Добавляем параметр attacker, чтобы знать, кто ударил
    public void TakeDamage(float amt, DamageType type, Transform attacker = null) {
        if (_dead) return;
        float final = amt * GetMult(type);
        _cur = Mathf.Clamp(_cur - final, 0, maxHealth);
        
        StartCoroutine(Flash(final > 0 ? Color.red : Color.green));
        SpawnText(final, type);
        OnHealthChanged?.Invoke(_cur / maxHealth);

        // --- ЛОГИКА КОНТРАТАКИ ---
        if (final > 0)
        {
            // Сообщаем ИИ дальнего боя (передаем attacker)
            if (_rangedAI != null) _rangedAI.OnTakeDamage(attacker);
            
            // Сообщаем ИИ ближнего боя (передаем attacker)
            if (_meleeAI != null) _meleeAI.OnTakeDamage(attacker);
        }
        // -------------------------

        if (_cur <= 0) Die();
    }

    private float GetMult(DamageType t) 
    {
        var res = resistances.Find(r => r.type == t);
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