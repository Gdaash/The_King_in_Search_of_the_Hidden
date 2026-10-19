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
            DayResourceLedger.BeginRun();
            PortalCloudTransition.Load(runScene);
        }
        public void ReturnToBase() { DayResourceLedger.EndRun(); SceneManager.LoadScene(baseScene); }
    }
}
