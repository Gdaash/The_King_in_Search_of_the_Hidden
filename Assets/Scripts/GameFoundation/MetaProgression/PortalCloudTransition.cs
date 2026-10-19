using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameFoundation.MetaProgression
{
    /// <summary>Persistent screen cover; a scene is replaced only after the cloud curtain is opaque.</summary>
    public sealed class PortalCloudTransition : MonoBehaviour
    {
        private const string ResourcePath = "UI/Portal Cloud Transition";
        private static PortalCloudTransition active;
        public static bool IsLoading { get; private set; }
        [SerializeField] private Image dimmer;
        [SerializeField] private RawImage clouds;
        [SerializeField, Range(0, 1)] private float dimmerOpacity = .3f;
        [SerializeField, Min(.01f)] private float dimSeconds = .18f;
        [SerializeField, Min(.01f)] private float closeSeconds = .85f;
        [SerializeField, Min(.01f)] private float openSeconds = .85f;
        private Material cloudMaterial;
        private readonly Dictionary<EventSystem, bool> navigation = new();
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");
        private static readonly int ViewSizeId = Shader.PropertyToID("_ViewSize");
        private static readonly int FlowTimeId = Shader.PropertyToID("_FlowTime");
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() { active = null; IsLoading = false; }
        public static bool CanLoad(string scene) => !IsLoading && Application.CanStreamedLevelBeLoaded(scene) &&
            Resources.Load<PortalCloudTransition>(ResourcePath) != null;
        public static bool Load(string scene)
        {
            if (!CanLoad(scene)) return false;
            IsLoading = true;
            active = Instantiate(Resources.Load<PortalCloudTransition>(ResourcePath));
            DontDestroyOnLoad(active.gameObject);
            active.Begin(scene, null);
            return true;
        }
        /// <summary>Apply a local screen change underneath the same closed cloud curtain.</summary>
        public static bool Cover(Action whenCovered)
        {
            if (IsLoading || whenCovered == null) return false;
            var prefab = Resources.Load<PortalCloudTransition>(ResourcePath);
            if (prefab == null) return false;
            IsLoading = true;
            active = Instantiate(prefab);
            DontDestroyOnLoad(active.gameObject);
            active.Begin(null, whenCovered);
            return true;
        }
        private void Begin(string scene, Action whenCovered)
        {
            cloudMaterial = new Material(clouds.material);
            clouds.material = cloudMaterial;
            cloudMaterial.SetFloat(ProgressId, 0);
            dimmer.color = new Color(0, 0, 0, 0);
            SceneManager.sceneLoaded += OnSceneLoaded;
            BlockNavigation();
            StartCoroutine(Travel(scene, whenCovered));
        }
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => BlockNavigation();
        private void BlockNavigation()
        {
            foreach (var system in UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
            {
                if (!navigation.ContainsKey(system)) navigation.Add(system, system.sendNavigationEvents);
                system.SetSelectedGameObject(null);
                system.sendNavigationEvents = false;
            }
        }
        private void LateUpdate()
        {
            if (cloudMaterial == null) return;
            var size = ((RectTransform)clouds.transform).rect.size;
            cloudMaterial.SetVector(ViewSizeId, new Vector4(Mathf.Max(1, size.x), Mathf.Max(1, size.y), 0, 0));
            cloudMaterial.SetFloat(FlowTimeId, Time.unscaledTime);
        }
        private IEnumerator Travel(string scene, Action whenCovered)
        {
            yield return FadeDimmer(0, dimmerOpacity);
            yield return MoveClouds(0, 1, closeSeconds);
            // Present one fully covered frame before asset loading or scene activation can stall.
            yield return null;
            AsyncOperation operation = null;
            try
            {
                whenCovered?.Invoke();
                if (!string.IsNullOrEmpty(scene)) operation = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
            }
            catch (Exception error) { Debug.LogException(error); }
            if (operation != null)
            {
                while (!operation.isDone) yield return null;
                // Awake, OnEnable and Start of the destination run underneath the closed curtain.
                yield return null;
            }
            else yield return null; // Let the report lay out underneath the fully closed curtain.
            yield return MoveClouds(1, 0, openSeconds);
            yield return FadeDimmer(dimmerOpacity, 0);
            RestoreNavigation();
            IsLoading = false; active = null;
            Destroy(gameObject);
        }
        private IEnumerator FadeDimmer(float from, float to)
        {
            float elapsed = 0;
            while (elapsed < dimSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                dimmer.color = new Color(0, 0, 0, Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, elapsed / dimSeconds)));
                yield return null;
            }
            dimmer.color = new Color(0, 0, 0, to);
        }
        private IEnumerator MoveClouds(float from, float to, float duration)
        {
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                cloudMaterial.SetFloat(ProgressId, Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, elapsed / duration)));
                yield return null;
            }
            cloudMaterial.SetFloat(ProgressId, to);
        }
        private void RestoreNavigation()
        {
            foreach (var pair in navigation) if (pair.Key != null) pair.Key.sendNavigationEvents = pair.Value;
            navigation.Clear();
        }
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            RestoreNavigation();
            if (cloudMaterial != null) Destroy(cloudMaterial);
            if (active == this) { active = null; IsLoading = false; }
        }
    }
}
