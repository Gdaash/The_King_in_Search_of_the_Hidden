using GameFoundation.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    /// <summary>Explains the actual availability state without making disabled buttons clickable.</summary>
    public sealed class BaseDisabledButtonHint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        public enum Reason { Resources, Housing, Refugees, Residents, MilitaryCapacity, Weapons, Warriors, Maximum, Research, Quest, Demo, TravelUsed, PortalLevel, Busy, ForagingUsed, NoHunger, Coop, Tannery, MagicTower, ArcheryRange }
        public static readonly string[] Russian = { "Недостаточно ресурсов", "Нет свободных домов", "Нет ожидающих беженцев", "Нет доступных жителей", "Нет свободных мест для воинов", "Недостаточно оружия", "Нет воинов для разоружения", "Достигнут максимальный уровень", "Изучите больше улучшений для разблокировки", "Сначала выполните необходимое задание", "Недоступно в демо", "Сегодня переход уже использован", "Требуется улучшение портала", "Дождитесь возвращения жителей", "Поиск еды уже использован сегодня", "Еды достаточно, поиски не требуются", "Постройте курятник", "Постройте дубильню", "Постройте магическую башню", "Улучшите стрельбище" };
        public static readonly string[] English = { "Not enough resources", "No vacant housing", "No refugees are waiting", "No residents available", "No room for more warriors", "Not enough weapons", "No warriors to disarm", "Maximum level reached", "Research more upgrades to unlock this", "Complete the required quest first", "Unavailable in the demo", "Travel has already been used today", "Upgrade the portal first", "Wait for the residents to return", "Foraging has already been used today", "There is enough food; foraging is not needed", "Build a chicken coop", "Build a tannery", "Build the Magic Tower", "Upgrade the range" };
        public static string Key(Reason reason) => "base.disabled." + reason.ToString().ToLowerInvariant();
        private Button button;
        private Reason reason;
        private bool hovering;
        private string shown;
        private string actionDetail;
        private string reasonText;
        private static BaseDisabledButtonHint owner;

        public static void Set(Button button, Reason reason, string actionDetail = null, string reasonText = null)
        {
            if (button == null) return;
            var hint = button.GetComponent<BaseDisabledButtonHint>() ?? button.gameObject.AddComponent<BaseDisabledButtonHint>();
            hint.button = button; hint.reason = reason; hint.actionDetail = actionDetail; hint.reasonText = reasonText;
        }
        public void OnPointerEnter(PointerEventData _) { hovering = true; Refresh(); }
        public void OnPointerExit(PointerEventData _) { hovering = false; Hide(); }
        public void OnSelect(BaseEventData _) { hovering = true; Refresh(); }
        public void OnDeselect(BaseEventData _) { hovering = false; Hide(); }
        private void Update() { if (hovering) Refresh(); }
        private void Refresh()
        {
            if (button == null || !button.gameObject.activeInHierarchy) { Hide(); return; }
            var language = LocalizationService.Instance;
            string key = Key(reason), text = reasonText ?? language?.Get(key);
            if (string.IsNullOrEmpty(text) || text == key) text = language?.Language == "en" ? English[(int)reason] : Russian[(int)reason];
            if (button.interactable) text = actionDetail;
            else if (!string.IsNullOrEmpty(actionDetail)) text = actionDetail + "\n" + text;
            if (string.IsNullOrEmpty(text)) { Hide(); return; }
            if (TooltipManager.Instance == null) return;
            if (shown == text && owner == this) return;
            BuildingConstructionTooltip.Instance?.Hide();
            TooltipManager.Instance.Show(text, (RectTransform)transform, true);
            owner = this; shown = text;
        }
        private void Hide()
        {
            if (owner == this) { TooltipManager.Instance?.Hide(); owner = null; }
            shown = null;
        }
        private void OnDisable() { hovering = false; Hide(); }
    }
}
