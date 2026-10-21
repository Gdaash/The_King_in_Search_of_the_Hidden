using UnityEngine;
using System.Collections.Generic;

public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifetime = 4f;
    [SerializeField] private LayerMask obstacleLayers;
    [Header("Визуальные эффекты")]
    [SerializeField] private Transform animatedVisual;
    [SerializeField] private float flightRotationSpeed;
    [SerializeField] private bool collapseOnImpact;
    [SerializeField, Min(.01f)] private float deathDuration = .2f;
    private SpriteRenderer[] visualSprites;
    private Color[] visualColors;
    private Collider2D[] hitColliders;
    private Vector3 visualScale;
    private float remainingLifetime, deathAge;
    private bool dying;
    public bool IsDying => dying;
    private void Awake()
    {
        visualSprites = GetComponentsInChildren<SpriteRenderer>(true);
        visualColors = new Color[visualSprites.Length];
        for (int i = 0; i < visualSprites.Length; i++) visualColors[i] = visualSprites[i].color;
        if (animatedVisual == null)
            foreach (var sprite in visualSprites) if (sprite.enabled && sprite.sprite != null) { animatedVisual = sprite.transform; break; }
        if (animatedVisual == null) animatedVisual = transform;
        hitColliders = GetComponentsInChildren<Collider2D>(true);
    }

    private Vector3 _direction;
    private string _targetTag;
    private List<GlobalStats.DamageInfo> _damageData;
    private Transform _owner;
    private float _damageMultiplier = 1f;
    private bool _hasHit;
    private bool singleDamage;
    private float hitDamage;
    private DamageType hitType;
    private DamageType visualDamageType;
    private void ApplyDamagePalette(DamageType type)
    {
        visualDamageType = type;
        var material = DamagePalette.MaterialFor(type);
        if (material != null) foreach (var sprite in visualSprites) sprite.sharedMaterial = material;
    }
    private float _speedMultiplier = 1f;
    public float BaseSpeed => speed;
    public float CurrentSpeed => speed * _speedMultiplier;
    public Vector3 Direction => _direction;
    public DamageType HitType => hitType;
    public float HitDamage => hitDamage;

    public void Setup(Vector3 dir, string targetTag, List<GlobalStats.DamageInfo> damageData, Transform owner = null, float damageMultiplier = 1f, float speedMultiplier = 1f, float minimumLifetime = 0f)
    {
        _direction = dir;
        _hasHit = false;
        dying = false; deathAge = 0;
        singleDamage=false;
        _targetTag = targetTag;
        _damageData = damageData;
        _owner = owner;
        _damageMultiplier = Mathf.Max(0f, damageMultiplier);
        _speedMultiplier = Mathf.Max(.01f,speedMultiplier);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        remainingLifetime = Mathf.Max(lifetime, minimumLifetime);
        visualScale = animatedVisual.localScale;
        DamageType primary = DamageType.Physical; float strongest = 0;
        if (damageData != null) foreach (var damage in damageData)
            if (damage.TotalDamage > strongest) { strongest = damage.TotalDamage; primary = damage.type; }
        ApplyDamagePalette(primary);
    }

    public void SetupHit(Vector3 dir,string targetTag,float damage,DamageType type,Transform owner,float speedMultiplier=1,float minimumLifetime=0)
    {
        Setup(dir,targetTag,null,owner,1,speedMultiplier,minimumLifetime);
        singleDamage=true;hitDamage=damage;hitType=type;
        ApplyDamagePalette(type);
    }

    void Update()
    {
        float delta = Time.deltaTime;
        if (animatedVisual != null && flightRotationSpeed != 0)
            animatedVisual.Rotate(0, 0, flightRotationSpeed * (dying ? 5 : 1) * delta);
        if (!dying)
        {
            transform.position += _direction * speed * _speedMultiplier * delta;
            remainingLifetime -= delta;
            if (remainingLifetime <= 0) BeginDeath();
            return;
        }
        deathAge += delta;
        float t = Mathf.Clamp01(deathAge / deathDuration);
        float scale = collapseOnImpact ? (t < .25f ? Mathf.Lerp(1, 1.3f, t * 4) : 1.3f * (1 - Mathf.SmoothStep(0, 1, (t - .25f) / .75f))) :
            Mathf.Lerp(1, 1.2f, Mathf.Clamp01(t * 4));
        animatedVisual.localScale = visualScale * scale;
        float alpha = collapseOnImpact ? 1 - Mathf.InverseLerp(.7f, 1, t) : 1 - t;
        for (int i = 0; i < visualSprites.Length; i++)
            if (visualSprites[i] != null) { var color = visualColors[i]; color.a *= alpha; visualSprites[i].color = color; }
        if (t >= 1) Destroy(gameObject);
    }
    public void BeginDeath()
    {
        if (dying) return;
        dying = true; _hasHit = true; deathAge = 0;
        visualScale = animatedVisual.localScale;
        foreach (var collider in hitColliders) if (collider != null) collider.enabled = false;
        if (TryGetComponent<Rigidbody2D>(out var body)) body.linearVelocity = Vector2.zero;
    }

    private void OnTriggerEnter2D(Collider2D collision) => ProcessHit(collision.gameObject);

    private void ProcessHit(GameObject target)
    {
        if (_hasHit) return;
        var civilianHealth = target.GetComponentInParent<Health>();
        bool hostileCivilianHit = _targetTag == "Player" && _owner != null && _owner.CompareTag("Enemy1") &&
            civilianHealth != null && WorldCivilianTarget.Contains(civilianHealth.gameObject);
        if (target.CompareTag(_targetTag) || hostileCivilianHit)
        {
            _hasHit = true;
            var h = target.GetComponentInParent<Health>();
            if(h!=null && singleDamage)h.TakeDamage(hitDamage,hitType,_owner!=null?_owner:transform);
            else if (h != null && _damageData != null) {
                foreach (var dmg in _damageData) h.TakeDamage(dmg.TotalDamage * _damageMultiplier, dmg.type, _owner != null ? _owner : transform);
            }
            CombatImpactBurst.Spawn(transform.position, DamagePalette.For(visualDamageType));
            BeginDeath();
        }
        else if (((1 << target.layer) & obstacleLayers) != 0) BeginDeath();
    }
}
