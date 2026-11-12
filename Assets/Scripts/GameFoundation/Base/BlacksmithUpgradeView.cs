using UnityEngine;
using UnityEngine.UI;
using GameFoundation.Localization;

namespace GameFoundation.Base
{
    public sealed class BlacksmithUpgradeView : MonoBehaviour
    {
        [System.Serializable] public sealed class Row
        {
            public EquipmentKind equipment;
            public ResourceType resource;
            public Button button;
            public Text label, amount, level;
            public Image icon;
            public UpgradeLevelPips pips;
        }
        public Row[] rows;
        private LocalizationService language;
        private void Awake()
        {
            foreach (var row in rows) Bind(row);
        }
        internal void AddRow(Row row)
        {
            System.Array.Resize(ref rows, rows.Length + 1);
            rows[^1] = row;
            Bind(row);
        }

        private void Bind(Row row) => row.button.onClick.AddListener(() =>
        {
            EquipmentUpgrades.TryBuy(row.equipment, row.resource);
            Refresh();
        });

        private void OnEnable()
        {
            GlobalResourceManager.OnResourceChanged += ResourceChanged;
            EquipmentUpgrades.Changed += Refresh;
            language = LocalizationService.Instance;
            if (language != null) language.LanguageChanged += Refresh;
            Refresh();
        }
        private void OnDisable()
        {
            GlobalResourceManager.OnResourceChanged -= ResourceChanged;
            EquipmentUpgrades.Changed -= Refresh;
            if (language != null) language.LanguageChanged -= Refresh;
        }
        private void ResourceChanged(ResourceType resource, int amount) => Refresh();
        public void Refresh()
        {
            foreach (var row in rows)
            {
                int level = EquipmentUpgrades.Level(row.equipment), max = EquipmentUpgrades.Maximum(row.equipment);
                bool complete = level >= max, available = EquipmentUpgrades.CanBuy(row.equipment, row.resource);
                row.button.interactable = available;
                BaseDisabledButtonHint.Set(row.button, complete ? BaseDisabledButtonHint.Reason.Maximum : BaseDisabledButtonHint.Reason.Resources);
                var actionColor = row.button.GetComponent<GameFoundation.UI.ActionButtonLabelColor>();
                if (actionColor != null) actionColor.ActionAvailable = available;
                row.label.text = LaboratoryUpgradeList.Tr(complete ? "blacksmith.maximum" : "blacksmith.upgrade", complete ? "Максимум" : "Улучшить");
                string stat = row.equipment switch
                {
                    EquipmentKind.Cart => LaboratoryUpgradeList.Tr("blacksmith.speed", "Скорость"),
                    EquipmentKind.LeatherArmor or EquipmentKind.IronArmor => LaboratoryUpgradeList.Tr("unit.stat.Health", "Здоровье"),
                    _ => LaboratoryUpgradeList.Tr("blacksmith.damage", "Урон")
                };
                row.level.text = level + "/" + max + "  ·  " + stat + " +" + level * EquipmentUpgrades.PercentPerLevel(row.equipment) + "%";
                if (row.pips != null) row.pips.Display(level, max);
                row.amount.text = complete ? "" : EquipmentUpgrades.Cost(row.equipment).ToString();
                row.amount.color = available ? new Color(.55f,.85f,.35f) : new Color(.95f,.38f,.35f);
                ResourceIconSizing.Apply(row.icon, row.resource.resourceIcon);
                row.icon.gameObject.SetActive(!complete);
            }
        }
    }
}
