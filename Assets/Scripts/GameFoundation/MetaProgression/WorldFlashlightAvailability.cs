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

        private float[] _initialLightIntensities;
        private bool _escapeRequested;

        private void Awake()
        {
            int count = 1;
            if (flashlightStats != null)
            {
                flashlightStats.LoadStats();
                for (int i = 2; i <= MaximumCount; i++)
                {
                    if (!flashlightStats.HasUnlockedFlashlight("Flashlight" + i)) break;
                    count++;
                }
            }
            if (flashlights == null) return;
            _initialLightIntensities = new float[flashlights.Length];

            // Этот компонент находится на первом фонаре: его корень должен
            // оставаться активным, даже если он выпадет последним в очереди.
            for (int i = 0; i < flashlights.Length; i++)
            {
                GameObject flashlight = flashlights[i];
                if (flashlight == null) continue;

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
                if (flashlight != gameObject)
                    flashlight.SetActive(false);
            }

            var order = new List<int>();
            for (int i = 0; i < Mathf.Min(count, flashlights.Length); i++)
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

                if (flashlight != gameObject)
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
