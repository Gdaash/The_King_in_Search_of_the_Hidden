using UnityEngine;
using PrimeTween;
using System;
using Zenject;

namespace GUICore.Fading
{
    /// <summary>
    /// Затухание экрана
    /// </summary>
    public class FadingScreen : MonoBehaviour, IFading
    {
        public Action OnComplete { get; set; }

        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _time;
        [SerializeField] private AnimationCurve _curve;

        private Tween _currentTween;

        [ContextMenu("In")]
        public void In()
        {
            SetFading(1);

            print("Затемнение экрана");
        }

        [ContextMenu("Out")]
        public void Out()
        {
            SetFading(0);
            print("Растемнение экрана");
        }

        private void SetFading(float targetValue)
        {
            _currentTween.Stop();
            _currentTween = Tween.Alpha(_canvasGroup, targetValue, _time, _curve)
                .OnComplete(() => OnComplete?.Invoke());
        }
    }
}