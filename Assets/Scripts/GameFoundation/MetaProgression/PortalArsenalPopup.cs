using System.Collections.Generic;
using GameFoundation.Localization;
using GameFoundation.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameFoundation.MetaProgression
{
    public sealed class PortalArsenalPopup : MonoBehaviour
    {
        public TMP_Text heading, listHeading, detailHeading, description, requirement, actionLabel;
        public UnityEngine.UI.Button closeButton, actionButton;
        public UnityEngine.UI.Image costIcon;
        public TMP_Text costAmount;
        public RectTransform list, stats;
        public UnityEngine.UI.ScrollRect statsScroll;
        public PortalArsenalWeaponRow rowTemplate;
        public UnitStatRowView statTemplate;
        [SerializeField] private UnitDescriptionView unitStatStyle;
        [SerializeField] private Sprite projectileStatIcon, areaStatIcon, energyStatIcon;
        [SerializeField] private PortalArsenalCatalog baseUnlockCatalog;
        [SerializeField] private RectTransform statHeader;
        public PortalWeaponSlots weaponSlots;
        public TMP_Text slotInstructions;
        public ArcherTower baseTowerPreview;
        public bool IsBaseArsenal { get; private set; }
        public PortalTowerBalance.Weapon SelectedWeapon { get; private set; } = PortalTowerBalance.Weapon.Bolts;
        private PortalTowerBalance baseBalance;
        public PortalTowerBalance Balance => IsBaseArsenal ? baseBalance : progression != null ? progression.Balance : null;
        public bool CanAssign(PortalTowerBalance.Weapon weapon) => IsBaseArsenal && Balance != null && Balance.arsenal.Find(weapon) != null && PortalArsenalProgress.IsUnlocked(weapon);
        public static bool IsOpen => opened != null;
        public static int ClosedFrame { get; private set; } = -1;
        private static PortalArsenalPopup opened;
        private readonly List<PortalArsenalWeaponRow> rows = new();
        private readonly List<UnitStatRowView> statRows = new();
        private PortalTowerProgression progression;
        private ArcherTower tower;
        private PortalTowerBalance.Weapon preview;
        private float previousSpeed;
        private LocalizationService language;
        private ButtonVisualTheme theme;
        public static string T(string suffix, string fallback)
        {
            string key = "arsenal." + suffix;
            string value = LocalizationService.Instance?.Get(key);
            return string.IsNullOrEmpty(value) || value == key ? fallback : value;
        }
        public static string WeaponTitle(PortalTowerBalance.WeaponDefinition weapon) => T("weapon." + weapon.weapon, weapon.title);
        public void Open(PortalTowerProgression owner, ArcherTower source)
        {
            if (IsOpen || owner == null || !owner.UsesArsenal || owner.UsesEnergyLoadout || owner.ExpeditionEnded || PortalTowerProgression.IsChoosingUpgrade || FirstRunLightTutorial.BlocksInput) return;
            IsBaseArsenal = false; progression = owner; tower = source; preview = owner.ActiveWeapon;
            OpenWindow();
        }
        public void OpenBase(PortalTowerBalance config)
        {
            if (IsOpen || config == null || !config.useEnergyLoadout) return;
            IsBaseArsenal = true; baseBalance = config; progression = null; tower = baseTowerPreview;
            preview = SelectedWeapon = PortalTowerBalance.Weapon.Bolts;
            OpenWindow();
        }
        private void OpenWindow()
        {
            theme = Resources.Load<ButtonVisualTheme>("UI/ButtonVisualTheme");
            previousSpeed = GameSpeedControls.SimulationSpeed; opened = this;
            gameObject.SetActive(true); GameSpeedControls.SetSimulationSpeed(0);
            closeButton.onClick.RemoveAllListeners(); closeButton.onClick.AddListener(Close);
            actionButton.onClick.RemoveAllListeners(); actionButton.onClick.AddListener(Act);
            if (progression != null) progression.Changed += Refresh;
            PortalWeaponLoadout.Changed += Refresh;
            GlobalResourceManager.OnResourceChanged += ResourceChanged;
            language = LocalizationService.Instance; if (language != null) language.LanguageChanged += Refresh;
            PortalArsenalProgress.RecordPopulation();
            if (rows.Count == 0)
                foreach (var entry in Balance.arsenal.entries)
                {
                    var row = Instantiate(rowTemplate, list); row.gameObject.SetActive(true);
                    row.Bind(this, entry.weapon); rows.Add(row);
                }
            if (weaponSlots != null) { weaponSlots.gameObject.SetActive(IsBaseArsenal); weaponSlots.Bind(Balance, PortalWeaponSlots.Context.Assignment, this); }
            if (slotInstructions != null) slotInstructions.gameObject.SetActive(IsBaseArsenal);
            Refresh();
            EventSystem.current?.SetSelectedGameObject(null);
            GameFoundation.Audio.GameAudioController.PlayUI(GameFoundation.Audio.GameAudioCue.UiConfirm);
        }
        private void ResourceChanged(ResourceType type, int amount) => Refresh();
        private void Update()
        {
            if (opened != this) return;
            if ((!IsBaseArsenal && (progression == null || progression.ExpeditionEnded)) || PortalCloudTransition.IsLoading || WorldEscapeController.ResultsActive) { Close(); return; }
            if (Input.GetKeyDown(KeyCode.Escape)) Close();
        }
        // Transfer the existing pause to a mandatory reward without briefly resuming combat.
        public static float ReleaseForUpgrade(float fallbackSpeed)
        {
            if (opened == null) return fallbackSpeed;
            float speed = opened.previousSpeed;
            opened.Close(false);
            return speed;
        }
        public void Close() => Close(true);
        private void Close(bool restoreSpeed)
        {
            if (opened != this) return;
            opened = null; ClosedFrame = Time.frameCount;
            if (progression != null) progression.Changed -= Refresh;
            PortalWeaponLoadout.Changed -= Refresh;
            GlobalResourceManager.OnResourceChanged -= ResourceChanged;
            if (language != null) language.LanguageChanged -= Refresh;
            gameObject.SetActive(false); EventSystem.current?.SetSelectedGameObject(null);
            if (restoreSpeed && !PortalCloudTransition.IsLoading && (progression == null || !progression.ExpeditionEnded)) GameSpeedControls.SetSimulationSpeed(previousSpeed);
        }
        private void OnDisable() => Close();
        public void PreviewWeapon(PortalTowerBalance.Weapon weapon)
        {
            if (preview == weapon) return;
            preview = weapon; RefreshDetails(); statsScroll.verticalNormalizedPosition = 1;
        }
        public void ClickWeapon(PortalTowerBalance.Weapon weapon)
        {
            if (IsBaseArsenal) { SelectedWeapon = weapon; PreviewWeapon(weapon); Refresh(); return; }
            PreviewWeapon(weapon);
            if (progression.OwnsWeapon(weapon)) progression.SelectWeapon(weapon);
        }
        private PortalArsenalCatalog BaseUnlocks(PortalTowerBalance config) => baseUnlockCatalog != null ? baseUnlockCatalog : config.arsenal;

        public bool HasAffordableBaseUnlock(PortalTowerBalance config)
        {
            var catalog = BaseUnlocks(config);
            if (catalog == null) return false;
            foreach (var entry in catalog.entries)
                if (PortalArsenalProgress.CanUnlock(entry)) return true;
            return false;
        }

        private PortalArsenalCatalog.Entry UnlockEntry(PortalTowerBalance.Weapon weapon) =>
            (IsBaseArsenal ? BaseUnlocks(Balance) : Balance.arsenal).Find(weapon);

        private void Act()
        {
            if (IsBaseArsenal)
            {
                if (PortalArsenalProgress.TryUnlock(UnlockEntry(preview))) GameFoundation.Audio.GameAudioController.PlayUI(GameFoundation.Audio.GameAudioCue.ContentUnlock);
                Refresh(); return;
            }
            if (progression.OwnsWeapon(preview)) progression.SelectWeapon(preview);
            else progression.UnlockWeapon(preview);
            Refresh();
        }
        public void Refresh()
        {
            if (Balance == null) return;
            heading.text = IsBaseArsenal ? T("base.title", "Магическая башня") : T("title", "Оружие башни портала");
            listHeading.text = T("weapons", "Выберите оружие");
            for (int i = 0; i < rows.Count; i++)
            {
                var entry = Balance.arsenal.entries[i]; var row = rows[i];
                var weapon = Balance.FindWeapon(entry.weapon);
                row.gameObject.SetActive(weapon != null && (IsBaseArsenal || !weapon.IsSupport));
                if (!row.gameObject.activeSelf) continue;
                row.title.text = WeaponTitle(weapon); ResourceIconSizing.Apply(row.icon, weapon.icon);
                bool unlocked = IsBaseArsenal ? PortalArsenalProgress.IsUnlocked(entry.weapon) : progression.OwnsWeapon(entry.weapon), active = !IsBaseArsenal && progression.ActiveWeapon == entry.weapon;
                row.icon.color = unlocked ? Color.white : new Color(.42f,.39f,.46f);
                row.state.text = active ? T("active", "Активно") : unlocked ? T("available", "Доступно") : T("locked", "Закрыто");
                bool affordable = IsBaseArsenal && !unlocked && PortalArsenalProgress.CanUnlock(UnlockEntry(entry.weapon));
                row.state.color = active || affordable ? theme.positiveLabelColor : theme.popupSubtitleColor;
                row.feedback.Toggled = IsBaseArsenal ? SelectedWeapon == entry.weapon : active;
            }
            if (slotInstructions != null && IsBaseArsenal) slotInstructions.text = T("slots.instructions", "Перетащите открытое оружие в слот или выберите его и нажмите 2–5. Слот 1 выключает оружие.");
            RefreshDetails();
        }
        private void RefreshDetails()
        {
            var weapon = Balance.FindWeapon(preview); if (weapon == null) return;
            var entry = UnlockEntry(preview);
            detailHeading.text = WeaponTitle(weapon);
            description.text = T("description." + preview, weapon.description) + (weapon.IsSupport ? "" : "\n" + GameFoundation.Combat.CombatEffectText.Description(weapon.damageType));
            bool unlocked = IsBaseArsenal ? PortalArsenalProgress.IsUnlocked(preview) : progression.OwnsWeapon(preview);
            bool active = !IsBaseArsenal && progression.ActiveWeapon == preview;
            requirement.gameObject.SetActive(!IsBaseArsenal);
            actionButton.gameObject.SetActive(!IsBaseArsenal || !unlocked);

            float textHeight = Mathf.Clamp(description.GetPreferredValues(description.text, description.rectTransform.rect.width, 1000).y + 4, 48, 164);
            var detail = (RectTransform)statsScroll.transform.parent;
            float descriptionTop = detail.rect.yMax - 66;
            description.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textHeight);
            description.rectTransform.anchoredPosition = new Vector2(0, descriptionTop - textHeight * .5f);
            float top = descriptionTop - textHeight - 10;
            if (statHeader != null)
            {
                statHeader.anchoredPosition = new Vector2(0, top - 14);
                top -= 32;
            }
            var action = (RectTransform)actionButton.transform;
            float bottom = !IsBaseArsenal ? requirement.rectTransform.anchoredPosition.y + requirement.rectTransform.rect.height * .5f + 8
                : unlocked ? detail.rect.yMin + 20 : action.anchoredPosition.y + action.rect.height * .5f + 12;
            var viewportRect = (RectTransform)statsScroll.transform;
            viewportRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, top - bottom);
            viewportRect.anchoredPosition = new Vector2(0, (top + bottom) * .5f);
            statsScroll.viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, top - bottom);
            int current = PortalArsenalProgress.Progress(entry);
            if (!IsBaseArsenal) requirement.text = unlocked ? T("permanent", "Открыто навсегда для этого сохранения.") : RequirementText(entry, current);
            actionLabel.text = active ? T("active", "Активно") : string.Format(
                unlocked ? T("equipNamed", "Выбрать: {0}") : T("unlockNamed", "Открыть: {0}"), WeaponTitle(weapon));
            actionButton.interactable = !active && (unlocked || PortalArsenalProgress.CanUnlock(entry));
            bool paid = !unlocked && entry.requirement == PortalArsenalCatalog.Requirement.Resource;
            var hint = actionButton.GetComponent<GameFoundation.Base.BaseDisabledButtonHint>();
            if (paid)
            {
                GameFoundation.Base.BaseDisabledButtonHint.Set(actionButton, GameFoundation.Base.BaseDisabledButtonHint.Reason.Resources);
                actionButton.GetComponent<GameFoundation.Base.BaseDisabledButtonHint>().enabled = true;
            }
            else if (hint != null) hint.enabled = false;
            costIcon.gameObject.SetActive(paid); costAmount.gameObject.SetActive(paid);
            if (paid)
            {
                ResourceIconSizing.Apply(costIcon, entry.resource != null ? entry.resource.resourceIcon : null);
                costAmount.text = entry.amount.ToString(); costAmount.color = current >= entry.amount ? new Color(.45f,.85f,.45f) : new Color(1,.3f,.35f);
            }
            actionLabel.rectTransform.offsetMax = new Vector2(paid ? -160 : -20, -4);
            var lines = PortalWeaponDescription.Stats(progression, weapon, tower);
            for (int i = 0; i < lines.Count; i++)
            {
                if (i == statRows.Count) statRows.Add(Instantiate(statTemplate, stats));
                var row = statRows[i]; row.gameObject.SetActive(true);
                var line = lines[i];
                if (unitStatStyle != null)
                    unitStatStyle.PresentStat(row, StatIcon(line.key, weapon.damageType), line.label, line.value, i, line.key is "energyCost" or "supportEnergy" or "energyPerSecond");
                else
                    row.Present(null, line.label, line.value, new Color(.94f,.91f,.82f), i % 2 == 0 ? new Color(.17f,.14f,.195f) : Color.clear);
            }
            for (int i = lines.Count; i < statRows.Count; i++) statRows[i].gameObject.SetActive(false);
        }
        private Sprite StatIcon(string key, DamageType type)
        {
            var damage = type switch
            {
                DamageType.Fire => UnitStat.FireDamage,
                DamageType.Ice => UnitStat.IceDamage,
                DamageType.Physical => UnitStat.PhysicalDamage,
                DamageType.Electric => UnitStat.ElectricDamage,
                _ => UnitStat.MagicDamage
            };
            return key switch
            {
                "healing" => unitStatStyle.StatIcon(UnitStat.Regeneration),
                "shield" => unitStatStyle.StatIcon(UnitStat.Defense),
                "armor" => unitStatStyle.StatIcon(UnitStat.Armor),
                "range" => unitStatStyle.StatIcon(UnitStat.AttackRange),
                "interval" or "supportInterval" or "tick" or "duration" or "burnDuration" or "burnTick" or "chillDuration" or "stunDuration" or "echoDelay"
                    => unitStatStyle.StatIcon(UnitStat.AttackRate),
                "speed" or "projectileSpeed" => unitStatStyle.StatIcon(UnitStat.MovementSpeed),
                "targets" or "chainCount" => unitStatStyle.StatIcon(UnitStat.TargetPriority),
                "projectiles" or "spread" => projectileStatIcon,
                "areas" or "radius" or "ringWidth" or "impactRadius" => areaStatIcon,
                "areaSlow" => unitStatStyle.StatIcon(UnitStat.IceDamage),
                "energyCost" or "supportEnergy" or "energyPerSecond" => energyStatIcon,
                _ => unitStatStyle.StatIcon(damage)
            };
        }

        private static string RequirementText(PortalArsenalCatalog.Entry entry, int progress)
        {
            if (entry == null) return "";
            string format = entry.requirement switch
            {
                PortalArsenalCatalog.Requirement.EnemyKills => T("require.kills", "Победить: {2}. Прогресс: {0} / {1}"),
                PortalArsenalCatalog.Requirement.Population => T("require.population", "Достичь населения {1}. Рекорд: {0}"),
                PortalArsenalCatalog.Requirement.ExploredHexes => T("require.hexes", "Открыть гексы: {0} / {1}"),
                PortalArsenalCatalog.Requirement.TowerLevel => T("require.level", "Достичь уровня башни {1}. Рекорд: {0}"),
                PortalArsenalCatalog.Requirement.Resource => T("require.resource", "Разблокировать за ресурсы. Цена указана на кнопке."),
                _ => T("available", "Доступно")
            };
            return string.Format(format, progress, entry.amount, T("enemy." + entry.enemyId, entry.enemyId));
        }
    }
}
