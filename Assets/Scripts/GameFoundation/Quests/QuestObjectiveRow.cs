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

        public void Present(QuestDefinition.Requirement goal, int stock, bool completed)
        {
            ResourceIconSizing.Apply(resourceIcon, goal.resource != null ? goal.resource.resourceIcon : null);
            amount.text = $"{(completed ? goal.amount : Mathf.Clamp(stock, 0, goal.amount))} / {goal.amount}";
            amount.color = completed || stock >= goal.amount ? completeColor : incompleteColor;
            purpose.text = goal.purpose;
            var height = Mathf.Max(60, resourceIcon.sprite != null ? resourceIcon.sprite.rect.height * 2 + 8 : 60);
            GetComponent<LayoutElement>().preferredHeight = height;
        }
    }
}
