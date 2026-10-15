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
            // Mandatory level choice covers HUD, tooltips and reward-flight overlays.
            var canvas=GetComponent<Canvas>();
            if(canvas!=null){canvas.overrideSorting=true;canvas.sortingOrder=32760;}
            transform.SetAsLastSibling();
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
        private string DeliveryName(PortalTowerBalance.DeliveryMethod method) => method switch
        {
            PortalTowerBalance.DeliveryMethod.Projectile => English ? "Projectile" : "Снаряд",
            PortalTowerBalance.DeliveryMethod.Area => English ? "Area" : "По области",
            _ => English ? "Instant hit" : "Мгновенный удар"
        };
        private string DamageName(DamageType type) => type switch
        {
            DamageType.Physical => English ? "Physical" : "Физический",
            DamageType.Fire => English ? "Fire" : "Огонь",
            DamageType.Ice => English ? "Cold" : "Морозный",
            DamageType.Electric => English ? "Electric" : "Электричество",
            _ => English ? "Magic" : "Магический"
        };
        private string ScopeName(PortalTowerBalance.Upgrade upgrade) => upgrade.scope switch
        {
            PortalTowerBalance.UpgradeScope.DeliveryMethod => DeliveryName(upgrade.deliveryMethod),
            PortalTowerBalance.UpgradeScope.DamageType => DamageName(upgrade.damageType),
            PortalTowerBalance.UpgradeScope.Portal => English ? "Portal" : "Портал",
            _ => English ? "All weapons" : "Всё оружие"
        };
        private void Refresh()
        {
            heading.text = English ? "PORTAL TOWER — LEVEL " + progression.ChoiceLevel : GameFoundation.Localization.GameText.Get("БАШНЯ ПОРТАЛА — УРОВЕНЬ ") + progression.ChoiceLevel;
            hint.text = progression.WeaponCount == 0
                ? (English ? "Choose your first weapon" : GameFoundation.Localization.GameText.Get("Выберите первое оружие"))
                : (English ? "Choose one upgrade to continue" : GameFoundation.Localization.GameText.Get("Выберите одно улучшение, чтобы продолжить"));
            for(int i=0;i<cards.Length;i++)
            {
                var c=cards[i]; bool visible=i<progression.Offers.Count;
                c.button.transform.parent.gameObject.SetActive(visible);
                if(!visible)continue;
                var u=progression.Offers[i];
                c.title.text=English?u.englishTitle:u.title;
                c.description.text=English?u.englishDescription:u.description;
                c.description.fontSize = 20;
                c.description.horizontalOverflow = HorizontalWrapMode.Wrap;
                c.icon.sprite=u.icon; c.icon.enabled=u.icon!=null;
                if(u.icon!=null)c.icon.rectTransform.sizeDelta=u.icon.rect.size*Mathf.Min(2,64f/Mathf.Max(u.icon.rect.width,u.icon.rect.height));
                var w=progression.Balance.FindWeapon(u.weapon);
                if(u.effect==PortalTowerBalance.Effect.UnlockWeapon && w!=null)
                {
                    c.description.text += $"\n{(English ? "Delivery" : "Способ")}: {DeliveryName(w.deliveryMethod)}\n{(English ? "Damage" : "Урон")}: {DamageName(w.damageType)}";
                    float damage = w.damage * progression.Multiplier(w.weapon, PortalTowerBalance.Effect.Damage);
                    float interval = w.cooldown / progression.Multiplier(w.weapon, PortalTowerBalance.Effect.AttackSpeed);
                    if (w.damageTickInterval > 0)
                    {
                        float lifetime = w.duration * progression.Multiplier(w.weapon, PortalTowerBalance.Effect.Duration);
                        c.description.text += $"\n{damage:0.##} / {w.damageTickInterval:0.##} {(English?"s":"с")} · {(English?"Life":"Жизнь")}: {lifetime:0.##} {(English?"s":"с")}";
                        if (w.slowFraction > 0)
                        {
                            float slow = Mathf.Min(.8f, w.slowFraction * progression.Multiplier(w.weapon, PortalTowerBalance.Effect.SlowStrength));
                            c.description.text += English ? $"\n−{slow*100:0}% speed · every {interval:0.##} s" : $"\n−{slow*100:0}% скорости · раз в {interval:0.##} с";
                        }
                    }
                    else
                    {
                        float dps = damage * progression.Count(w.weapon) / Mathf.Max(.05f, interval);
                        c.description.text += $"\n{damage:0.##} / {interval:0.##} {(English?"s":"с")} · DPS: {dps:0.##}";
                    }
                }
                c.category.text=u.effect==PortalTowerBalance.Effect.UnlockWeapon
                    ? (English?"NEW WEAPON":GameFoundation.Localization.GameText.Get("НОВОЕ ОРУЖИЕ"))
                    : ScopeName(u) + " · " + (progression.Rank(u.id)+1);
                c.button.interactable=progression.CanPurchase(u);
                c.button.GetComponentInChildren<Text>().text=English?"Choose":"Выбрать";
            }
        }
    }
}
