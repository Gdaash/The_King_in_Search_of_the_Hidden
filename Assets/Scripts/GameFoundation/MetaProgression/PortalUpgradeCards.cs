using System;
using System.Collections.Generic;
using System.Globalization;
using GameFoundation.Localization;
using GameFoundation.UI;
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
        public Button rerollButton;
        public Text rerollLabel;
        [SerializeField] private Text rerollAmount;
        public GameObject rerollTooltip;
        public Text rerollTooltipText;
        public void Reroll()
        {
            if (progression.UpgradeCrystalChoice.HasValue) progression.DeclineUpgradeCrystal();
            else progression.Reroll();
        }
        public void ShowRerollTooltip() { if (rerollTooltip != null && !progression.UpgradeCrystalChoice.HasValue) rerollTooltip.SetActive(true); }
        public void HideRerollTooltip() { if (rerollTooltip != null) rerollTooltip.SetActive(false); }
        private LocalizationService language;
        private Vector2[] cardPositions;
        private Color[] cardColors, cardTitleColors;
        private Vector2[] cardTitlePositions, cardTitleSizes, cardDescriptionPositions;
        private Image leftHeadingCrystal, rightHeadingCrystal;
        private ActionButtonLabelColor declineLabelColor;
        private bool normalButtonNegative;
        private RectTransform panel;
        private Image crystalIcon;
        private GameObject rerollCostIcon;
        private Vector2 normalHintPosition, normalButtonPosition, normalButtonLabelPosition;
        private TextAnchor normalButtonLabelAlignment;
        private Vector2 normalPanelSize, normalHeadingPosition, normalHeadingSize;
        private void Awake()
        {
            panel = heading.rectTransform.parent as RectTransform;
            normalPanelSize = panel.sizeDelta;
            normalHeadingPosition = heading.rectTransform.anchoredPosition;
            normalHeadingSize = heading.rectTransform.sizeDelta;
            normalHintPosition = hint.rectTransform.anchoredPosition;
            if (rerollButton != null)
            {
                normalButtonPosition = ((RectTransform)rerollButton.transform).anchoredPosition;
                normalButtonLabelPosition = rerollLabel.rectTransform.anchoredPosition;
                normalButtonLabelAlignment = rerollLabel.alignment;
                rerollCostIcon = rerollButton.transform.Find("Crystal cost")?.gameObject;
                declineLabelColor = rerollButton.GetComponent<ActionButtonLabelColor>();
                if (declineLabelColor != null) normalButtonNegative = declineLabelColor.Negative;
            }
            cardPositions = new Vector2[cards.Length];
            cardColors = new Color[cards.Length];
            cardTitleColors = new Color[cards.Length];
            cardTitlePositions = new Vector2[cards.Length];
            cardTitleSizes = new Vector2[cards.Length];
            cardDescriptionPositions = new Vector2[cards.Length];
            for (int i = 0; i < cards.Length; i++)
            {
                var card = (RectTransform)cards[i].button.transform.parent;
                cardPositions[i] = card.anchoredPosition;
                cardColors[i] = card.GetComponent<Image>().color;
                cardTitleColors[i] = cards[i].title.color;
                cardTitlePositions[i] = cards[i].title.rectTransform.anchoredPosition;
                cardTitleSizes[i] = cards[i].title.rectTransform.sizeDelta;
                cardDescriptionPositions[i] = cards[i].description.rectTransform.anchoredPosition;
            }
        }
        private string Localized(string key, string fallback)
        {
            string value = LocalizationService.Instance?.Get(key);
            return string.IsNullOrEmpty(value) || value == key ? fallback : value;
        }
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
            HideRerollTooltip();
            progression.Changed -= Refresh;
            if(language!=null) language.LanguageChanged -= Refresh;
        }
        public void Choose(int index)
        {
            if (progression.UpgradeCrystalChoice.HasValue)
            {
                progression.ChooseCrystalUpgrade(index);
                return;
            }
            if(index>=0 && index<progression.Offers.Count) progression.Purchase(progression.Offers[index].id);
        }
        private bool English => language!=null && language.Language == "en";
        private string DeliveryName(PortalTowerBalance.DeliveryMethod method) => method switch
        {
            PortalTowerBalance.DeliveryMethod.Projectile => English ? "Projectile" : "Снаряд",
            PortalTowerBalance.DeliveryMethod.Area => English ? "Area" : "По области",
            _ => English ? "Instant hit" : "Мгновенный удар"
        };
        private string ScopeName(PortalTowerBalance.Upgrade upgrade) => upgrade.scope switch
        {
            PortalTowerBalance.UpgradeScope.DeliveryMethod => DeliveryName(upgrade.deliveryMethod),
            PortalTowerBalance.UpgradeScope.DamageType => English ? "All weapons" : "Всё оружие",
            PortalTowerBalance.UpgradeScope.Portal => English ? "Portal" : "Портал",
            _ => English ? "All weapons" : "Всё оружие"
        };
        private void Refresh()
        {
            if (progression.UpgradeCrystalChoice is PortalTowerProgression.UpgradeCrystal crystalQuality)
            {
                ShowUpgradeCrystal(crystalQuality);
                return;
            }
            if (crystalIcon != null) crystalIcon.gameObject.SetActive(false);
            if (leftHeadingCrystal != null) leftHeadingCrystal.gameObject.SetActive(false);
            if (rightHeadingCrystal != null) rightHeadingCrystal.gameObject.SetActive(false);
            hint.rectTransform.anchoredPosition = normalHintPosition;
            bool lightReward = progression.IsGuaranteedLightChoice;
            if (hint != null) hint.gameObject.SetActive(!lightReward);
            if (rerollButton != null)
            {
                ((RectTransform)rerollButton.transform).anchoredPosition = normalButtonPosition;
                rerollLabel.rectTransform.anchoredPosition = normalButtonLabelPosition;
                rerollLabel.alignment = normalButtonLabelAlignment;
                if (declineLabelColor != null) declineLabelColor.Negative = normalButtonNegative;
                if (rerollAmount != null) rerollAmount.gameObject.SetActive(true);
                if (rerollCostIcon != null) rerollCostIcon.SetActive(true);
                rerollButton.gameObject.SetActive(!lightReward);
                if (lightReward) HideRerollTooltip();
                rerollButton.interactable = progression.CanReroll;
                rerollLabel.text = English ? "Reroll" : GameText.Get("Переброс");
                if (rerollAmount != null) rerollAmount.text = progression.RerollCost.ToString();
                rerollTooltipText.text = English
                    ? "You borrow crystals from the next level. Experience will not accumulate until this debt is repaid."
                    : "Вы берёте кристаллы в долг из будущего уровня. Пока этот долг не отработан, опыт уровня не будет копиться.";
            }
            heading.gameObject.SetActive(true);
            heading.text = string.Format(Localized("world.portal.level_heading", "БАШНЯ ПОРТАЛА – УРОВЕНЬ {0}"), progression.ChoiceLevel);
            hint.text = progression.IsInitialWeaponChoice
                ? (English ? "Choose your first weapon" : GameFoundation.Localization.GameText.Get("Выберите первое оружие"))
                : (English ? "Choose one upgrade to continue" : GameFoundation.Localization.GameText.Get("Выберите одно улучшение, чтобы продолжить"));
            for(int i=0;i<cards.Length;i++)
            {
                var c=cards[i]; bool visible=i<progression.Offers.Count;
                c.button.transform.parent.GetComponent<Image>().color = cardColors[i];
                c.title.color = cardTitleColors[i];
                c.title.rectTransform.anchoredPosition = cardTitlePositions[i];
                c.title.rectTransform.sizeDelta = cardTitleSizes[i];
                c.description.rectTransform.anchoredPosition = cardDescriptionPositions[i];
                c.button.transform.parent.gameObject.SetActive(visible);
                if (cardPositions != null)
                    ((RectTransform)c.button.transform.parent).anchoredPosition = lightReward
                        ? new Vector2(0, cardPositions[i].y) : cardPositions[i];
                if(!visible)continue;
                var u=progression.Offers[i];
                c.title.text=English?u.englishTitle:u.title;
                c.description.text=English?u.englishDescription:u.description;
                c.description.fontSize = 22;
                c.description.horizontalOverflow = HorizontalWrapMode.Wrap;
                c.icon.sprite=u.icon; c.icon.enabled=u.icon!=null;
                c.icon.material=DamagePalette.IconMaterial(u.icon);
                if(u.icon!=null)c.icon.rectTransform.sizeDelta=u.icon.rect.size*(96f/Mathf.Max(u.icon.rect.width,u.icon.rect.height));
                c.category.gameObject.SetActive(!lightReward);
                if (lightReward)
                {
                    c.title.text = Localized("world.portal.light_reward_title", "Дополнительный рабочий луч");
                    c.description.text = Localized("world.portal.light_reward_description", "Ещё один луч для открытия гексов, добычи ресурсов и обороны.");
                    c.button.interactable = progression.CanPurchase(u);
                    c.button.GetComponentInChildren<Text>().text = "OK";
                    continue;
                }
                var w=progression.Balance.FindWeapon(u.weapon);
                if(u.effect==PortalTowerBalance.Effect.UnlockWeapon && w!=null)
                {
                    c.description.text = $"{(English ? "Delivery" : "Способ")}: {DeliveryName(w.deliveryMethod)}\n{GameFoundation.Combat.CombatEffectText.Description(w.damageType)}";
                    float damage = w.damage * progression.Multiplier(w.weapon, PortalTowerBalance.Effect.Damage);
                    float interval = progression.AttackInterval(w.weapon);
                    if (w.HasTicks)
                    {
                        float lifetime = w.duration * progression.Multiplier(w.weapon, PortalTowerBalance.Effect.Duration);
                        c.description.text += $"\n{damage:0.##} / {progression.TickInterval(w.weapon):0.##} {(English?"s":"с")}{(w.HasDuration ? $" · {(English ? "Life" : "Жизнь")}: {lifetime:0.##} {(English ? "s" : "с")}" : "")}";
                        if (w.slowFraction > 0)
                        {
                            float slow = Mathf.Min(.8f, w.slowFraction * progression.Multiplier(w.weapon, PortalTowerBalance.Effect.SlowStrength));
                            c.description.text += English ? $"\n−{slow*100:0}% move/attack · {interval:0.##} s" : $"\n−{slow*100:0}% движ./атак · {interval:0.##} с";
                        }
                    }
                    else
                    {
                        float dps = damage * progression.Count(w.weapon) / Mathf.Max(.05f, interval);
                        c.description.text += $"\n{damage:0.##} / {interval:0.##} {(English?"s":"с")} · DPS: {dps:0.##}";
                    }
                }
                bool weaponOffer = u.effect == PortalTowerBalance.Effect.UnlockWeapon;
                string quality = u.quality switch
                {
                    PortalTowerBalance.Quality.Rare => English ? "Rare" : "Редкий",
                    PortalTowerBalance.Quality.Legendary => English ? "Legendary" : "Легендарный",
                    _ => English ? "Common" : "Обычный"
                };
                c.category.text = weaponOffer ? (English ? "NEW WEAPON" : "НОВОЕ ОРУЖИЕ")
                    : (u.hasQuality ? quality : ScopeName(u)) + " · " + (progression.Rank(u.id) + 1) + "/" + u.maximumRank;
                c.category.color = !weaponOffer && u.hasQuality && u.quality == PortalTowerBalance.Quality.Rare
                    ? new Color(.45f,.75f,1) : !weaponOffer && u.hasQuality && u.quality == PortalTowerBalance.Quality.Legendary
                    ? new Color(1,.78f,.3f) : new Color(1,.9f,.65f);
                if (u.hasQuality)
                {
                    bool reduction = u.effect == PortalTowerBalance.Effect.TickInterval || u.effect == PortalTowerBalance.Effect.AttackInterval;
                    bool flat = u.effect == PortalTowerBalance.Effect.Projectiles || u.effect == PortalTowerBalance.Effect.Range;
                    c.description.text += "\n\n" + (reduction ? "−" : "+") + (flat ? u.OfferedValue.ToString("0.##") : (u.OfferedValue * 100).ToString("0.##") + "%");
                }
                c.button.interactable=progression.CanPurchase(u);
                c.button.GetComponentInChildren<Text>().text=English?"Choose":"Выбрать";
            }
            FitRewardPanel(lightReward);
        }
        private void ShowUpgradeCrystal(PortalTowerProgression.UpgradeCrystal quality)
        {
            bool hasOffers = progression.CrystalOffers.Count > 0;
            FitRewardPanel(false);
            heading.gameObject.SetActive(true);
            if (!hasOffers)
            {
                panel.sizeDelta = new Vector2(560, 330);
                heading.rectTransform.sizeDelta = new Vector2(496, normalHeadingSize.y);
                heading.rectTransform.anchoredPosition = new Vector2(0, 120);
            }
            heading.text = quality switch
            {
                PortalTowerProgression.UpgradeCrystal.Green => Localized("world.upgrade_crystal.green", "Зелёный кристалл"),
                PortalTowerProgression.UpgradeCrystal.Blue => Localized("world.upgrade_crystal.blue", "Синий кристалл"),
                _ => Localized("world.upgrade_crystal.purple", "Фиолетовый кристалл")
            };
            LayoutHeadingCrystals(progression.UpgradeCrystalIcon(quality));
            var tint = quality switch
            {
                PortalTowerProgression.UpgradeCrystal.Green => new Color(.53f, .93f, .51f),
                PortalTowerProgression.UpgradeCrystal.Blue => new Color(.52f, .77f, 1f),
                _ => new Color(.85f, .61f, 1f)
            };
            for (int i = 0; i < cards.Length; i++)
            {
                var card = cards[i];
                var rect = (RectTransform)card.button.transform.parent;
                bool visible = i < progression.CrystalOffers.Count;
                rect.gameObject.SetActive(visible);
                if (!visible) continue;
                float pitch = cards.Length > 1 ? cardPositions[1].x - cardPositions[0].x : rect.rect.width;
                rect.anchoredPosition = new Vector2((i - (progression.CrystalOffers.Count - 1) * .5f) * pitch, cardPositions[i].y);
                rect.GetComponent<Image>().color = Color.Lerp(cardColors[i], tint, .62f);
                var upgrade = CombatUpgradeCatalog.All[progression.CrystalOffers[i]];
                card.category.gameObject.SetActive(false);
                card.title.text = StatName(upgrade.bonus);
                card.title.color = tint;
                card.title.horizontalOverflow = HorizontalWrapMode.Wrap;
                card.title.rectTransform.anchoredPosition = new Vector2(0, 228);
                card.title.rectTransform.sizeDelta = new Vector2(rect.rect.width - 44, 72);
                card.icon.enabled = false;
                card.description.rectTransform.anchoredPosition = cardDescriptionPositions[i] + new Vector2(0, 50);
                card.description.fontSize = 22;
                card.description.horizontalOverflow = HorizontalWrapMode.Wrap;
                card.description.supportRichText = true;
                card.description.text = DescribeCrystalUpgrade(upgrade, quality);
                card.button.interactable = progression.UpgradeMatchesLoadout(upgrade);
                card.button.GetComponentInChildren<Text>().text = Localized("world.upgrade_crystal.choose", "Выбрать");
            }
            if (crystalIcon == null)
            {
                crystalIcon = new GameObject("Upgrade crystal icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                crystalIcon.transform.SetParent(panel, false);
                crystalIcon.raycastTarget = false;
                crystalIcon.rectTransform.anchoredPosition = new Vector2(0, 48);
            }
            ResourceIconSizing.Apply(crystalIcon, progression.UpgradeCrystalIcon(quality));
            crystalIcon.gameObject.SetActive(!hasOffers && crystalIcon.sprite != null);

            hint.gameObject.SetActive(!hasOffers);
            hint.rectTransform.anchoredPosition = new Vector2(0, -28);
            hint.text = Localized("world.upgrade_crystal.pending", "Для этого кристалла пока нет улучшений.");
            HideRerollTooltip();
            if (rerollButton == null) return;
            rerollButton.gameObject.SetActive(true);
            rerollButton.interactable = true;
            ((RectTransform)rerollButton.transform).anchoredPosition = hasOffers ? normalButtonPosition : new Vector2(0, -106);
            rerollLabel.rectTransform.anchoredPosition = Vector2.zero;
            rerollLabel.alignment = TextAnchor.MiddleCenter;
            if (declineLabelColor != null) declineLabelColor.Negative = true;
            rerollLabel.text = Localized("world.upgrade_crystal.decline", "Отказаться");
            if (rerollAmount != null) rerollAmount.gameObject.SetActive(false);
            if (rerollCostIcon != null) rerollCostIcon.SetActive(false);
        }

        private void LayoutHeadingCrystals(Sprite sprite)
        {
            if (leftHeadingCrystal == null)
            {
                leftHeadingCrystal = CreateHeadingCrystal("Left heading crystal");
                rightHeadingCrystal = CreateHeadingCrystal("Right heading crystal");
            }

            var text = heading.GetComponentInChildren<TMPro.TMP_Text>(true);
            float width = text != null
                ? text.GetPreferredValues(heading.text, Mathf.Infinity, Mathf.Infinity).x
                : heading.preferredWidth;
            Place(leftHeadingCrystal, -1);
            Place(rightHeadingCrystal, 1);

            void Place(Image icon, int direction)
            {
                ResourceIconSizing.Apply(icon, sprite);
                icon.gameObject.SetActive(sprite != null);
                float distance = width * .5f + 16 + icon.rectTransform.rect.width * .5f;
                icon.rectTransform.anchoredPosition = new Vector2(direction * distance, 0);
            }
        }

        private Image CreateHeadingCrystal(string name)
        {
            var icon = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            icon.transform.SetParent(heading.transform, false);
            icon.raycastTarget = false;
            return icon;
        }

        private string StatName(CombatUpgradeCatalog.Stat stat) =>
            Localized("world.upgrade_stat." + stat, stat.ToString());

        private string DescribeCrystalUpgrade(CombatUpgradeCatalog.Upgrade upgrade, PortalTowerProgression.UpgradeCrystal quality)
        {
            string Line(CombatUpgradeCatalog.Stat stat, int value)
            {
                var culture = English ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo("ru");
                string unit = CombatUpgradeCatalog.IsFlat(stat) ? "" : CombatUpgradeCatalog.IsPercentagePoint(stat)
                    ? Localized("world.upgrade_stat.percentage_points", " п. п.") : "%";
                return $"{StatName(stat)} {value.ToString("+0;−0", culture)}{unit}";
            }
            var names = new List<string>();
            for (int slot = 1; slot < PortalWeaponLoadout.SlotCount; slot++)
            {
                var definition = progression.Balance.FindWeapon(progression.WeaponInBattleSlot(slot));
                if (definition == null || !upgrade.Applies(definition)) continue;
                string name = PortalArsenalPopup.WeaponTitle(definition);
                if (!names.Contains(name)) names.Add(name);
            }
            return "<color=#A4E585>" + Line(upgrade.bonus, upgrade.benefit) + "</color>\n" +
                "<color=#F7A3A6>" + Line(upgrade.drawback, upgrade.Penalty(quality)) + "</color>\n\n" +
                (names.Count > 0 ? string.Join(", ", names) :
                    Localized("world.upgrade_crystal.incompatible", "Нет подходящего оружия в наборе"));
        }

        private void FitRewardPanel(bool lightReward)
        {
            if (panel == null) return;
            if (!lightReward)
            {
                panel.sizeDelta = normalPanelSize;
                heading.rectTransform.sizeDelta = normalHeadingSize;
                heading.rectTransform.anchoredPosition = normalHeadingPosition;
                return;
            }
            const float padding = 32f, gap = 24f;
            var card = (RectTransform)cards[0].button.transform.parent;
            float titleHeight = normalHeadingSize.y;
            float titleGap = gap;
            // The visible heading uses TMP, whose glyph widths differ from the legacy Text binding.
            var renderedHeading = heading.GetComponentInChildren<TMPro.TMP_Text>(true);
            float titleWidth = renderedHeading != null
                ? renderedHeading.GetPreferredValues(heading.text, Mathf.Infinity, Mathf.Infinity).x
                : heading.preferredWidth;
            float width = Mathf.Ceil(Mathf.Max(card.rect.width, titleWidth + 12f) + padding * 2f);
            float height = card.rect.height + titleHeight + titleGap + padding * 2f;
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            heading.rectTransform.sizeDelta = new Vector2(width - padding * 2f, titleHeight);
            heading.rectTransform.anchoredPosition = new Vector2(0, height * .5f - padding - titleHeight * .5f);
            card.anchoredPosition = new Vector2(0, -height * .5f + padding + card.rect.height * .5f);
        }
    }
}
