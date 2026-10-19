using UnityEngine;
using GameFoundation.MetaProgression;

/// <summary>Restores authored visuals and gameplay state before a pooled alarm object is enabled.</summary>
public sealed class WorldPooledObject : MonoBehaviour
{
    public WorldSpawnPool Owner { get; private set; }
    public GameObject Prefab { get; private set; }
    internal bool Queued;
    private Transform[] transforms;
    private Vector3[] positions, scales;
    private Quaternion[] rotations;
    private SpriteRenderer[] sprites;
    private Color[] colors;
    private Collider2D[] colliders;
    private bool[] colliderEnabled;
    private Behaviour[] behaviours;
    private bool[] behaviourEnabled;
    private Health health;
    private EnemyLevel level;
    private EnemyAI melee;
    private EnemyAI_Ranged ranged;
    private EnemyMovement movement;
    private EnemyVisuals visuals;
    private EnemyVisuals_Ranged rangedVisuals;
    private HealthBar[] bars;
    private Rigidbody2D body;
    private Vector3 rootScale;

    internal void Initialize(WorldSpawnPool owner, GameObject prefab)
    {
        Owner = owner; Prefab = prefab; rootScale = transform.localScale;
        transforms = GetComponentsInChildren<Transform>(true);
        positions = new Vector3[transforms.Length]; scales = new Vector3[transforms.Length]; rotations = new Quaternion[transforms.Length];
        for (int i = 0; i < transforms.Length; i++) { positions[i] = transforms[i].localPosition; scales[i] = transforms[i].localScale; rotations[i] = transforms[i].localRotation; }
        sprites = GetComponentsInChildren<SpriteRenderer>(true); colors = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++) colors[i] = sprites[i].color;
        colliders = GetComponentsInChildren<Collider2D>(true); colliderEnabled = new bool[colliders.Length];
        for (int i = 0; i < colliders.Length; i++) colliderEnabled[i] = colliders[i].enabled;
        behaviours = GetComponentsInChildren<Behaviour>(true); behaviourEnabled = new bool[behaviours.Length];
        for (int i = 0; i < behaviours.Length; i++) behaviourEnabled[i] = behaviours[i].enabled;
        health = GetComponent<Health>(); level = GetComponent<EnemyLevel>();
        melee = GetComponent<EnemyAI>(); ranged = GetComponent<EnemyAI_Ranged>(); movement = GetComponent<EnemyMovement>();
        visuals = GetComponent<EnemyVisuals>(); rangedVisuals = GetComponent<EnemyVisuals_Ranged>();
        bars = GetComponentsInChildren<HealthBar>(true); body = GetComponent<Rigidbody2D>();
    }

    internal void Prepare(Vector3 position, Quaternion rotation)
    {
        for (int i = 0; i < transforms.Length; i++) if (transforms[i] != null && transforms[i] != transform)
        { transforms[i].localPosition = positions[i]; transforms[i].localRotation = rotations[i]; transforms[i].localScale = scales[i]; }
        transform.SetPositionAndRotation(position, rotation); transform.localScale = rootScale;
        for (int i = 0; i < sprites.Length; i++) if (sprites[i] != null) sprites[i].color = colors[i];
        for (int i = 0; i < colliders.Length; i++) if (colliders[i] != null) colliders[i].enabled = colliderEnabled[i];
        for (int i = 0; i < behaviours.Length; i++) if (behaviours[i] != null) behaviours[i].enabled = behaviourEnabled[i];
        if (body != null) { body.linearVelocity = Vector2.zero; body.angularVelocity = 0; body.position = position; }
        if (level != null) level.ResetForSpawn(AlarmSystem.Instance != null ? AlarmSystem.Instance.EnemyLevel : 1);
        if (health != null) health.ResetForSpawn();
        if (movement != null) movement.ResetForSpawn();
        if (melee != null) melee.ResetForSpawn();
        if (ranged != null) ranged.ResetForSpawn();
        if (visuals != null) visuals.ResetForSpawn();
        if (rangedVisuals != null) rangedVisuals.ResetForSpawn();
        foreach (var bar in bars) if (bar != null) bar.ResetForSpawn();
    }

    private void OnDisable() { if (Owner != null) Owner.Return(this); }
    private void OnDestroy() { if (Owner != null) Owner.Forget(this); }
}
