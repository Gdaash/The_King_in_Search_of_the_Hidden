using System.Collections;
using System.Collections.Generic;
using GameFoundation.MetaProgression;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameFoundation.Saves
{
    /// <summary>Unload old gameplay before clearing its slot, so teardown cannot restore old progress.</summary>
    public sealed class CurrentSaveReset : MonoBehaviour
    {
        private static bool running;
        public static void Reload()
        {
            if (running) return;
            running = true;
            var runner = new GameObject("Current save reset").AddComponent<CurrentSaveReset>();
            DontDestroyOnLoad(runner.gameObject);
            runner.StartCoroutine(runner.ResetAndReload());
        }

        private IEnumerator ResetAndReload()
        {
            Scene original = SceneManager.GetActiveScene();
            string scenePath = original.path;
            int slot = SaveSlotPrefs.SelectedSlot;
            GameSaveService.WritesSuspended = true;
            Time.timeScale = 1f;
            Scene empty = SceneManager.CreateScene("Save reset transition");
            SceneManager.SetActiveScene(empty);
            var roots = new HashSet<GameObject>();
            if (GlobalResourceManager.Instance != null) roots.Add(GlobalResourceManager.Instance.transform.root.gameObject);
            if (DayCycleService.Instance != null) roots.Add(DayCycleService.Instance.transform.root.gameObject);
            if (MetaProgressionService.Instance != null) roots.Add(MetaProgressionService.Instance.transform.root.gameObject);
            foreach (var root in roots) Destroy(root);
            yield return SceneManager.UnloadSceneAsync(original);
            yield return null;
            GameSaveService.ResetSlot(slot);
            DayResourceLedger.ResetForSlot();
            GameSaveService.WritesSuspended = false;
            yield return SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
            running = false;
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            GameSaveService.WritesSuspended = false;
            running = false;
        }
    }
}
