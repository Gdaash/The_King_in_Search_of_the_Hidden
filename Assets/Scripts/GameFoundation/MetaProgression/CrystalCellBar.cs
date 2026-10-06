using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.MetaProgression
{
    /// <summary>The same authored cell row is used in the HUD and above the hovered hex.</summary>
    public sealed class CrystalCellBar : MonoBehaviour
    {
        [System.Serializable] public sealed class CellView
        {
            public GameObject root;
            public Image frame;
            public Image fill;
        }
        [SerializeField] private CellView[] cells;
        [SerializeField] private Color readyColor = new(.45f, .82f, 1f, 1f);
        [SerializeField] private Color chargingColor = new(.38f, .3f, .62f, 1f);
        [SerializeField] private Color activeColor = new(1f, .8f, .25f, 1f);
        [SerializeField] private Color waitingColor = new(.72f, .5f, 1f, 1f);
        private WorldFlashlightAvailability crystal;
        public float WidthForCount(int count)
        {
            var layout = GetComponent<HorizontalLayoutGroup>();
            float width = layout != null ? layout.padding.horizontal : 0;
            int visible = Mathf.Min(count, cells.Length);
            for (int i = 0; i < visible; i++) width += ((RectTransform)cells[i].root.transform).rect.width;
            return width + Mathf.Max(0, visible - 1) * (layout != null ? layout.spacing : 0);
        }
        public void Bind(WorldFlashlightAvailability owner) { crystal = owner; Refresh(); }
        private void LateUpdate() => Refresh();
        public void Refresh()
        {
            if (crystal == null) return;
            for (int i = 0; i < cells.Length; i++)
            {
                var view = cells[i];
                bool visible = i < crystal.CellCount;
                if (view.root.activeSelf != visible) view.root.SetActive(visible);
                if (!visible) continue;
                var state = crystal.State(i);
                view.frame.color = state switch
                {
                    WorldFlashlightAvailability.CellState.Ready => readyColor,
                    WorldFlashlightAvailability.CellState.Charging => chargingColor,
                    WorldFlashlightAvailability.CellState.WaitingForResources => waitingColor,
                    _ => activeColor
                };
                view.fill.color = state == WorldFlashlightAvailability.CellState.Ready ? Color.white : chargingColor;
                view.fill.fillAmount = crystal.Charge(i);
            }
        }
    }
}
