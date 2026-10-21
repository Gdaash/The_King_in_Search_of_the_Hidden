using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Quests
{
    public sealed class QuestObjectiveRow : MonoBehaviour
    {
        [SerializeField] private Image resourceIcon;
        [SerializeField] private Text amount;
        [SerializeField] private Text purpose;
        [SerializeField] private Color incompleteColor = new(.95f, .91f, .8f);
        [SerializeField] private Color completeColor = new(.5f, .85f, .5f);

        public void PresentReward(Sprite icon, string value, string caption)
        {
            ResourceIconSizing.Apply(resourceIcon, icon);
            resourceIcon.enabled = icon != null;
            amount.text = value; amount.color = completeColor;
            purpose.text = GameFoundation.Localization.GameText.Get(caption);
            GetComponent<LayoutElement>().preferredHeight = Mathf.Max(44, icon != null ? icon.rect.height * 2 + 8 : 44);
        }
        public void Present(QuestDefinition.Requirement goal, int stock, bool completed, int requiredAmount = -1)
        {
            int required = requiredAmount >= 0 ? requiredAmount : goal.amount;
            ResourceIconSizing.Apply(resourceIcon, !string.IsNullOrEmpty(goal.buildingId) ? goal.buildingIcon : goal.resource != null ? goal.resource.resourceIcon : null);
            amount.text = $"{(completed ? required : Mathf.Clamp(stock, 0, required))} / {required}";
            amount.color = completed || stock >= required ? completeColor : incompleteColor;
            purpose.text = GameFoundation.Localization.GameText.Get(goal.purpose);
            var height = Mathf.Max(60, resourceIcon.sprite != null ? resourceIcon.sprite.rect.height * 2 + 8 : 60);
            GetComponent<LayoutElement>().preferredHeight = height;
        }
    }
}
