using System;
using System.Collections;
using System.Collections.Generic;
using GameFoundation.Audio;
using RunStat = GameFoundation.MetaProgression.CombatUpgradeCatalog.Stat;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameFoundation.MetaProgression
{
    /// <summary>Owns progression for this expedition only; never writes to the permanent laboratory save.</summary>
    [DefaultExecutionOrder(-150)]
    public sealed class PortalTowerProgression : MonoBehaviour
    {
        public static PortalTowerProgression Instance { get; private set; }
        public static bool IsChoosingUpgrade => Instance != null && (Instance.LevelPoints > 0 || Instance.UpgradeCrystalChoice.HasValue);
        public bool UsesArsenal { get; private set; }
        public bool UsesEnergyLoadout { get; private set; }
        public int Energy { get; private set; }
        public int EnergyCapacity => PortalEnergyRules.Capacity(balance);
        public int CrystalEfficiencyRank => scientificStats != null ? scientificStats.CrystalEfficiencyRank : 0;
        public event Action EnergyChanged;
        public event Action<int> EnergyLightCountChanged;
        internal event Action EnergyCrystalDelivered;
        internal event Action EnergyDepleted;
        private readonly Dictionary<PortalTowerBalance.Weapon, (float start, float end)> reloads = new();

        public void ReportReload(PortalTowerBalance.Weapon weapon, float readyAt)
        {
            if (UsesEnergyLoadout && readyAt > Time.time) reloads[weapon] = (Time.time, readyAt);
        }

        public float ReloadProgress(PortalTowerBalance.Weapon weapon)
        {
            if (expeditionEnded || !reloads.TryGetValue(weapon, out var reload) || Time.time >= reload.end) return 0;
            return Mathf.InverseLerp(reload.start, reload.end, Time.time);
        }
        private readonly float[] shotEnergyRemainders = new float[(int)PortalTowerBalance.Weapon.DefenseField + 1];
        private readonly PortalWeaponMode[] weaponModes = new PortalWeaponMode[(int)PortalTowerBalance.Weapon.DefenseField + 1];
        internal PortalWeaponProfiles.Profile Profile(PortalTowerBalance.Weapon weapon) =>
            PortalWeaponProfiles.Get(weapon, UsesEnergyLoadout ? weaponModes[(int)weapon] : PortalWeaponMode.Balanced);
        public float ShotEnergy(PortalTowerBalance.Weapon weapon) =>
            Profile(weapon).Cost(Mathf.Max(1, balance.FindWeapon(weapon)?.energyPerShot ?? 1)) * RunMultiplier(weapon, RunStat.Energy);
        private int NextShotCost(PortalTowerBalance.Weapon weapon) => Mathf.FloorToInt(ShotEnergy(weapon) + shotEnergyRemainders[(int)weapon] + .0001f);
        public bool CanPayForShot(PortalTowerBalance.Weapon weapon) => !UsesEnergyLoadout || (Energy > 0 && Energy >= NextShotCost(weapon));
        public bool TrySpendShot(PortalTowerBalance.Weapon weapon)
        {
            if (!HasWeapon(weapon) || !CanPayForShot(weapon)) return false;
            if (UsesEnergyLoadout)
            {
                int cost = NextShotCost(weapon);
                int index = (int)weapon;
                shotEnergyRemainders[index] = Mathf.Max(0, shotEnergyRemainders[index] + ShotEnergy(weapon) - cost);
                SetEnergy(Energy - cost);
            }
            return true;
        }
        internal void SpendContinuousEnergy(int amount)
        {
            if (UsesEnergyLoadout && !expeditionEnded && amount > 0) SetEnergy(Energy - amount);
        }

        private void SetEnergy(int value, bool notifyLightChange = true)
        {
            int oldLights = LightCount, oldLevel = Level, oldEnergy = Energy;
            Energy = Mathf.Clamp(value, 0, EnergyCapacity);
            Level = PortalEnergyRules.Level(balance, Energy, CrystalEfficiencyRank);
            PortalArsenalProgress.RecordLevel(Level);
            EnergyChanged?.Invoke();
            int lightChange = LightCount - oldLights;
            if (lightChange != 0 || oldLevel != Level) Changed?.Invoke();
            if (notifyLightChange && lightChange != 0 && !expeditionEnded)
            {
                GameAudioController.PlayUI(lightChange > 0 ? GameAudioCue.ContentUnlock : GameAudioCue.UiDenied);
                EnergyLightCountChanged?.Invoke(lightChange);
            }
            if (oldEnergy > 0 && Energy == 0 && !expeditionEnded) EnergyDepleted?.Invoke();
        }
        public PortalTowerBalance.Weapon ActiveWeapon { get; private set; } = PortalTowerBalance.Weapon.Bolts;
        private bool energyWeaponsAvailable;
        private bool usePracticeWeapon;
        public bool WeaponsAvailable => UsesEnergyLoadout ? energyWeaponsAvailable : UsesArsenal || CrystalTechnologyAvailable;
        public bool OwnsWeapon(PortalTowerBalance.Weapon weapon) => UsesArsenal
            ? balance.arsenal != null && balance.arsenal.Find(weapon) != null && (UsesEnergyLoadout || balance.FindWeapon(weapon)?.IsSupport != true) && PortalArsenalProgress.IsUnlocked(weapon)
            : HasWeapon(weapon);
        internal PortalTowerBalance.Weapon WeaponInBattleSlot(int index)
        {
            var weapon = PortalWeaponLoadout.Get(index);
            if (!usePracticeWeapon) return weapon;
            if (index == 1) return PortalTowerBalance.Weapon.Bolts;
            return weapon == PortalTowerBalance.Weapon.Bolts ? PortalWeaponLoadout.Get(1) : weapon;
        }

        internal void PrepareWeaponPractice()
        {
            var weapon = PortalWeaponLoadout.Get(1);
            usePracticeWeapon = weapon == PortalTowerBalance.Weapon.None || !OwnsWeapon(weapon) ||
                balance.FindWeapon(weapon)?.IsSupport != false;
            ActiveWeapon = PortalTowerBalance.Weapon.None;
            energyWeaponsAvailable = true;
            Changed?.Invoke();
        }

        internal bool SelectWeaponSlot(int index) =>
            FirstRunLightTutorial.CanUseWeaponSlot(index) &&
            SelectWeapon(WeaponInBattleSlot(index), FirstRunLightTutorial.RequiredWeaponSlot == index);

        public bool SelectWeapon(PortalTowerBalance.Weapon weapon) => SelectWeapon(weapon, false);

        private bool SelectWeapon(PortalTowerBalance.Weapon weapon, bool practicing)
        {
            if (!UsesArsenal || !WeaponsAvailable || expeditionEnded || IsChoosingUpgrade ||
                (FirstRunLightTutorial.BlocksInput && !practicing) ||
                (weapon != PortalTowerBalance.Weapon.None && !OwnsWeapon(weapon)) ||
                (UsesEnergyLoadout && !PortalWeaponLoadout.Contains(weapon) && !(usePracticeWeapon && weapon == PortalTowerBalance.Weapon.Bolts)) ||
                (!UsesEnergyLoadout && weapon == PortalTowerBalance.Weapon.None)) return false;
            if (ActiveWeapon == weapon) return true;
            ActiveWeapon = weapon;
            PortalWeaponController.Instance?.WeaponSelectionChanged();
            GameAudioController.PlayUI(GameAudioCue.UiConfirm); Changed?.Invoke(); return true;
        }
        public bool UnlockWeapon(PortalTowerBalance.Weapon weapon)
        {
            if (!UsesArsenal || expeditionEnded || IsChoosingUpgrade || FirstRunLightTutorial.BlocksInput || !PortalArsenalProgress.TryUnlock(balance.arsenal.Find(weapon))) return false;
            GameAudioController.PlayUI(GameAudioCue.ContentUnlock); Changed?.Invoke(); return true;
        }
        [SerializeField] private PortalTowerBalance balance;
        [SerializeField] private GameObject upgradeWindow;
        [Header("Получение опыта")]
        [SerializeField] private PortalExperienceCrystal experienceCrystalPrefab;
        [SerializeField] private Sprite groundExperienceCrystal;
        [SerializeField] private GlobalStats scientificStats;
        private ResourceType crystalResource;
        public enum UpgradeCrystal { Green, Blue, Purple }
        public UpgradeCrystal? UpgradeCrystalChoice { get; private set; }
        private readonly Dictionary<UpgradeCrystal, ResourceType> upgradeCrystals = new();
        private readonly Queue<UpgradeCrystal> pendingUpgradeCrystals = new();
        private int upgradeCrystalChance, highestDangerLevel;

        public Sprite UpgradeCrystalIcon(UpgradeCrystal quality) => GetUpgradeCrystal(quality)?.resourceIcon;

        private ResourceType GetUpgradeCrystal(UpgradeCrystal quality)
        {
            if (upgradeCrystals.TryGetValue(quality, out var type)) return type;
            var icon = Resources.Load<Sprite>("World/Upgrade Crystal " + quality);
            if (icon == null) return null;
            type = ScriptableObject.CreateInstance<ResourceType>();
            type.name = quality + " Upgrade Crystal";
            type.hideFlags = HideFlags.DontSave;
            type.resourceName = type.name;
            type.notifyResourceChanges = false;
            type.resourceIcon = type.defaultCarrySprite = type.groundSprite = icon;
            upgradeCrystals.Add(quality, type);
            return type;
        }

        private void TryDropUpgradeCrystal(Vector3 position, int dangerLevel)
        {
            if (dangerLevel > highestDangerLevel)
            {
                upgradeCrystalChance = Mathf.Min(100, upgradeCrystalChance + 10 * (dangerLevel - highestDangerLevel));
                highestDangerLevel = dangerLevel;
            }
            if (upgradeCrystalChance == 0 || UnityEngine.Random.Range(0, 100) >= upgradeCrystalChance) return;
            int roll = UnityEngine.Random.Range(0, 100);
            var quality = roll < 50 ? UpgradeCrystal.Green : roll < 85 ? UpgradeCrystal.Blue : UpgradeCrystal.Purple;
            var type = GetUpgradeCrystal(quality);
            if (type == null || !SpawnGroundCrystal(type, position + (Vector3)(UnityEngine.Random.insideUnitCircle * .45f), true)) return;
            upgradeCrystalChance = Mathf.Max(5, upgradeCrystalChance - 5);
        }

        private void LateUpdate()
        {
            if (pendingUpgradeCrystals.Count == 0 || IsChoosingUpgrade || upgradeWindow.activeSelf || expeditionEnded ||
                FirstRunLightTutorial.BlocksInput || WorldEscapeController.ResultsActive || PortalCloudTransition.IsLoading) return;

            var quality = pendingUpgradeCrystals.Dequeue();
            RollCrystalOffers();
            if (crystalOffers.Count == 0) return;
            UpgradeCrystalChoice = quality;
            previousSpeed = PortalArsenalPopup.ReleaseForUpgrade(GameSpeedControls.SimulationSpeed);
            GameSpeedControls.SetSimulationSpeed(0);
            BlockInterface();
            upgradeWindow.SetActive(true);
            GameFoundation.UI.PopupMotion.ShowDimmed(upgradeWindow);
            Changed?.Invoke();
        }

        private readonly List<int> crystalOffers = new();
        private readonly Dictionary<int, UpgradeCrystal> chosenCrystalUpgrades = new();
        public IReadOnlyList<int> CrystalOffers => crystalOffers;

        private void RollCrystalOffers()
        {
            crystalOffers.Clear();
            var available = new List<int>();
            var compatible = new List<int>();
            for (int i = 0; i < CombatUpgradeCatalog.All.Count; i++)
            {
                if (chosenCrystalUpgrades.ContainsKey(i)) continue;
                available.Add(i);
                if (UpgradeMatchesLoadout(CombatUpgradeCatalog.All[i])) compatible.Add(i);
            }

            if (compatible.Count == 0) return;
            while (crystalOffers.Count < 3)
            {
                // Prefer useful cards, but a third card must break a pair of the same type.
                var typeToAvoid = crystalOffers.Count == 2 &&
                    CombatUpgradeCatalog.All[crystalOffers[0]].OfferType == CombatUpgradeCatalog.All[crystalOffers[1]].OfferType
                    ? (RunStat?)CombatUpgradeCatalog.All[crystalOffers[0]].OfferType : null;
                int offer = RandomCrystalOffer(compatible, typeToAvoid);
                if (offer < 0) offer = RandomCrystalOffer(available, typeToAvoid);
                if (offer < 0) break;
                crystalOffers.Add(offer);
                compatible.Remove(offer);
                available.Remove(offer);
            }
        }

        private static int RandomCrystalOffer(List<int> pool, RunStat? typeToAvoid)
        {
            if (typeToAvoid.HasValue)
                pool = pool.FindAll(i => CombatUpgradeCatalog.All[i].OfferType != typeToAvoid.Value);
            return pool.Count == 0 ? -1 : pool[UnityEngine.Random.Range(0, pool.Count)];
        }

        internal bool UpgradeMatchesLoadout(CombatUpgradeCatalog.Upgrade upgrade)
        {
            for (int slot = 1; slot < PortalWeaponLoadout.SlotCount; slot++)
            {
                var weapon = balance.FindWeapon(WeaponInBattleSlot(slot));
                if (weapon != null && upgrade.Applies(weapon)) return true;
            }
            return false;
        }

        public bool ChooseCrystalUpgrade(int index)
        {
            if (expeditionEnded || !UpgradeCrystalChoice.HasValue || index < 0 || index >= crystalOffers.Count) return false;
            if (!UpgradeMatchesLoadout(CombatUpgradeCatalog.All[crystalOffers[index]])) return false;
            chosenCrystalUpgrades.Add(crystalOffers[index], UpgradeCrystalChoice.Value);
            CloseUpgradeCrystal();
            GameAudioController.PlayUI(GameAudioCue.ContentUnlock);
            return true;
        }

        internal int RunAdjustment(PortalTowerBalance.Weapon weapon, RunStat stat)
        {
            if (chosenCrystalUpgrades.Count == 0) return 0;
            var definition = balance.FindWeapon(weapon);
            if (definition == null) return 0;
            int result = 0;
            foreach (var entry in chosenCrystalUpgrades)
            {
                var upgrade = CombatUpgradeCatalog.All[entry.Key];
                if (!upgrade.Applies(definition)) continue;
                if (upgrade.bonus == stat) result += upgrade.benefit;
                if (upgrade.drawback == stat) result += upgrade.Penalty(entry.Value);
            }
            return result;
        }

        internal float RunMultiplier(PortalTowerBalance.Weapon weapon, RunStat stat)
        {
            var definition = balance.FindWeapon(weapon);
            if (definition == null) return 1;
            float multiplier = 1;
            // Each percentage modifies the current value. Two speed cards must never add up to a free 90% reduction.
            foreach (var entry in chosenCrystalUpgrades)
            {
                var upgrade = CombatUpgradeCatalog.All[entry.Key];
                if (!upgrade.Applies(definition)) continue;
                if (upgrade.bonus == stat) multiplier *= 1 + upgrade.benefit / 100f;
                if (upgrade.drawback == stat) multiplier *= 1 + upgrade.Penalty(entry.Value) / 100f;
            }
            return Mathf.Max(stat is RunStat.Energy or RunStat.Drain ? 1 : .1f, multiplier);
        }

        internal static PortalTowerProgression FromAttack(Transform attacker) =>
            attacker != null && attacker.TryGetComponent<ArcherTower>(out var tower) ? tower.PortalProgression : null;

        public void DeclineUpgradeCrystal()
        {
            if (!UpgradeCrystalChoice.HasValue || expeditionEnded) return;
            CloseUpgradeCrystal();
            GameAudioController.PlayUI(GameAudioCue.UiConfirm);
        }

        private void CloseUpgradeCrystal()
        {
            UpgradeCrystalChoice = null;
            crystalOffers.Clear();
            if (expeditionEnded)
            {
                upgradeWindow.SetActive(false);
                FinishUpgradeWindow();
            }
            else GameFoundation.UI.PopupMotion.Hide(upgradeWindow, FinishUpgradeWindow);
        }
        private void FinishUpgradeWindow()
        {
            RestoreInterface();
            GameSpeedControls.SetSimulationSpeed(previousSpeed);
            Changed?.Invoke();
        }

        private readonly EnemyCrystalDropBudget enemyCrystalBudget = new();
        private readonly HashSet<HexBlocker> openedCrystalBudgetHexes = new();
        public int EnemyCrystalDropLimit => enemyCrystalBudget.Limit;
        public int EnemyCrystalsDropped => enemyCrystalBudget.DroppedCrystals;
        public int EnemyCrystalsRemaining => enemyCrystalBudget.Remaining;
        private readonly Dictionary<string, int> ranks = new();
        private readonly Dictionary<string, float> acquiredValues = new();
        private float AcquiredValue(PortalTowerBalance.Upgrade u) => Rank(u.id) == 0 ? 0 : acquiredValues.TryGetValue(u.id, out float total) ? total : u.value * Rank(u.id);
        private PortalUpgradeModifiers modifiers;
        private PortalUpgradeModifiers Modifiers => modifiers ??= new PortalUpgradeModifiers(balance.upgrades, Rank, AcquiredValue);
        private readonly List<PortalTowerBalance.Upgrade> offers = new();
        public IReadOnlyList<PortalTowerBalance.Upgrade> Offers => offers;
        public bool IsInitialWeaponChoice { get; private set; }
        public bool IsGuaranteedLightChoice { get; private set; }
        private int guaranteedLights;
        private readonly HashSet<int> claimedLightLevels = new();
        public static bool IsLightRewardLevel(int level) => level == 2 || (level >= 4 && level % 4 == 0);
        public int ChoiceLevel => IsInitialWeaponChoice ? 1 : Level - LevelPoints + 1;
        public bool HasWeapon(PortalTowerBalance.Weapon weapon) => UsesArsenal ? WeaponsAvailable && !expeditionEnded && ActiveWeapon == weapon && OwnsWeapon(weapon)
            : CrystalTechnologyAvailable && balance.IsWeaponEnabled(weapon) && balance.upgrades.Exists(u =>
                u.weapon == weapon && u.effect == PortalTowerBalance.Effect.UnlockWeapon && Rank(u.id) > 0);
        public int WeaponCount => balance.weapons.FindAll(w => HasWeapon(w.weapon)).Count;
        private float previousSpeed;
        private bool expeditionEnded;
        private CanvasGroup outerInterface;
        private bool previousInteractable;
        private bool previousRaycasts;
        private GameObject previousSelection;
        public event Action Changed;
        public PortalTowerBalance Balance => balance;
        public bool CrystalTechnologyAvailable { get; private set; }
        public bool CanReceiveWaveReward => CrystalTechnologyAvailable && !expeditionEnded && balance != null && !IsChoosingUpgrade;
        public void AwardWaveCrystals(int amount)
        {
            if (CanReceiveWaveReward) AddExperience(amount);
        }
        public ResourceType ExperienceCrystalResource
        {
            get
            {
                if (crystalResource != null) return crystalResource;
                var source = experienceCrystalPrefab != null ? experienceCrystalPrefab.GetComponent<SpriteRenderer>() : null;
                if (source == null || source.sprite == null) return null;
                crystalResource = ScriptableObject.CreateInstance<ResourceType>();
                crystalResource.name = "Run Experience Crystal";
                crystalResource.hideFlags = HideFlags.DontSave;
                crystalResource.resourceName = "Experience Crystal";
                crystalResource.notifyResourceChanges = false;
                crystalResource.resourceIcon = crystalResource.defaultCarrySprite = source.sprite;
                crystalResource.groundSprite = groundExperienceCrystal != null ? groundExperienceCrystal : source.sprite;
                return crystalResource;
            }
        }
        public int Level { get; private set; } = 1;
        public int Experience { get; private set; }
        public int PendingExperience { get; private set; }
        public int CrystalDebt { get; private set; }
        private int rerollPriceStep;
        private int lastRerollChoiceLevel = 1;
        public long RerollCost => 1L << Mathf.Min(rerollPriceStep, 62);
        public bool CanReroll => !UsesEnergyLoadout && !IsGuaranteedLightChoice && CrystalTechnologyAvailable && !expeditionEnded && LevelPoints > 0 && offers.Count > 0 && RerollCost <= RequiredExperience - CrystalDebt;
        public int PreviewExperience(int incoming) => Experience + Mathf.Max(0, incoming - CrystalDebt);
        public bool Reroll()
        {
            if (!CanReroll) return false;
            CrystalDebt += (int)RerollCost;
            rerollPriceStep++;
            RollOffers();
            EventSystem.current?.SetSelectedGameObject(null);
            GameAudioController.PlayUI(GameAudioCue.UiConfirm);
            Changed?.Invoke();
            return true;
        }
        private void ReceiveExperience(int amount)
        {
            if (UsesEnergyLoadout) { SetEnergy((int)Math.Min(EnergyCapacity, (long)Energy + (long)amount * Mathf.Max(1, balance.energyPerCrystal))); return; }
            int repayment = Mathf.Min(CrystalDebt, amount);
            CrystalDebt -= repayment;
            Experience += amount - repayment;
        }
        public int LevelPoints { get; private set; }
        public int RequiredExperience => ScientificUpgrades.RequiredCrystals(balance.RequiredExperience(Level), scientificStats != null ? scientificStats.CrystalEfficiencyRank : 0);
        public bool CanAttack => !expeditionEnded && HasWeapon(PortalTowerBalance.Weapon.Bolts) && CanPayForShot(PortalTowerBalance.Weapon.Bolts);
        private int ResearchLightCount => scientificStats != null ? scientificStats.StartingFlashlightCount : 1;
        public int PermanentLightCount => Mathf.Clamp((UsesEnergyLoadout ? 1 : ResearchLightCount) + guaranteedLights, 1, WorldFlashlightAvailability.MaximumCount);
        public int LightCount => UsesEnergyLoadout
            ? Mathf.Clamp(Mathf.Max(PermanentLightCount, 1 + PortalEnergyRules.BonusLights(balance, Energy, CrystalEfficiencyRank)), 1, WorldFlashlightAvailability.MaximumCount)
            : Mathf.Clamp(PermanentLightCount + Mathf.RoundToInt(EffectTotal(PortalTowerBalance.Effect.ExtraLight)), 1, WorldFlashlightAvailability.MaximumCount);
        public int ProjectileCount => Count(PortalTowerBalance.Weapon.Bolts);
        public float DamageMultiplier => Multiplier(PortalTowerBalance.Weapon.Bolts, PortalTowerBalance.Effect.Damage);
        public float WeaponDamage(PortalTowerBalance.Weapon weapon, float bonusDamage = 0)
        {
            var definition = balance.FindWeapon(weapon);
            if (definition == null) return 0;
            // Add flat milestones directly. Division followed by multiplication made 13 damage become 12.999999.
            float damage = (definition.damage + bonusDamage) * definition.BaseDamageScale(UsesEnergyLoadout);
            if (UsesEnergyLoadout) damage += PortalWeaponEnhancements.Damage(weapon);
            return damage * Multiplier(weapon, PortalTowerBalance.Effect.Damage);
        }
        public float WeaponDamageMultiplier(PortalTowerBalance.Weapon weapon) => (balance.FindWeapon(weapon)?.BaseDamageScale(UsesEnergyLoadout) ?? .5f) * Multiplier(weapon, PortalTowerBalance.Effect.Damage);
        public float AttackSpeedMultiplier => (balance.FindWeapon(PortalTowerBalance.Weapon.Bolts)?.cooldown ?? 1) / AttackInterval(PortalTowerBalance.Weapon.Bolts);
        public float RangeMultiplier => 1;
        public float AddedRange(PortalTowerBalance.Weapon w) => EffectTotal(PortalTowerBalance.Effect.Range, w);
        public float WeaponRange(PortalTowerBalance.Weapon w, float bonus = 0) =>
            Mathf.Max(0, ((balance.FindWeapon(w)?.range ?? 0) * Profile(w).Range + bonus + AddedRange(w)) * RunMultiplier(w, RunStat.Range));
        public float AttackInterval(PortalTowerBalance.Weapon w, float flatReduction = 0) => Mathf.Max(PortalTowerBalance.MinimumInterval, ((balance.FindWeapon(w)?.cooldown ?? 1) * Profile(w).Interval - flatReduction - (UsesEnergyLoadout ? PortalWeaponEnhancements.IntervalReduction(w) : 0)) * (1 - Mathf.Clamp01(EffectTotal(PortalTowerBalance.Effect.AttackInterval, w))) * RunMultiplier(w, RunStat.Interval));
        public float TickInterval(PortalTowerBalance.Weapon w) => Mathf.Max(PortalTowerBalance.MinimumInterval, Mathf.Max(1, balance.FindWeapon(w)?.damageTickInterval ?? 1) * (1 - Mathf.Clamp01(EffectTotal(PortalTowerBalance.Effect.TickInterval, w))) * RunMultiplier(w, RunStat.Tick));
        public float Multiplier(PortalTowerBalance.Weapon weapon, PortalTowerBalance.Effect effect)
        {
            float legacy = 1 + EffectTotal(effect, weapon);
            var profile = Profile(weapon);
            return effect switch
            {
                PortalTowerBalance.Effect.Damage => legacy * profile.Damage * RunMultiplier(weapon, RunStat.Damage),
                PortalTowerBalance.Effect.Area => legacy * profile.Area * RunMultiplier(weapon, RunStat.Area),
                PortalTowerBalance.Effect.Duration => legacy * profile.Duration * RunMultiplier(weapon, RunStat.Duration),
                PortalTowerBalance.Effect.ExpansionSpeed => legacy * profile.Speed * RunMultiplier(weapon, RunStat.Speed),
                _ => legacy
            };
        }
        public int Count(PortalTowerBalance.Weapon weapon)
        {
            var definition = balance.FindWeapon(weapon);
            var effect = definition != null && definition.deliveryMethod == PortalTowerBalance.DeliveryMethod.InstantHit
                ? PortalTowerBalance.Effect.Targets : PortalTowerBalance.Effect.Projectiles;
            return Mathf.Max(1, (definition?.count ?? 1) + Mathf.RoundToInt(EffectTotal(effect, weapon)) + RunAdjustment(weapon, RunStat.Projectiles));
        }
        public bool ExpeditionEnded => expeditionEnded;
        private void Awake()
        {
            Instance = this;
            UsesArsenal = balance != null && balance.useSelectableArsenal && balance.arsenal != null;
            UsesEnergyLoadout = UsesArsenal && balance.useEnergyLoadout;
            if (UsesEnergyLoadout)
            {
                for (int i = 0; i < weaponModes.Length; i++)
                    weaponModes[i] = PortalWeaponProfiles.Selected((PortalTowerBalance.Weapon)i);
                ActiveWeapon = PortalWeaponLoadout.Get(PortalWeaponLoadout.StartingSlot);
            }
            if (upgradeWindow != null) upgradeWindow.SetActive(false);
        }
        private void Start()
        {
            energyWeaponsAvailable = UsesEnergyLoadout && (PlaytestSettings.TutorialsAndQuestsDisabled ||
                !DayCycleService.IsFirstPortalRun && FirstRunLightTutorial.WeaponTutorialComplete);
            CrystalTechnologyAvailable = UsesEnergyLoadout || PlaytestSettings.TutorialsAndQuestsDisabled || (DayCycleService.Instance != null && DayCycleService.Instance.Day > 1);
            if (UsesEnergyLoadout)
            {
                if (!energyWeaponsAvailable) ActiveWeapon = PortalTowerBalance.Weapon.None;
                int startingLights = ResearchLightCount;
                if (startingLights > 1)
                    SetEnergy(PortalEnergyRules.LightThreshold(balance, startingLights - 2, CrystalEfficiencyRank), notifyLightChange: false);
            }
            if (UsesArsenal)
            {
                PortalArsenalProgress.RecordPopulation(); PortalArsenalProgress.RecordLevel(1);
                if (!UsesEnergyLoadout) gameObject.AddComponent<PortalArsenalLauncher>().Initialize(this);
            }
            if (!CrystalTechnologyAvailable) { Changed?.Invoke(); return; }
            if (balance == null || upgradeWindow == null) { Changed?.Invoke(); return; }
            var tutorial = UnityEngine.Object.FindFirstObjectByType<FirstRunLightTutorial>();
            if (!UsesEnergyLoadout && tutorial != null && tutorial.TryExplainLegacyCrystals(BeginInitialWeaponChoice)) { Changed?.Invoke(); return; }
            BeginInitialWeaponChoice();
        }
        private void BeginInitialWeaponChoice()
        {
            if (expeditionEnded) return;
            if (UsesArsenal) { Changed?.Invoke(); return; }
            IsInitialWeaponChoice = true;
            LevelPoints = 1;
            ShowChoice();
            Changed?.Invoke();
        }
        private void ShowChoice()
        {
            if (!RollPendingOffers()) return;
            previousSpeed = PortalArsenalPopup.ReleaseForUpgrade(GameSpeedControls.SimulationSpeed);
            GameSpeedControls.SetSimulationSpeed(0);
            BlockInterface();
            upgradeWindow.SetActive(true);
            GameFoundation.UI.PopupMotion.ShowDimmed(upgradeWindow);
            EventSystem.current?.SetSelectedGameObject(null);
        }
#if UNITY_EDITOR
        private void Update()
        {
            if (!PortalArsenalPopup.IsOpen && !EditorWaveCheatPopup.IsOpen && !FirstRunLightTutorial.BlocksInput && Input.GetKeyDown(KeyCode.K)) GrantEditorLevel();
        }
        public void GrantEditorLevel()
        {
            if (!CrystalTechnologyAvailable || !Application.isPlaying || expeditionEnded || LevelPoints > 0) return;
            if (UsesEnergyLoadout) { AddExperience(10); return; }
            AddExperience(CrystalDebt + RequiredExperience - Experience);
        }
#endif
        public int Rank(string id) => ranks.TryGetValue(id, out int rank) ? rank : 0;
        public bool IsComplete(PortalTowerBalance.Upgrade upgrade) =>
            (upgrade.effect == PortalTowerBalance.Effect.ExtraLight && LightCount >= WorldFlashlightAvailability.MaximumCount) ||
            (upgrade.maximumRank > 0 && Rank(upgrade.id) >= upgrade.maximumRank);
        public bool IsUnlocked(PortalTowerBalance.Upgrade upgrade)
        {
            if (upgrade == null) return false;
            if (upgrade.effect == PortalTowerBalance.Effect.UnlockWeapon)
                return !UsesArsenal && (IsInitialWeaponChoice || (ChoiceLevel >= 5 && ChoiceLevel % 5 == 0)) &&
                    upgrade.weapon != PortalTowerBalance.Weapon.None && balance.IsWeaponEnabled(upgrade.weapon) && !HasWeapon(upgrade.weapon);
            if (upgrade.scope == PortalTowerBalance.UpgradeScope.Portal) return true;
            return balance.weapons.Exists(w => OwnsWeapon(w.weapon) && PortalUpgradeModifiers.Applies(upgrade, w));
        }
        public bool CanPurchase(PortalTowerBalance.Upgrade upgrade) => !UsesEnergyLoadout && CrystalTechnologyAvailable &&
            upgrade != null && LevelPoints > 0 && offers.Contains(upgrade) && !IsComplete(upgrade) && IsUnlocked(upgrade);
        private void RollOffers()
        {
            offers.Clear();
            var weapons = balance.upgrades.FindAll(u => u.effect == PortalTowerBalance.Effect.UnlockWeapon && IsUnlocked(u));
            var upgrades = balance.upgrades.FindAll(u => u.effect != PortalTowerBalance.Effect.UnlockWeapon && u.effect != PortalTowerBalance.Effect.ExtraLight && !IsComplete(u) && IsUnlocked(u));
            void Draw(List<PortalTowerBalance.Upgrade> pool, int count)
            {
                while (count-- > 0 && pool.Count > 0)
                {
                    int index = UnityEngine.Random.Range(0,pool.Count); offers.Add(pool[index].CreateOffer(PortalTowerBalance.RollQuality(UnityEngine.Random.value))); pool.RemoveAt(index);
                }
            }
            if (IsInitialWeaponChoice) Draw(weapons,3);
            else
            {
                if (ChoiceLevel >= 5 && ChoiceLevel % 5 == 0) Draw(weapons,1);
                Draw(upgrades,3-offers.Count);
            }
        }
        private bool RollPendingOffers()
        {
            while (LevelPoints > 0)
            {
                int advancedLevels = ChoiceLevel - lastRerollChoiceLevel;
                if (advancedLevels > 0)
                {
                    rerollPriceStep = Mathf.Max(0, rerollPriceStep - 3 * advancedLevels);
                    lastRerollChoiceLevel = ChoiceLevel;
                }
                IsGuaranteedLightChoice = false;
                var lightReward = balance.upgrades.Find(u => u.effect == PortalTowerBalance.Effect.ExtraLight);
                if (!IsInitialWeaponChoice && IsLightRewardLevel(ChoiceLevel) && !claimedLightLevels.Contains(ChoiceLevel) &&
                    LightCount < WorldFlashlightAvailability.MaximumCount && lightReward != null)
                {
                    IsGuaranteedLightChoice = true;
                    offers.Clear(); offers.Add(lightReward.CreateOffer(PortalTowerBalance.Quality.Common));
                    return true;
                }
                RollOffers();
                if (offers.Count > 0) return true;
                LevelPoints--;
            }
            return false;
        }
        public Vector2Int HexCrystalDropRange
        {
            get
            {
                int minimum = Mathf.Clamp(balance.minimumCrystalsPerEmptyHex, 1, 32);
                int maximum = Mathf.Clamp(balance.maximumCrystalsPerEmptyHex, minimum, 32);
                int extra = scientificStats != null ? scientificStats.ExtraHexCrystals : 0;
                return new Vector2Int(minimum + extra, maximum + extra);
            }
        }
        public void AwardHexExperience(Vector3 position)
        {
            var range = HexCrystalDropRange;
            DropExperienceCrystals(UnityEngine.Random.Range(range.x, range.y + 1), position, true);
        }
        public void RegisterOpenedHex(HexBlocker hex)
        {
            if (expeditionEnded || hex == null || hex.gameObject.scene != gameObject.scene ||
                !openedCrystalBudgetHexes.Add(hex)) return;
            enemyCrystalBudget.RegisterOpenedHex();
            if (UsesArsenal) PortalArsenalProgress.RecordHex();
        }

        public void AwardEnemyExperience(Vector3 position)
        {
            if (!CrystalTechnologyAvailable || expeditionEnded) return;
            if (UsesEnergyLoadout && upgradeWindow != null)
                TryDropUpgradeCrystal(position, AlarmSystem.Instance != null ? AlarmSystem.Instance.ActiveDangerLevel : 0);
            if (enemyCrystalBudget.Remaining <= 0) return;
            if (enemyCrystalBudget.CanDrop(UnityEngine.Random.value) && DropExperienceCrystals(1, position, false))
                enemyCrystalBudget.RecordDrop();
        }

        public void AwardDepletedHexExperience(GameObject depletedObject, Vector3 hexCenter)
        {
            if (CrystalTechnologyAvailable && !expeditionEnded && depletedObject != null && depletedObject.scene == gameObject.scene)
                StartCoroutine(WaitForClearedHex(depletedObject, hexCenter));
        }

        private IEnumerator WaitForClearedHex(GameObject depletedObject, Vector3 hexCenter)
        {
            // Resource prefabs hide their visuals first, then deactivate after their final animation.
            while (depletedObject != null && depletedObject.activeInHierarchy && !expeditionEnded) yield return null;
            if (!expeditionEnded && gameObject.scene.isLoaded) AwardHexExperience(hexCenter);
        }

        private bool DropExperienceCrystals(int amount, Vector3 position, bool spreadAcrossHex)
        {
            if (!CrystalTechnologyAvailable || expeditionEnded || amount <= 0) return false;
            var source = experienceCrystalPrefab != null ? experienceCrystalPrefab.GetComponent<SpriteRenderer>() : null;
            if (source == null || source.sprite == null)
            {
                Debug.LogError("Portal tower requires an experience crystal sprite for ground drops.", this);
                return false;
            }
            _ = ExperienceCrystalResource;
            var positions = new List<Vector2>(amount);
            for (int i = 0; i < amount; i++)
            {
                Vector2 offset = Vector2.zero;
                for (int attempt = 0; attempt < 32; attempt++)
                {
                    if (spreadAcrossHex)
                    {
                        // Inset hexagon: leave a margin along every edge of the tile.
                        float y = UnityEngine.Random.Range(-1.1f, 1.1f);
                        float width = 1.4f * (1 - .5f * Mathf.Abs(y) / 1.1f);
                        offset = new Vector2(UnityEngine.Random.Range(-width, width), y);
                    }
                    else offset = UnityEngine.Random.insideUnitCircle * .45f;
                    if (!positions.Exists(p => (p - offset).sqrMagnitude < .09f)) break;
                }
                positions.Add(offset);
                SpawnGroundCrystal(crystalResource, position + (Vector3)offset);
            }
            return true;
        }

        private bool SpawnGroundCrystal(ResourceType type, Vector3 position, bool pulse = false)
        {
            if (type == null || experienceCrystalPrefab == null ||
                !experienceCrystalPrefab.TryGetComponent<SpriteRenderer>(out var source)) return false;
            var drop = new GameObject(type.name, typeof(SpriteRenderer));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(drop, gameObject.scene);
            drop.transform.position = position;
            drop.transform.localScale = experienceCrystalPrefab.transform.localScale;
            var visual = drop.GetComponent<SpriteRenderer>();
            visual.sprite = type.groundSprite;
            visual.color = source.color;
            visual.sharedMaterial = source.sharedMaterial;
            visual.sortingLayerID = source.sortingLayerID;
            visual.sortingOrder = 2;
            var item = drop.AddComponent<ResourceItem>();
            item.type = type;
            item.carrySprite = type.defaultCarrySprite;
            item.weight = .1f;
            if (pulse) drop.AddComponent<UpgradeCrystalPulse>();
            GameAudioController.PlayResourceGain(.4f);
            return true;
        }

        public bool TryDeliverCrystal(ResourceType type, Vector3 portalPosition)
        {
            foreach (var entry in upgradeCrystals)
            {
                if (entry.Value != type) continue;
                if (!expeditionEnded && upgradeWindow != null) pendingUpgradeCrystals.Enqueue(entry.Key);
                return true;
            }
            if (crystalResource == null || type != crystalResource) return false;
            AwardExperience(1, portalPosition);
            if (UsesEnergyLoadout && CrystalTechnologyAvailable && !expeditionEnded) EnergyCrystalDelivered?.Invoke();
            return true;
        }
        private void AwardExperience(int amount, Vector3 position)
        {
            if (!CrystalTechnologyAvailable || expeditionEnded || amount <= 0) return;
            if (experienceCrystalPrefab != null)
            {
                var bar=FindFirstObjectByType<PortalTowerExperienceBar>();
                if(bar!=null)
                {
                    int count=Mathf.Min(amount,32);
                    PendingExperience+=amount;
                    Changed?.Invoke();
                    int targetExperience=Experience+PendingExperience-amount;
                    for(int i=0;i<count;i++)
                    {
                        int units=amount/count+(i<amount%count?1:0);
                        targetExperience+=units;
                        int incoming = targetExperience - Experience;
                        float targetFill=UsesEnergyLoadout ? Mathf.Clamp01((float)(Energy + incoming * balance.energyPerCrystal) / EnergyCapacity) : Mathf.Clamp01((float)(incoming < CrystalDebt ? CrystalDebt - incoming : PreviewExperience(incoming))/RequiredExperience);
                        Instantiate(experienceCrystalPrefab,position,Quaternion.identity).Launch(position,bar,i*.06f,()=>CommitIncomingExperience(units),targetFill);
                    }
                    return;
                }
            }
            AddExperience(amount);
        }
        private void CommitIncomingExperience(int amount)
        {
            if(!CrystalTechnologyAvailable || expeditionEnded)return;
            PendingExperience=Mathf.Max(0,PendingExperience-amount);
            ReceiveExperience(amount);
            Changed?.Invoke();
            if(!UsesEnergyLoadout && Experience>=RequiredExperience)StartCoroutine(DelayedLevel(Experience+processedExperience));
        }
        private IEnumerator DelayedLevel(int arrivedExperience)
        {
            yield return new WaitForSecondsRealtime(.5f);
            while (FirstRunLightTutorial.BlocksInput && !expeditionEnded) yield return null;
            if(expeditionEnded)yield break;
            // Only experience that had arrived when this delay began can level up.
            ProcessLevels(arrivedExperience);
        }
        public void AddExperience(int amount)
        {
            if (!CrystalTechnologyAvailable || amount <= 0 || balance == null || expeditionEnded) return;
            ReceiveExperience(amount);
            if (!UsesEnergyLoadout) ProcessLevels(Experience+processedExperience);
        }
        private int processedExperience;
        private void ProcessLevels(int availableExperience)
        {
            if (CrystalDebt > 0) { Changed?.Invoke(); return; }
            bool gainedLevel = false;
            int eligible=availableExperience-processedExperience;
            while (eligible >= RequiredExperience)
            {
                int required=RequiredExperience;
                eligible-=required;
                processedExperience+=required;
                Experience -= required;
                Level++;
                if (UsesArsenal) PortalArsenalProgress.RecordLevel(Level);
                LevelPoints++;
                gainedLevel = true;
            }
            if (gainedLevel)
            {
                if (upgradeWindow != null && !upgradeWindow.activeSelf)
                {
                    ShowChoice();
                }
                GameAudioController.PlayUI(GameAudioCue.ContentUnlock);
            }
            Changed?.Invoke();
        }
        public bool Purchase(string id)
        {
            var upgrade = offers.Find(u => u.id == id);
            if (!CanPurchase(upgrade)) return false;
            if (IsGuaranteedLightChoice)
            {
                claimedLightLevels.Add(ChoiceLevel);
                guaranteedLights++;
                IsGuaranteedLightChoice = false;
                // The reward does not consume the normal upgrade choice for this level.
                RollPendingOffers();
                GameAudioController.PlayUI(GameAudioCue.UiConfirm);
                Changed?.Invoke();
                if (LevelPoints == 0)
                {
                    GameFoundation.UI.PopupMotion.Hide(upgradeWindow, FinishUpgradeWindow);
                }
                return true;
            }
            acquiredValues[id] = AcquiredValue(upgrade) + upgrade.OfferedValue;
            ranks[id] = Rank(id) + 1;
            IsInitialWeaponChoice = false;
            LevelPoints--;
            if (LevelPoints > 0) RollPendingOffers(); else offers.Clear();
            GameAudioController.PlayUI(GameAudioCue.UiConfirm);
            // Notify light ownership before closing the mandatory modal.
            Changed?.Invoke();
            if (LevelPoints == 0)
            {
                GameFoundation.UI.PopupMotion.Hide(upgradeWindow, FinishUpgradeWindow);
            }
            return true;
        }
        public void EndExpedition()
        {
            expeditionEnded = true;
            PendingExperience=0;
            pendingUpgradeCrystals.Clear();
            if (UpgradeCrystalChoice.HasValue) CloseUpgradeCrystal();
            Changed?.Invoke();
        }
        private void BlockInterface()
        {
            var canvas = GetComponentInParent<Canvas>()?.rootCanvas;
            if (canvas == null) return;
            outerInterface = canvas.GetComponent<CanvasGroup>();
            if (outerInterface == null) outerInterface = canvas.gameObject.AddComponent<CanvasGroup>();
            previousInteractable = outerInterface.interactable;
            previousRaycasts = outerInterface.blocksRaycasts;
            outerInterface.interactable = false;
            outerInterface.blocksRaycasts = false;
            previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            EventSystem.current?.SetSelectedGameObject(null);
        }
        private void RestoreInterface()
        {
            if (outerInterface != null)
            {
                outerInterface.interactable = previousInteractable;
                outerInterface.blocksRaycasts = previousRaycasts;
            }
            EventSystem.current?.SetSelectedGameObject(previousSelection != null && previousSelection.activeInHierarchy ? previousSelection : null);
            outerInterface = null; previousSelection = null;
        }
        private float EffectTotal(PortalTowerBalance.Effect effect, PortalTowerBalance.Weapon weapon = PortalTowerBalance.Weapon.None)
        {
            if (balance == null) return 0;
            if (weapon == PortalTowerBalance.Weapon.None) return Modifiers.PortalTotal(effect);
            var definition = balance.FindWeapon(weapon);
            return definition != null ? Modifiers.Total(effect, definition) : 0;
        }
        private void OnDestroy()
        {
            if (crystalResource != null) Destroy(crystalResource);
            foreach (var type in upgradeCrystals.Values) Destroy(type);
            if (Instance != this) return;
            bool paused = LevelPoints > 0 || UpgradeCrystalChoice.HasValue;
            Instance = null;
            if (paused) { RestoreInterface(); GameSpeedControls.SetSimulationSpeed(previousSpeed); }
        }
    }
}
