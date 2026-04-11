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
            CurrentDragInfo.CurrentDropRegion = this;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            CurrentDragInfo.CurrentDropRegion = null;
        }
    }
}