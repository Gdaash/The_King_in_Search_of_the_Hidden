using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameFoundation.Intro
{
    public sealed class IntroComicController : MonoBehaviour
    {
        [Serializable]
        public sealed class Slide
        {
            public GameObject root;
            public CanvasGroup[] sections;
        }

        [SerializeField] private RectTransform viewport;
        [SerializeField] private RectTransform artworkRoot;
        [SerializeField] private UnityEngine.UI.Button advanceButton;
        [SerializeField] private GameObject continueHint;
        [SerializeField] private GameObject finishHint;
        [SerializeField] private Slide[] slides;
        [SerializeField, Min(0f)] private float revealDuration = 0.2f;

        private IntroComicProgress progress;
        private Coroutine reveal;
        private CanvasGroup revealingSection;
        private Vector2 fittedSize;
        private int lastAdvanceFrame;

        public int PageIndex => progress?.PageIndex ?? 0;
        public int RevealedSections => progress?.RevealedSections ?? 0;
        public bool IsComplete => progress != null && progress.IsComplete;

        private void Awake()
        {
            if (!viewport || !artworkRoot || !advanceButton || slides == null || slides.Length == 0)
            {
                Debug.LogError("Intro comic is missing its scene references.", this);
                enabled = false;
                return;
            }
            var counts = new int[slides.Length];
            for (int i = 0; i < slides.Length; i++)
            {
                if (slides[i]?.root == null || slides[i].sections == null || slides[i].sections.Length == 0 ||
                    Array.Exists(slides[i].sections, section => section == null))
                {
                    Debug.LogError("Intro comic has an incomplete page.", this);
                    enabled = false;
                    return;
                }
                counts[i] = slides[i].sections.Length;
                slides[i].root.SetActive(false);
            }
            progress = new IntroComicProgress(counts);
            advanceButton.onClick.AddListener(Advance);
        }

        private void Start()
        {
            Time.timeScale = 1f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            lastAdvanceFrame = Time.frameCount;
            Canvas.ForceUpdateCanvases();
            FitArtwork();
            RefreshPage();
        }

        private void LateUpdate()
        {
            if (viewport.rect.size != fittedSize) FitArtwork();
        }

        private void FitArtwork()
        {
            fittedSize = viewport.rect.size;
            Vector2 artSize = artworkRoot.rect.size;
            if (artSize.x <= 0f || artSize.y <= 0f) return;
            // The individual Image RectTransforms stay at twice the source sprite dimensions.
            // Scale the shared composition root uniformly to fit any screen without cropping.
            float scale = Mathf.Min(fittedSize.x / artSize.x, fittedSize.y / artSize.y);
            artworkRoot.localScale = Vector3.one * scale;
        }

        public void Advance()
        {
            if (!isActiveAndEnabled || progress == null || progress.IsComplete || lastAdvanceFrame == Time.frameCount)
                return;
            lastAdvanceFrame = Time.frameCount;
            FinishReveal();
            progress.Advance();
            if (progress.IsComplete)
            {
                advanceButton.interactable = false;
                SceneManager.LoadScene(IntroComicFlow.Complete());
                return;
            }
            RefreshPage();
        }

        private void RefreshPage()
        {
            for (int i = 0; i < slides.Length; i++) slides[i].root.SetActive(i == progress.PageIndex);
            CanvasGroup[] sections = slides[progress.PageIndex].sections;
            for (int i = 0; i < sections.Length; i++)
            {
                bool visible = i < progress.RevealedSections;
                sections[i].gameObject.SetActive(visible);
                sections[i].alpha = visible ? 1f : 0f;
            }
            if (continueHint) continueHint.SetActive(!progress.IsLastSection);
            if (finishHint) finishHint.SetActive(progress.IsLastSection);
            revealingSection = sections[progress.RevealedSections - 1];
            if (revealDuration > 0f) reveal = StartCoroutine(Reveal(revealingSection));
        }

        private IEnumerator Reveal(CanvasGroup section)
        {
            section.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < revealDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                section.alpha = Mathf.SmoothStep(0f, 1f, elapsed / revealDuration);
                yield return null;
            }
            section.alpha = 1f;
            reveal = null;
        }

        private void FinishReveal()
        {
            if (reveal != null) StopCoroutine(reveal);
            if (revealingSection) revealingSection.alpha = 1f;
            reveal = null;
        }

        private void OnDisable() => FinishReveal();

        private void OnDestroy()
        {
            if (advanceButton) advanceButton.onClick.RemoveListener(Advance);
        }
    }
}
