using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
namespace GameFoundation.Base
{
    public static class ForagingInputLock
    {
        private static readonly List<EventSystem> suspended = new();
        public static void SetLocked(bool locked)
        {
            if (locked)
            {
                foreach (var system in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
                    if (system.enabled) { system.SetSelectedGameObject(null); system.enabled = false; suspended.Add(system); }
            }
            else
            {
                foreach (var system in suspended) if (system != null) system.enabled = true;
                suspended.Clear();
            }
        }
    }
}
