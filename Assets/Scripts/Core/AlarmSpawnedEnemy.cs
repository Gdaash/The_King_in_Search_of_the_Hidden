using UnityEngine;
using UnityEngine.Events;

/// <summary>Tracks a spawned enemy's lifetime without polling the scene or changing combat behavior.</summary>
[DisallowMultipleComponent]
public sealed class AlarmSpawnedEnemy : MonoBehaviour
{
    private AlarmSystem owner;
    private GameObject prefab;
    private Health health;
    private bool counted;
    private bool subscribed;

    public void Initialize(AlarmSystem source, GameObject type)
    {
        if (owner == source && prefab == type) return;
        Detach();
        owner = source;
        prefab = type;
        health = GetComponent<Health>();
        if (isActiveAndEnabled) Attach();
    }

    private void OnEnable() => Attach();
    private void OnDisable() => Detach();

    private void Attach()
    {
        if (owner == null || health == null || subscribed) return;
        health.OnHealthChanged ??= new UnityEvent<float>();
        health.OnDeath ??= new UnityEvent();
        health.OnHealthChanged.AddListener(OnHealthChanged);
        health.OnDeath.AddListener(OnDeath);
        subscribed = true;
        SetCounted(!health.IsDead && health.CurrentHealth > 0f);
    }

    private void Detach()
    {
        if (subscribed && health != null)
        {
            health.OnHealthChanged.RemoveListener(OnHealthChanged);
            health.OnDeath.RemoveListener(OnDeath);
        }
        subscribed = false;
        SetCounted(false);
    }

    private void OnHealthChanged(float fraction) => SetCounted(fraction > 0f && !health.IsDead);
    private void OnDeath() => SetCounted(false);
    private void SetCounted(bool value)
    {
        if (counted == value) return;
        counted = value;
        if (owner != null) owner.ChangeEnemyCount(prefab, value ? 1 : -1);
    }
}
