using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.MetaProgression
{
    public sealed class CrystalChargeHud : MonoBehaviour
    {
        [SerializeField] private WorldFlashlightAvailability crystal;
        [SerializeField] private RectTransform cellsRect;
        [SerializeField] private CrystalCellBar cellBar;
        [SerializeField] private Button rechargeButton;
        [SerializeField] private Image resourceIcon;
        [SerializeField] private TMP_Text price;
        [SerializeField, Min(0)] private float minimumCellsWidth = 112;
        private int lastCount = -1;
        private void Start()
        {
            if (crystal == null) crystal = FindFirstObjectByType<WorldFlashlightAvailability>();
            if (cellBar != null) cellBar.Bind(crystal);
            if (rechargeButton != null) rechargeButton.onClick.AddListener(Recharge);
            if (crystal != null)
            {
                ResourceIconSizing.Apply(resourceIcon, crystal.RechargeResource != null ? crystal.RechargeResource.resourceIcon : null);
                price.text = crystal.RechargeCost.ToString();
            }
        }
        private void OnDestroy() { if (rechargeButton != null) rechargeButton.onClick.RemoveListener(Recharge); }
        private void Recharge() => crystal?.RechargeAll();
        private void Update()
        {
            if (crystal == null) return;
            if (lastCount != crystal.CellCount)
            {
                lastCount = crystal.CellCount;
                float width = Mathf.Max(minimumCellsWidth, cellBar.WidthForCount(lastCount));
                cellsRect.sizeDelta = new Vector2(width, cellsRect.sizeDelta.y);
                ((RectTransform)rechargeButton.transform).anchoredPosition = new Vector2(width + 18, 0);
                ((RectTransform)transform).sizeDelta = new Vector2(width + 18 + 238, 60);
                LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform.parent);
            }
            rechargeButton.interactable = crystal.CanRecharge;
        }
    }
}

