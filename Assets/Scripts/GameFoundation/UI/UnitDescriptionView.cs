using System;
using System.Collections.Generic;
using System.Globalization;
using GameFoundation.Base;
using GameFoundation.Localization;
using GameFoundation.MetaProgression;
using UnityEngine;

namespace GameFoundation.UI
{
    public enum UnitStat
    {
        Health, PhysicalDamage, FireDamage, IceDamage, MagicDamage, AttackRate, AttackInterval,
        AttackRange, MovementSpeed, DetectionRange, PhysicalResistance, FireResistance,
        IceResistance, MagicResistance, Regeneration, RegenerationDelay, PortalHealing, Retreat, Food, TargetPriority, Defense
    }

    [Serializable]
    public sealed class UnitStatPresentation
    {
        public UnitStat stat;
        public string labelKey;
        public string fallbackLabel;
        public Sprite icon;
        public bool hideWhenZero;
    }

    /// <summary>Shared, inspector-authored unit card. Reads combat components, never owns combat values.</summary>
    public sealed class UnitDescriptionView : MonoBehaviour
    {
        [Header("Данные и порядок характеристик")]
        [SerializeField] private UnitDescriptionDefinition definition;
        [SerializeField] private UnitStatPresentation[] stats;
        [SerializeField] private bool showFoodStat = true;
        [Header("Общая карточка")]
        [SerializeField] private UnityEngine.UI.Text title;
        [SerializeField] private UnityEngine.UI.Text role;
        [SerializeField] private UnityEngine.UI.Text description;
        [SerializeField] private UnityEngine.UI.Text progress;
        [SerializeField] private UnityEngine.UI.Text footer;
        [SerializeField] private UnityEngine.UI.Image portrait;
        [SerializeField] private UnityEngine.UI.ScrollRect scroll;
        [SerializeField] private RectTransform rows;
        [SerializeField] private UnitStatRowView rowTemplate;
        [Header("Цвета")]
        [SerializeField] private Color normalColor = new(.94f, .91f, .82f);
        [SerializeField] private Color positiveColor = new(.55f, .85f, .54f);
        [SerializeField] private Color criticalColor = new(1f, .38f, .37f);
        [SerializeField] private Color mutedColor = new(.66f, .62f, .69f);
        [SerializeField] private Color stripeColor = new(.32f, .27f, .36f, .23f);
        [SerializeField, Min(.05f)] private float refreshInterval = .2f;

        [SerializeField, HideInInspector] private List<UnitStatRowView> rowViews = new();
        private MilitaryProfile profile;
        private GameObject liveUnit;
        private Health health;
        private EnemyMovement movement;
        private EnemyAI melee;
        private EnemyAI_Ranged ranged;
        private GlobalStats healthStats, movementStats, damageStats;
        private LocalizationService localization;
        private float nextRefresh;
        public UnitDescriptionDefinition Definition => definition;

        private void OnEnable()
        {
            localization = LocalizationService.Instance;
            if (localization != null) localization.LanguageChanged += Refresh;
            CacheSources();
            Refresh();
        }
        private void OnDisable()
        {
            if (localization != null) localization.LanguageChanged -= Refresh;
        }
        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + refreshInterval;
            Refresh();
        }
        public void Show(UnitDescriptionDefinition unit, MilitaryProfile warrior = null, GameObject deployedUnit = null)
        {
            definition = unit; profile = warrior; liveUnit = deployedUnit;
            CacheSources();
            Refresh();
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
        }
        public void Scroll(float delta)
        {
            if (scroll == null || scroll.content == null || scroll.viewport == null) return;
            float excess = scroll.content.rect.height - scroll.viewport.rect.height;
            if (excess > 0f) scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition + delta * scroll.scrollSensitivity / excess);
        }
        private void CacheSources()
        {
            GameObject source = liveUnit != null ? liveUnit : definition != null ? definition.unitPrefab : null;
            health = source != null ? source.GetComponent<Health>() : null;
            movement = source != null ? source.GetComponent<EnemyMovement>() : null;
            melee = source != null ? source.GetComponent<EnemyAI>() : null;
            ranged = source != null ? source.GetComponent<EnemyAI_Ranged>() : null;
            healthStats = health != null ? health.Stats : null;
            movementStats = movement != null ? movement.Stats : null;
            damageStats = ranged != null ? ranged.GetStats() : source != null ? source.GetComponent<EnemyVisuals>()?.Stats : null;
        }
        public void Refresh()
        {
            if (definition == null || rows == null || rowTemplate == null || stats == null) return;
            int stars = MilitaryExperienceService.Stars(profile);
            float multiplier = 1f + stars * .1f;
            if (title != null) title.text = definition.Title;
            if (role != null) role.text = definition.Role;
            if (description != null) description.text = definition.Description;
            if (portrait != null)
            {
                portrait.enabled = definition.Portrait != null;
                ResourceIconSizing.Apply(portrait, definition.Portrait);
            }
            if (progress != null)
            {
                int earned = 0;
                for (int level = 0; level < stars; level++) earned += 10 + 5 * level;
                progress.text = definition.isEnemy ? T("enemy_type", "Характеристики типа врага") : profile == null ? T("recruit", "Новобранец · без звёзд") :
                    string.Format(T("progress", "Уровень {0}/10 · опыт {1}"), stars,
                        stars >= 10 ? T("maximum", "максимум") : (profile.experience - earned) + " / " + (10 + 5 * stars));
            }
            if (footer != null) footer.text = definition.isEnemy
                ? T("enemy_footer", "Базовые характеристики этого типа врагов.\nКолесо мыши — прокрутка характеристик.")
                : string.Format(T("footer", "За убийство: {0} опыта · за помощь: {1}\nНовый день полностью восстанавливает здоровье.\nКолесо мыши — прокрутка характеристик."), MilitaryExperience.KillExperience, MilitaryExperience.AssistExperience);

            int visible = 0;
            foreach (UnitStatPresentation entry in stats)
            {
                if (entry != null && entry.stat == UnitStat.Food && !showFoodStat) continue;
                if (definition.isEnemy && entry != null && (entry.stat == UnitStat.Food || entry.stat == UnitStat.Retreat || entry.stat == UnitStat.PortalHealing)) continue;
                if (entry == null || !TryValue(entry, multiplier, out string value, out Color color)) continue;
                if (visible == rowViews.Count)
                {
                    UnitStatRowView row = Instantiate(rowTemplate, rows);
                    row.name = "Stat " + entry.stat;
                    rowViews.Add(row);
                }
                UnitStatRowView view = rowViews[visible];
                view.gameObject.SetActive(true);
                Sprite icon = entry.stat == UnitStat.Food && definition.food != null ? definition.food.resourceIcon : entry.icon;
                var layout = view.GetComponent<UnityEngine.UI.LayoutElement>();
                if (layout != null) layout.preferredHeight = Mathf.Max(34f, icon != null ? icon.rect.height * 2f + 6f : 34f);
                string caption = UnitDescriptionText.Get(entry.labelKey, entry.fallbackLabel);
                Color stripe = visible % 2 == 0 ? stripeColor : Color.clear;
                if (entry.stat == UnitStat.Defense)
                    view.PresentDefense(icon, caption, healthStats, positiveColor, criticalColor, mutedColor, stripe);
                else
                    view.Present(icon, caption, value, color, stripe, entry.stat == UnitStat.Food);
                visible++;
            }
            for (int i = visible; i < rowViews.Count; i++) rowViews[i].gameObject.SetActive(false);
        }

        private bool TryValue(UnitStatPresentation entry, float multiplier, out string value, out Color color)
        {
            value = ""; color = normalColor;
            float number = 0f;
            switch (entry.stat)
            {
                case UnitStat.Health:
                    float max = (healthStats != null ? healthStats.TotalMaxHealth : 100f) * multiplier;
                    float current = liveUnit != null && health != null ? health.CurrentHealth : max * MilitaryExperienceService.HealthPercent(profile);
                    value = N(current) + " / " + N(max);
                    color = current < max * .25f ? criticalColor : current < max ? normalColor : positiveColor;
                    return true;
                case UnitStat.PhysicalDamage: case UnitStat.FireDamage: case UnitStat.IceDamage: case UnitStat.MagicDamage:
                    DamageType damageType = (DamageType)(entry.stat - UnitStat.PhysicalDamage);
                    if (damageStats != null) foreach (var damage in damageStats.damageSettings)
                        if (damage.type == damageType) number += damage.TotalDamage * multiplier;
                    value = N(number); break;
                case UnitStat.AttackRate:
                    // Attacks per second is the reciprocal of the base interval, with the level bonus applied.
                    value = N(1f / Cooldown(multiplier)) + T("per_second", " / с"); return true;
                case UnitStat.AttackInterval:
                    float variation = ranged != null ? ranged.CooldownVariation : melee != null ? melee.CooldownVariation / multiplier : 0f;
                    value = Range(Mathf.Max(.05f, Cooldown(multiplier) - variation), Cooldown(multiplier) + variation) + T("seconds", " с"); return true;
                case UnitStat.AttackRange:
                    number = (ranged != null ? ranged.CurrentAttackRange / MilitaryExperience.Multiplier(ranged) : melee != null ? melee.BaseAttackRange : 0f) * multiplier;
                    value = N(number) + T("distance", " ед."); break;
                case UnitStat.MovementSpeed:
                    float speed = movementStats != null ? movementStats.TotalSpeed : 3f;
                    float spread = movement != null ? movement.SpeedVariation : 0f;
                    value = (liveUnit != null && movement != null ? N(movement.CombatSpeed) : Range(speed - spread, speed + spread)) + T("speed_unit", " ед./с"); return true;
                case UnitStat.DetectionRange:
                    number = ranged != null ? ranged.DetectionRange : melee != null ? melee.DetectionRange : 0f;
                    value = N(number) + T("distance", " ед."); break;
                case UnitStat.PhysicalResistance: case UnitStat.FireResistance: case UnitStat.IceResistance: case UnitStat.MagicResistance:
                    DamageType resistType = (DamageType)(entry.stat - UnitStat.PhysicalResistance);
                    var resistance = healthStats != null ? healthStats.resistances.Find(item => item.type == resistType) : null;
                    number = resistance != null ? (1f - resistance.CurrentMult) * 100f : 0f;
                    value = N(number) + "%"; color = number > 0f ? positiveColor : number < 0f ? criticalColor : mutedColor; break;
                case UnitStat.Defense:
                    return true;
                case UnitStat.Regeneration:
                    number = healthStats != null ? healthStats.TotalRegenAmount * multiplier : 0f;
                    value = number > 0f ? N(number) + T("hp_per_second", " HP/с") : T("none", "Нет"); break;
                case UnitStat.RegenerationDelay:
                    if (healthStats == null || healthStats.TotalRegenAmount <= 0f) return false;
                    value = N(healthStats.TotalRegenDelay / multiplier) + T("seconds", " с"); return true;
                case UnitStat.PortalHealing:
                    number = definition.scientificStats != null ? definition.scientificStats.WarriorBaseRegenPerSecond * 100f : 0f;
                    value = number > 0f ? N(number) + T("percent_per_second", "% HP/с") : T("not_researched", "Не изучено");
                    color = number > 0f ? positiveColor : mutedColor; return true;
                case UnitStat.Retreat:
                    bool retreat = RoyalDecreeService.IsEnabled(RoyalDecreeService.CautiousWarriors);
                    value = retreat ? T("retreat_threshold", "При HP ≤ 10%") : T("decree_off", "Указ выключен");
                    color = retreat ? positiveColor : mutedColor; return true;
                case UnitStat.Food:
                    value = T("food_amount", "1 / день"); return definition.food != null;
                case UnitStat.TargetPriority:
                    bool weakest = !definition.isEnemy && ranged != null && RoyalDecreeService.IsEnabled(RoyalDecreeService.FinishOffEnemies);
                    value = weakest ? T("weakest", "Меньше % HP") : T("nearest", "Ближайший");
                    color = weakest ? positiveColor : normalColor; return true;
            }
            if (entry.hideWhenZero && Mathf.Approximately(number, 0f)) return false;
            return true;
        }

        private float Cooldown(float multiplier) => Mathf.Max(.05f,
            (ranged != null ? (ranged.GetStats() != null ? ranged.GetStats().TotalCooldown : 2f) : melee != null ? melee.BaseAttackCooldown : 1.5f) / multiplier);
        private static string N(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);
        private static string Range(float low, float high) => Mathf.Approximately(low, high) ? N(low) : N(low) + "–" + N(high);
        private static string T(string suffix, string fallback) => UnitDescriptionText.Get("unit.details." + suffix, fallback);
    }
}
