using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class PortalFrameAnimator : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField, Min(1f)] private float framesPerSecond = 12f;

    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        UpdateFrame();
    }

    private void Update()
    {
        UpdateFrame();
    }

    private void UpdateFrame()
    {
        if (_spriteRenderer == null || frames == null || frames.Length == 0)
            return;

        int frameIndex = Mathf.FloorToInt(Time.time * framesPerSecond) % frames.Length;
        _spriteRenderer.sprite = frames[frameIndex];
    }
}
