using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameFoundation.MetaProgression
{
    public sealed class RunSceneRouter : MonoBehaviour
    {
        [SerializeField] private string baseScene = "Base";
        [SerializeField] private string runScene = "World";
        public bool CanEnterRun => PortalCloudTransition.CanLoad(runScene);
        public void EnterRun()
        {
            if (!CanEnterRun) return;
            GameFoundation.Saves.SaveSlotPrefs.BeginRunCheckpoint();
            DayResourceLedger.BeginRun();
            PortalCloudTransition.Load(runScene);
        }
        public void ReturnToBase()
        {
            if (!PortalCloudTransition.CanLoad(baseScene)) return;
            var military = FindAnyObjectByType<WorldMilitaryDeploymentController>();
            if (military != null) military.FinalizeSurvivors();
            DayResourceLedger.EndRun(); GameFoundation.Saves.SaveSlotPrefs.CompleteRunCheckpoint(); PortalCloudTransition.Load(baseScene);
        }
    }
}
