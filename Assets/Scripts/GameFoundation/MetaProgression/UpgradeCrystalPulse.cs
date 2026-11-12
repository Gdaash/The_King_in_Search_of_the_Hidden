using UnityEngine;

namespace GameFoundation.MetaProgression
{
    internal sealed class UpgradeCrystalPulse : MonoBehaviour
    {
        private Vector3 baseScale;
        private Quaternion baseRotation;
        private float phase;

        private void Awake()
        {
            baseScale = transform.localScale;
            baseRotation = transform.localRotation;
            phase = Random.Range(0f, Mathf.PI * 2);
        }

        private void Update()
        {
            float t = Time.time;
            transform.localScale = baseScale * (1 + .055f * Mathf.Sin(t * 4f + phase));
            transform.localRotation = baseRotation * Quaternion.Euler(0, 0, 4 * Mathf.Sin(t * 15f + phase));
        }
    }
}
