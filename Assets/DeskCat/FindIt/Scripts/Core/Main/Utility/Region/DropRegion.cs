using DeskCat.FindIt.Scripts.Core.Main.Utility.DragObj;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

namespace DeskCat.FindIt.Scripts.Core.Main.Utility.Region
{
    public class DropRegion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [global::System.Serializable]
        public class RegionItem
        {
            public string regionName; 
            public int regionID;      
            public bool isActive = true;
            public UnityEvent dropRegionEvent;
        }

        [Header("Список регионов")]
        public List<RegionItem> regions = new List<RegionItem>();

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!enabled) return;
            CurrentDragInfo.CurrentDropRegion = this;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (CurrentDragInfo.CurrentDropRegion == this)
            {
                CurrentDragInfo.CurrentDropRegion = null;
            }
        }

        private void OnDisable()
        {
            if (CurrentDragInfo.CurrentDropRegion == this)
            {
                CurrentDragInfo.CurrentDropRegion = null;
            }
        }

        /// <summary>
        /// Вызывается из DragObj.
        /// </summary>
        public void ExecuteRegionEvent(string draggedItemName)
        {
            if (!enabled) return;

            foreach (var item in regions)
            {
                if (item.isActive && item.regionName == draggedItemName)
                {
                    item.dropRegionEvent?.Invoke();
                    return; 
                }
            }
        }

        // --- МЕТОДЫ УПРАВЛЕНИЯ ---

        public void EnableRegionByID(int id) => SetRegionActiveByID(id, true);
        public void DisableRegionByID(int id) => SetRegionActiveByID(id, false);

        public void SetRegionActiveByID(int id, bool state)
        {
            var region = regions.FirstOrDefault(r => r.regionID == id);
            if (region != null) 
            {
                region.isActive = state;
            }
        }

        public void SetRegionActiveByName(string name, bool state)
        {
            foreach (var r in regions.Where(r => r.regionName == name))
            {
                r.isActive = state;
            }
        }
    }
}