using UnityEngine;

/// <summary>Shows the frame and dragon together with the return button, including its tutorial reveal.</summary>
[RequireComponent(typeof(CanvasGroup))]
public sealed class WorldReturnPanel : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Button returnButton;
    private CanvasGroup group;

    private void Awake() => group = GetComponent<CanvasGroup>();
    private void LateUpdate()
    {
        if (!group) group = GetComponent<CanvasGroup>();
        group.alpha = returnButton && returnButton.gameObject.activeSelf ? 1 : 0;
    }
}
