using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameFoundation.MetaProgression
{
    public sealed class RunSceneRouter : MonoBehaviour
    {
        [SerializeField] private string baseScene = "Base";
        [SerializeField] private string runScene = "World";
        public void EnterRun() { DayResourceLedger.BeginRun(); SceneManager.LoadScene(runScene); }
        public void ReturnToBase() { DayResourceLedger.EndRun(); SceneManager.LoadScene(baseScene); }
    }
}
