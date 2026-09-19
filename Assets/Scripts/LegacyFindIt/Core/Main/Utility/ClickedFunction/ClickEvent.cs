using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace DeskCat.FindIt.Scripts.Core.Main.Utility.ClickedFunction
{
    public class ClickEvent : MonoBehaviour, IPointerClickHandler
    {
        [Header("Event triggered on click")] 
        public UnityEvent OnClickEvent;
        
        private void Update()
        {
            if (Input.touchSupported && Input.touchCount > 0)
            {
                HandleTouchInput();
            }
        }
        
        private void HandleTouchInput()
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase != TouchPhase.Began) return;
            if (IsPointerOverUIObject(touch.fingerId)) return;
            
            Ray ray = Camera.main.ScreenPointToRay(touch.position);
            if (Physics.Raycast(ray, out RaycastHit hit) && hit.transform == transform)
            {
                OnClickEvent?.Invoke();
            }
        }
        
        private void OnMouseDown()
        {
            if (!IsPointerOverUIObject() && !Input.touchSupported)
            {
                OnClickEvent?.Invoke();
            }
        }


        public void OnPointerClick(PointerEventData eventData)
        {
            OnClickEvent?.Invoke();
        }
        
        private static bool IsPointerOverUIObject(int fingerId = -1)
        {
            if (EventSystem.current == null) return false;

            return fingerId >= 0
                ? EventSystem.current.IsPointerOverGameObject(fingerId)
                : EventSystem.current.IsPointerOverGameObject();
        }
    }
}