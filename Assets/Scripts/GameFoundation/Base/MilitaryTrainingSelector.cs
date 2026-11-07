using UnityEngine;using UnityEngine.UI;using GameFoundation.UI;
namespace GameFoundation.Base
{
    public sealed class MilitaryTrainingSelector : MonoBehaviour
    {
        [SerializeField] private MilitaryTrainingView training;
        [SerializeField] private Button swordsmenButton,spearmenButton;
        [SerializeField] private ResourceType sword,swordsman,spear,spearman;
        [SerializeField] private UnitDescriptionDefinition swordDescription,spearDescription;
        private void Awake(){swordsmenButton.onClick.AddListener(SelectSwordsmen);spearmenButton.onClick.AddListener(SelectSpearmen);}
        private void OnEnable()=>SelectSwordsmen();
        public void SelectSwordsmen(){training.SelectUnit(sword,swordsman,swordDescription);Highlight(swordsmenButton,true);Highlight(spearmenButton,false);}
        public void SelectSpearmen(){training.SelectUnit(spear,spearman,spearDescription);Highlight(swordsmenButton,false);Highlight(spearmenButton,true);}
        private static void Highlight(Button button,bool selected)
        {
            button.interactable=true;
            var controller=button.GetComponent<GameFoundation.UI.ActionButtonLabelColor>();if(controller!=null)controller.enabled=false;
            var color=selected?new Color(.55f,.85f,.54f):new Color(.94f,.91f,.82f);
            var text=button.GetComponentInChildren<Text>(true);if(text!=null)text.color=color;
            foreach(var label in button.GetComponentsInChildren<TMPro.TMP_Text>(true))label.color=color;
        }
    }
}
