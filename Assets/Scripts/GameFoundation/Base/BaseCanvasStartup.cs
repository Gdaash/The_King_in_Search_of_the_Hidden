using UnityEngine;

namespace GameFoundation.Base
{
    public sealed class BaseCanvasStartup : MonoBehaviour
    {
        [SerializeField] private Canvas interfaceCanvas;

        private void Awake()
        {
            if (interfaceCanvas != null) interfaceCanvas.gameObject.SetActive(true);
        }
    }
}
