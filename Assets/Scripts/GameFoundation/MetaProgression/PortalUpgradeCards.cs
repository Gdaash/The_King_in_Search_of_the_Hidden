using System;
using GameFoundation.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.MetaProgression
{
    public sealed class PortalUpgradeCards : MonoBehaviour
    {
        [Serializable] public sealed class Card
        {
            public Button button;
            public Image icon;
            public Text title, description, category;
        }
        public PortalTowerProgression progression;
        public Text heading, hint;
        public Card[] cards;
        private LocalizationService language;
        private void OnEnable()
        {
            progression.Changed += Refresh;
            language = LocalizationService.Instance;
            if(language!=null) language.LanguageChanged += Refresh;
            Refresh();
        }
        private void OnDisable()
        {
            progression.Changed -= Refresh;
            if(language!=null) language.LanguageChanged -= Refresh;
        }
        public void Choose(int index)
        {
            if(index>=0 && index<progression.Offers.Count) progression.Purchase(progression.Offers[index].id);
        }
        private bool English => language!=null && language.Language == "en";
        private void Refresh()
        {
            heading.text = English ? "PORTAL TOWER — LEVEL " + progression.ChoiceLevel : "БАШНЯ ПОРТАЛА — УРОВЕНЬ " + progression.ChoiceLevel;
            hint.text = progression.WeaponCount == 0
                ? (English ? "Choose your first weapon" : "Выберите первое оружие")
                : (English ? "Choose one upgrade to continue" : "Выберите одно улучшение, чтобы продолжить");
            for(int i=0;i<cards.Length;i++)
            {
                var c=cards[i]; bool visible=i<progression.Offers.Count;
                c.button.transform.parent.gameObject.SetActive(visible);
                if(!visible)continue;
                var u=progression.Offers[i];
                c.title.text=English?u.englishTitle:u.title;
                c.description.text=English?u.englishDescription:u.description;
                c.icon.sprite=u.icon; c.icon.enabled=u.icon!=null;
                if(u.icon!=null)c.icon.rectTransform.sizeDelta=u.icon.rect.size*2;
                var w=progression.Balance.FindWeapon(u.weapon);
                c.category.text=u.effect==PortalTowerBalance.Effect.UnlockWeapon
                    ? (English?"NEW WEAPON":"НОВОЕ ОРУЖИЕ")
                    : (w!=null?(English?w.englishTitle:w.title):(English?"Portal":"Портал")) + " · " + (progression.Rank(u.id)+1);
                c.button.interactable=progression.CanPurchase(u);
                c.button.GetComponentInChildren<Text>().text=English?"Choose":"Выбрать";
            }
        }
    }
}
