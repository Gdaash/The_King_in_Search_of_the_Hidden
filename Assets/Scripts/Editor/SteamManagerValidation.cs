#if UNITY_EDITOR
using UnityEngine;
public static class SteamManagerValidation
{
    public static string Check()
    {
        var manager=Object.FindFirstObjectByType<Steamworks.SteamManager>();
        if(manager==null)return "No live SteamManager";
        if(!Steamworks.CallbackDispatcher.IsInitialized)return "Steam dispatcher unavailable; callback calls are guarded";
        Steamworks.SteamAPI.RunCallbacks();
        return "Steam dispatcher initialized; RunCallbacks completed successfully";
    }
}
#endif
