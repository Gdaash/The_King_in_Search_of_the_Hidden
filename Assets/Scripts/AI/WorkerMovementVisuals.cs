using UnityEngine;

// Animate the visual pivot only; navigation and collision stay on the unit root.
[RequireComponent(typeof(Rigidbody2D))]
public sealed class WorkerMovementVisuals : MonoBehaviour
{
    [SerializeField] private Transform visualPivot;
    [SerializeField, Min(0f)] private float hopHeight = 0.065f;
    [SerializeField, Min(0f)] private float stepsPerSecond = 3.5f;
    [SerializeField, Min(0f)] private float swayDegrees = 2.2f;
    [SerializeField, Min(0f)] private float shakeDegrees = 0.65f;
    [SerializeField, Min(0f)] private float shakeDistance = 0.006f;
    private Rigidbody2D body;
    private Vector3 restPosition;
    private Quaternion restRotation;
    private float phase;
    private float weight;
    private bool initialized;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (visualPivot == null) { enabled = false; return; }
        restPosition = visualPivot.localPosition;
        restRotation = visualPivot.localRotation;
        initialized = true;
        phase = Random.value * Mathf.PI * 2f;
    }

    private void LateUpdate()
    {
        if (visualPivot == null || Time.deltaTime <= 0f) return;
        bool moving = body.linearVelocity.sqrMagnitude > 0.01f;
        weight = Mathf.MoveTowards(weight, moving ? 1f : 0f, Time.deltaTime * 10f);
        if (moving) phase += Time.deltaTime * stepsPerSecond * Mathf.PI;
        float shake = Mathf.Sin(phase * 7f);
        float angle = (Mathf.Sin(phase) * swayDegrees + shake * shakeDegrees) * weight;
        visualPivot.localPosition = restPosition + new Vector3(shake * shakeDistance,
            Mathf.Abs(Mathf.Sin(phase)) * hopHeight, 0f) * weight;
        visualPivot.localRotation = restRotation * Quaternion.Euler(0f, 0f, angle);
    }

    private void OnDisable()
    {
        weight = 0f;
        if (!initialized || visualPivot == null) return;
        visualPivot.localPosition = restPosition;
        visualPivot.localRotation = restRotation;
    }
}
