using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

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

    void Start() {
        _cur = maxHealth;
        if (!targetSprite) targetSprite = GetComponentInChildren<SpriteRenderer>();
        if (targetSprite) _orig = targetSprite.color;
    }

    public void TakeDamage(float amt, DamageType type) {
        if (_dead) return;
        float final = amt * GetMult(type);
        _cur = Mathf.Clamp(_cur - final, 0, maxHealth);
        
        StartCoroutine(Flash(final > 0 ? Color.red : Color.green));
        SpawnText(final, type);
        OnHealthChanged?.Invoke(_cur / maxHealth);
        if (_cur <= 0) Die();
    }

    private float GetMult(DamageType t) 
{
    // Ищем сопротивление в списке
    var res = resistances.Find(r => r.type == t);
    
    // Если нашли (тип не дефолтный), возвращаем множитель. Если нет — возвращаем 1.0
    return res.mult != 0 ? res.mult : 1f;
}

    private void SpawnText(float val, DamageType t) {
        if (!damageTextPrefab) return;
        var go = Instantiate(damageTextPrefab, transform.position + Vector3.up, Quaternion.identity);
        go.GetComponent<DamageText>()?.Setup(Mathf.Abs(val).ToString("F0"), val > 0 ? Color.red : Color.green);
    }

    private IEnumerator Flash(Color c) {
        targetSprite.color = c;
        yield return new WaitForSeconds(0.2f);
        targetSprite.color = _orig;
    }

    private void Die() { _dead = true; OnDeath?.Invoke(); gameObject.SetActive(false); }
}