using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GameFoundation.MetaProgression
{
    public sealed class WorldFlashlightAvailability : MonoBehaviour
    {
        public const string LegacyUpgradeId = "world.flashlights";
        public const int MaximumCount = 6;

        [SerializeField] private GlobalStats flashlightStats;
        [SerializeField] private GameObject[] flashlights;
        [SerializeField, Min(0f)] private float activationDelay = 0.1f;
        [SerializeField, Min(0f)] private float lightWarmupDelay = 0.3f;
        [Header("Activation button placement")]
        [Tooltip("World-space offset from the first flashlight. It is not affected by the flashlight rotation.")]
        [SerializeField] private Vector3 activationButtonWorldOffset = new(0f, -3f, 0f);

        private float[] _initialLightIntensities;
        private bool _escapeRequested;
        private bool _lightsActivated;
        private int _availableFlashlightCount;
        private FlashlightActivationButton _activationButton;

        private void Awake()
        {
            _availableFlashlightCount = flashlightStats != null ? flashlightStats.AvailableFlashlightCount : 1;
            if (flashlights == null) return;
            _initialLightIntensities = new float[flashlights.Length];

            // Purchased flashlights are visible from the start. Their beams and ground
            // markers stay off until the player deliberately turns the network on.
            for (int i = 0; i < flashlights.Length; i++)
            {
                GameObject flashlight = flashlights[i];
                if (flashlight == null) continue;

                bool isAvailable = i < Mathf.Min(_availableFlashlightCount, flashlights.Length);
                if (flashlight != gameObject) flashlight.SetActive(isAvailable);
                if (!isAvailable) continue;

                FlashlightActivationButton button = flashlight.GetComponentInChildren<FlashlightActivationButton>(true);
                if (button != null) button.gameObject.SetActive(false);

                Transform light = flashlight.transform.Find("Light");
                if (light != null && light.TryGetComponent(out Light2D light2D))
                {
                    _initialLightIntensities[i] = light2D.intensity;
                    light2D.intensity = 0f;
                }

                LogisticFlag flag = flashlight.GetComponentInChildren<LogisticFlag>(true);
                if (flag != null) flag.enabled = false;
                Transform flagLight = flag != null ? flag.transform.Find("Light") : null;
                if (flagLight != null) flagLight.gameObject.SetActive(false);

                SetContentsActive(flashlight, false);
            }
            if (flashlights.Length > 0 && flashlights[0] != null)
            {
                _activationButton = flashlights[0].GetComponentInChildren<FlashlightActivationButton>(true);
                if (_activationButton != null)
                {
                    // The editable source stays in the Flashlight prefab, but the
                    // runtime button becomes an independent scene object. This keeps
                    // it upright and prevents the flashlight's aiming rotation from
                    // affecting its position or collider.
                    _activationButton.transform.SetParent(null, true);
                    _activationButton.transform.position = flashlights[0].transform.position + activationButtonWorldOffset;
                    _activationButton.transform.rotation = Quaternion.identity;
                    _activationButton.gameObject.SetActive(true);
                }
            }
        }

        public void ActivateAvailableLights()
        {
            if (_lightsActivated || _escapeRequested || flashlights == null) return;
            _lightsActivated = true;
            if (_activationButton != null) _activationButton.gameObject.SetActive(false);

            var order = new List<int>();
            int count = Mathf.Min(_availableFlashlightCount, flashlights.Length);
            for (int i = 0; i < count; i++)
                if (flashlights[i] != null)
                    order.Add(i);

            for (int i = order.Count - 1; i > 0; i--)
            {
                int other = Random.Range(0, i + 1);
                (order[i], order[other]) = (order[other], order[i]);
            }
            StartCoroutine(ActivateInOrder(order));
        }

        private IEnumerator ActivateInOrder(List<int> order)
        {
            for (int i = 0; i < order.Count; i++)
            {
                int flashlightIndex = order[i];
                GameObject flashlight = flashlights[flashlightIndex];
                LogisticFlag flag = flashlight.GetComponentInChildren<LogisticFlag>(true);
                Transform light = flashlight.transform.Find("Light");
                Transform flagLight = flag != null ? flag.transform.Find("Light") : null;
                float initialIntensity = _initialLightIntensities[flashlightIndex];
                if (flag != null)
                    flag.transform.localScale = Vector3.one * 0.5f;

                flashlight.SetActive(true);
                if (flag != null) flag.gameObject.SetActive(true);

                if (flag != null)
                {
                    Transform flagTransform = flag.transform;
                    flagTransform.DOKill();
                    DOTween.Sequence().SetTarget(flagTransform)
                        .Append(flagTransform.DOScale(1.12f, 0.18f).SetEase(Ease.OutQuad))
                        .Append(flagTransform.DOScale(0.95f, 0.1f).SetEase(Ease.InOutQuad))
                        .Append(flagTransform.DOScale(1f, 0.1f).SetEase(Ease.OutQuad))
                        .OnComplete(() => ActivateLight(light, flag, flagLight, initialIntensity));
                }
                else
                    ActivateLight(light, flag, flagLight, initialIntensity);

                if (i + 1 < order.Count)
                    yield return new WaitForSeconds(activationDelay);
            }
        }

        private void ActivateLight(Transform light, LogisticFlag flag, Transform flagLight, float initialIntensity)
        {
            if (_escapeRequested) return;
            if (light == null)
            {
                EnableFlag(flag, flagLight);
                return;
            }
            light.gameObject.SetActive(true);
            if (light.TryGetComponent(out Light2D light2D))
                StartCoroutine(RestoreLightIntensity(light2D, flag, flagLight, initialIntensity));
            else
                EnableFlag(flag, flagLight);
        }

        private IEnumerator RestoreLightIntensity(Light2D light, LogisticFlag flag, Transform flagLight, float initialIntensity)
        {
            yield return new WaitForSeconds(lightWarmupDelay);
            if (light != null) light.intensity = initialIntensity;
            EnableFlag(flag, flagLight);
        }

        private static void EnableFlag(LogisticFlag flag, Transform flagLight)
        {
            if (flag != null) flag.enabled = true;
            if (flagLight != null) flagLight.gameObject.SetActive(true);
        }

        private static void SetContentsActive(GameObject flashlight, bool active)
        {
            Transform light = flashlight.transform.Find("Light");
            if (light != null) light.gameObject.SetActive(active);

            LogisticFlag flag = flashlight.GetComponentInChildren<LogisticFlag>(true);
            if (flag != null) flag.gameObject.SetActive(active);
        }

        public void DisableAllForEscape()
        {
            _escapeRequested = true;
            StopAllCoroutines();
            if (_activationButton != null) _activationButton.gameObject.SetActive(false);
            if (flashlights == null) return;

            foreach (GameObject flashlight in flashlights)
            {
                if (flashlight == null) continue;
                LogisticFlag flag = flashlight.GetComponentInChildren<LogisticFlag>(true);
                if (flag != null) flag.transform.DOKill();
                SetContentsActive(flashlight, false);
                if (flashlight != gameObject)
                    flashlight.SetActive(false);
            }
        }
    }
}
