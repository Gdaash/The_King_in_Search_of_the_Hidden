using UnityEngine;

namespace GameFoundation.Saves
{
    public sealed class SaveSlotClock : MonoBehaviour
    {
        private float _elapsed;

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed >= 10f) Flush();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Flush();
        }

        private void OnApplicationQuit() => Flush();
        private void OnDestroy() => Flush();

        private void Flush()
        {
            if (_elapsed <= 0f) return;
            SaveSlotPrefs.AddPlaySeconds(_elapsed);
            _elapsed = 0f;
        }
    }
}
