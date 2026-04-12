using DeskCat.FindIt.Scripts.Core.Main.Utility.DragObj;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace DeskCat.FindIt.Scripts.Core.Main.Utility.Region
{
    public class DropRegion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string RegionName;
        public UnityEvent DropRegionEvent;

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Если галочка скрипта снята — ничего не делаем
            if (!enabled) return;

            CurrentDragInfo.CurrentDropRegion = this;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Аналогично при выходе
            if (!enabled) return;

            // Сбрасываем регион только если мы сами являемся текущим регионом
            if (CurrentDragInfo.CurrentDropRegion == this)
            {
                CurrentDragInfo.CurrentDropRegion = null;
            }
        }

        // На всякий случай сбрасываем регион, если скрипт выключили в процессе работы
        private void OnDisable()
        {
            if (CurrentDragInfo.CurrentDropRegion == this)
            {
                CurrentDragInfo.CurrentDropRegion = null;
            }
        }
    }
}