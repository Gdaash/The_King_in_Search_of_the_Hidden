using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance;

    [SerializeField] private Text tooltipText; // Ссылка на текст внутри префаба
    [SerializeField] private float fadeSpeed = 5f;

    private CanvasGroup _canvasGroup;
    private Coroutine _fadeRoutine;

    private void Awake()
    {
        Instance = this;
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0; // По умолчанию невидим
    }

    public void Show(string description)
    {
        if (tooltipText != null) tooltipText.text = description;
        Fade(1f);
    }

    public void Hide()
    {
        Fade(0f);
    }

    private void Fade(float targetAlpha)
    {
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha));
    }

    private IEnumerator FadeRoutine(float target)
    {
        while (!Mathf.Approximately(_canvasGroup.alpha, target))
        {
            _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, target, Time.deltaTime * fadeSpeed);
            yield return null;
        }
    }
}
