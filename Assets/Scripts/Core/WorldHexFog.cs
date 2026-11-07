using System.Collections.Generic;
using UnityEngine;

/// <summary>Persistent identity of an explored tile. Its original cover can be reused without respawning content.</summary>
public sealed class WorldHexFog : MonoBehaviour
{
    private static readonly HashSet<WorldHexFog> tiles = new();
    public HexBlocker Cover { get; private set; }
    public EyeHexSealer Owner { get; private set; }
    public bool IsCapturing { get; private set; }
    public bool IsClosed { get; private set; }
    public bool IsFading { get; private set; }
    public bool IsOpen => !IsClosed && Cover != null && Cover.IsExplored;
    public Vector2 Center => transform.position;
    public const float HexRadius = 1.65f;
    private readonly List<ResourceRequester> workplaces = new();
    private readonly Dictionary<SpriteRenderer, bool> hiddenContent = new();
    private readonly Dictionary<Canvas, bool> hiddenCanvases = new();
    public static IEnumerable<WorldHexFog> Tiles => tiles;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() => tiles.Clear();
    private void OnEnable() => tiles.Add(this);
    private void OnDisable() => tiles.Remove(this);

    public static WorldHexFog Remember(HexBlocker cover)
    {
        var root = new GameObject("Explored hex state");
        root.transform.SetParent(cover.transform.parent, false);
        root.transform.position = cover.transform.position;
        var state = root.AddComponent<WorldHexFog>();
        state.Cover = cover;
        return state;
    }

    public static WorldHexFog At(Vector2 point)
    {
        WorldHexFog best = null;
        float distance = 1.5f * 1.5f;
        foreach (var tile in tiles)
        {
            if (tile == null || tile.Cover == null) continue;
            float candidate = (tile.Center - point).sqrMagnitude;
            if (candidate < distance) { best = tile; distance = candidate; }
        }
        return best;
    }
    public static bool BlocksWork(Vector2 point)
    {
        var tile = At(point);
        return tile != null && (tile.IsCapturing || tile.IsClosed);
    }
    public static bool BlocksOpening(Vector2 point)
    {
        var tile = At(point);
        return tile != null && (tile.IsCapturing || tile.IsFading);
    }
    public bool TryClaim(EyeHexSealer eye)
    {
        if (!IsOpen || (Owner != null && Owner != eye)) return false;
        Owner = eye;
        return true;
    }
    public void BeginCapture(EyeHexSealer eye)
    {
        if (Owner != eye || !IsOpen || IsCapturing) return;
        IsCapturing = true;
        workplaces.Clear();
        foreach (var job in ResourceRequester.ActiveInstances)
            if (job != null && At(job.WorkerPosition) == this)
            {
                workplaces.Add(job);
                job.ResetWorkForFog();
            }
    }
    public void CancelCapture(EyeHexSealer eye)
    {
        if (Owner != eye) return;
        Owner = null;
        if (IsClosed) return;
        IsCapturing = false;
        ResumeWork();
    }
    public void Seal(EyeHexSealer eye)
    {
        if (Owner != eye || !IsCapturing || IsClosed) return;
        // Only residents assigned to a workplace die; passing units are never queried.
        foreach (var job in ResourceRequester.ActiveInstances)
            if (job != null && At(job.WorkerPosition) == this)
            {
                job.GetComponent<ResourceWorkforce>()?.LoseResidentsToFog();
                // Conceal existing content visually; never disable its production hierarchy.
                foreach (var sprite in job.transform.root.GetComponentsInChildren<SpriteRenderer>(true))
                    if (!hiddenContent.ContainsKey(sprite)) { hiddenContent.Add(sprite, sprite.forceRenderingOff); sprite.forceRenderingOff = true; }
                foreach (var canvas in job.transform.root.GetComponentsInChildren<Canvas>(true))
                    if (!hiddenCanvases.ContainsKey(canvas)) { hiddenCanvases.Add(canvas, canvas.enabled); canvas.enabled = false; }
            }
        IsClosed = IsFading = true;
        IsCapturing = false;
        Cover.RestoreFogCover();
        foreach (var lights in Object.FindObjectsByType<GameFoundation.MetaProgression.WorldFlashlightAvailability>(FindObjectsSortMode.None))
            if (lights.gameObject.scene == gameObject.scene) lights.ReleaseForFog(Center);
    }
    public void FinishFade() { IsFading = false; Owner = null; }
    public void Reopened()
    {
        IsClosed = IsFading = IsCapturing = false;
        Owner = null;
        foreach (var sprite in hiddenContent) if (sprite.Key != null) sprite.Key.forceRenderingOff = sprite.Value;
        foreach (var canvas in hiddenCanvases) if (canvas.Key != null) canvas.Key.enabled = canvas.Value;
        hiddenContent.Clear(); hiddenCanvases.Clear();
        ResumeWork();
    }
    private void ResumeWork()
    {
        foreach (var job in workplaces)
            if (job != null && job.isActiveAndEnabled) { job.TryStartCrystalCycle(); job.UpdateIndicator(); }
        OrderManager.Instance?.ForceUpdateOrders();
    }
}
