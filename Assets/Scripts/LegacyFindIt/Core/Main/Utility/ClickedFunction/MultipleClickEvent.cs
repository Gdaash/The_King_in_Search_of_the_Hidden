using System;
using System.Collections;
using DeskCat.FindIt.Scripts.Core.Model;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DeskCat.FindIt.Scripts.Core.Main.Utility.ClickedFunction
{
    public class MultipleClickEvent : MonoBehaviour, IPointerClickHandler
    {
        [Header("General Settings")] public bool EnableClickLoop = true;

        [Tooltip("List of Unity events to execute on clicks.")] [NonReorderable]
        public CustomUnityEvent[] OnClickEventList;

        [Header("State Settings")] [SerializeField]
        private int _currentClickCount = 0;

        [SerializeField] private bool _canExecuteEvent = true;

        private void Update()
        {
            if (Input.touchSupported && Input.touchCount > 0)
            {
                HandleTouchInput();
            }
        }

        /// <summary>
        /// Handles touch input for triggering click events.
        /// </summary>
        private void HandleTouchInput()
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                if (IsPointerOverUI(touch.fingerId)) return;

                Ray ray = Camera.main.ScreenPointToRay(touch.position);
                if (Physics.Raycast(ray, out RaycastHit hit) && hit.transform == transform)
                {
                    HandleClick();
                }
            }
        }

        public void OnMouseDown()
        {
            if (IsPointerOverUI() || Input.touchSupported) return;

            HandleClick();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            HandleClick();
        }

        /// <summary>
        /// Handles logic for triggering the appropriate click event from the list.
        /// </summary>
        private void HandleClick()
        {
            // Prevent execution if no more events can be triggered
            if (_currentClickCount >= OnClickEventList.Length)
            {
                if (EnableClickLoop)
                {
                    _currentClickCount = 0;
                }
                else
                {
                    return;
                }
            }

            if (_canExecuteEvent)
            {
                CustomUnityEvent currentEvent = OnClickEventList[_currentClickCount];
                StartCoroutine(ExecuteEventAfterDelay(currentEvent.WaitTimer, () =>
                {
                    currentEvent?.OnClickEventList.Invoke();
                    _canExecuteEvent = false;

                    StartCoroutine(ExecuteAfterDelay(currentEvent.DelayTimer, () =>
                    {
                        _canExecuteEvent = true;
                        _currentClickCount++;
                    }));
                }));
            }
        }

        /// <summary>
        /// Resets the click count to the beginning of the event list.
        /// </summary>
        public void ResetCount()
        {
            _currentClickCount = 0;
        }

        /// <summary>
        /// Waits for a specified amount of time before invoking a function.
        /// </summary>
        /// <param name="time">Delay in seconds.</param>
        /// <param name="callback">Function to invoke after the delay.</param>
        private IEnumerator ExecuteEventAfterDelay(float time, Action callback)
        {
            yield return new WaitForSeconds(time);
            callback?.Invoke();
        }

        private IEnumerator ExecuteAfterDelay(float time, Action callback)
        {
            yield return new WaitForSeconds(time);
            callback?.Invoke();
        }

        /// <summary>
        /// Checks if the pointer is over a UI element.
        /// </summary>
        /// <param name="fingerId">Finger ID for touch input.</param>
        /// <returns>True if over a UI element, otherwise false.</returns>
        private static bool IsPointerOverUI(int fingerId = -1)
        {
            if (EventSystem.current == null) return false;

            return fingerId >= 0
                ? EventSystem.current.IsPointerOverGameObject(fingerId)
                : EventSystem.current.IsPointerOverGameObject();
        }
    }
}