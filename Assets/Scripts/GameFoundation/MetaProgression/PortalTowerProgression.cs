using System;
using System.Collections;
using System.Collections.Generic;
using GameFoundation.Audio;
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
        public static bool IsChoosingUpgrade => Instance != null && Instance.LevelPoints > 0;
        [SerializeField] private PortalTowerBalance balance;
        [SerializeField] private GameObject upgradeWindow;
        [Header("Получение опыта")]
        [SerializeField] private PortalExperienceCrystal experienceCrystalPrefab;
        [SerializeField] private Sprite groundExperienceCrystal;
        private ResourceType crystalResource;
        private readonly Dictionary<string, int> ranks = new();
        private readonly Dictionary<string, float> acquiredValues = new();
        private float AcquiredValue(PortalTowerBalance.Upgrade u) => Rank(u.id) == 0 ? 0 : acquiredValues.TryGetValue(u.id, out float total) ? total : u.value * Rank(u.id);
        private PortalUpgradeModifiers modifiers;
        private PortalUpgradeModifiers Modifiers => modifiers ??= new PortalUpgradeModifiers(balance.upgrades, Rank, AcquiredValue);
        private readonly List<PortalTowerBalance.Upgrade> offers = new();
        public IReadOnlyList<PortalTowerBalance.Upgrade> Offers => offers;
        public bool IsInitialWeaponChoice { get; private set; }
        public int ChoiceLevel => IsInitialWeaponChoice ? 1 : Level - LevelPoints + 1;
        public bool HasWeapon(PortalTowerBalance.Weapon weapon) => CrystalTechnologyAvailable && balance.IsWeaponEnabled(weapon) && balance.upgrades.Exists(u =>
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
        public int Level { get; private set; } = 1;
        public int Experience { get; private set; }
        public int PendingExperience { get; private set; }
        public int CrystalDebt { get; private set; }
        private int rerollPriceStep;
        private int lastRerollChoiceLevel = 1;
        public long RerollCost => 1L << Mathf.Min(rerollPriceStep, 62);
        public bool CanReroll => CrystalTechnologyAvailable && !expeditionEnded && LevelPoints > 0 && offers.Count > 0 && RerollCost <= RequiredExperience - CrystalDebt;
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
            int repayment = Mathf.Min(CrystalDebt, amount);
            CrystalDebt -= repayment;
            Experience += amount - repayment;
        }
        public int LevelPoints { get; private set; }
        public int RequiredExperience => balance.RequiredExperience(Level);
        public bool CanAttack => !expeditionEnded && HasWeapon(PortalTowerBalance.Weapon.Bolts);
        public int LightCount => Mathf.Clamp(1 + Mathf.RoundToInt(EffectTotal(PortalTowerBalance.Effect.ExtraLight)), 1, WorldFlashlightAvailability.MaximumCount);
        public int ProjectileCount => Count(PortalTowerBalance.Weapon.Bolts);
        public float DamageMultiplier => Multiplier(PortalTowerBalance.Weapon.Bolts, PortalTowerBalance.Effect.Damage);
        public float AttackSpeedMultiplier => (balance.FindWeapon(PortalTowerBalance.Weapon.Bolts)?.cooldown ?? 1) / AttackInterval(PortalTowerBalance.Weapon.Bolts);
        public float RangeMultiplier => 1;
        public float AddedRange(PortalTowerBalance.Weapon w) => EffectTotal(PortalTowerBalance.Effect.Range, w);
        public float AttackInterval(PortalTowerBalance.Weapon w, float flatReduction = 0) => Mathf.Max(PortalTowerBalance.MinimumInterval, ((balance.FindWeapon(w)?.cooldown ?? 1) - flatReduction) * (1 - Mathf.Clamp01(EffectTotal(PortalTowerBalance.Effect.AttackInterval, w))));
        public float TickInterval(PortalTowerBalance.Weapon w) => Mathf.Max(PortalTowerBalance.MinimumInterval, Mathf.Max(1, balance.FindWeapon(w)?.damageTickInterval ?? 1) * (1 - Mathf.Clamp01(EffectTotal(PortalTowerBalance.Effect.TickInterval, w))));
        public float Multiplier(PortalTowerBalance.Weapon weapon, PortalTowerBalance.Effect effect) => 1 + EffectTotal(effect, weapon);
        public int Count(PortalTowerBalance.Weapon weapon)
        {
            var definition = balance.FindWeapon(weapon);
            var effect = definition != null && definition.deliveryMethod == PortalTowerBalance.DeliveryMethod.InstantHit
                ? PortalTowerBalance.Effect.Targets : PortalTowerBalance.Effect.Projectiles;
            return Mathf.Max(1, (definition?.count ?? 1) + Mathf.RoundToInt(EffectTotal(effect, weapon)));
        }
        public bool ExpeditionEnded => expeditionEnded;
        private void Awake()
        {
            Instance = this;
            if (upgradeWindow != null) upgradeWindow.SetActive(false);
        }
        private void Start()
        {
            CrystalTechnologyAvailable = PlaytestSettings.TutorialsAndQuestsDisabled || (DayCycleService.Instance != null && DayCycleService.Instance.Day > 1);
            if (!CrystalTechnologyAvailable) { Changed?.Invoke(); return; }
            if (balance == null || upgradeWindow == null) return;
            IsInitialWeaponChoice = true;
            LevelPoints = 1;
            ShowChoice();
            Changed?.Invoke();
        }
        private void ShowChoice()
        {
            if (!RollPendingOffers()) return;
            previousSpeed = GameSpeedControls.SimulationSpeed;
            GameSpeedControls.SetSimulationSpeed(0);
            BlockInterface();
            upgradeWindow.SetActive(true);
            EventSystem.current?.SetSelectedGameObject(null);
        }
#if UNITY_EDITOR
        private void Update()
        {
            if (!FirstRunLightTutorial.BlocksInput && Input.GetKeyDown(KeyCode.K)) GrantEditorLevel();
        }
        public void GrantEditorLevel()
        {
            if (!CrystalTechnologyAvailable || !Application.isPlaying || expeditionEnded || LevelPoints > 0) return;
            AddExperience(CrystalDebt + RequiredExperience - Experience);
        }
#endif
        public int Rank(string id) => ranks.TryGetValue(id, out int rank) ? rank : 0;
        public bool IsComplete(PortalTowerBalance.Upgrade upgrade) => upgrade.maximumRank > 0 && Rank(upgrade.id) >= upgrade.maximumRank;
        public bool IsUnlocked(PortalTowerBalance.Upgrade upgrade)
        {
            if (upgrade == null) return false;
            if (upgrade.effect == PortalTowerBalance.Effect.UnlockWeapon)
                return (IsInitialWeaponChoice || (ChoiceLevel >= 5 && ChoiceLevel % 5 == 0)) &&
                    upgrade.weapon != PortalTowerBalance.Weapon.None && balance.IsWeaponEnabled(upgrade.weapon) && !HasWeapon(upgrade.weapon);
            if (upgrade.scope == PortalTowerBalance.UpgradeScope.Portal) return true;
            return balance.weapons.Exists(w => HasWeapon(w.weapon) && PortalUpgradeModifiers.Applies(upgrade, w));
        }
        public bool CanPurchase(PortalTowerBalance.Upgrade upgrade) => CrystalTechnologyAvailable && upgrade != null && LevelPoints > 0 && offers.Contains(upgrade) && !IsComplete(upgrade) && IsUnlocked(upgrade);
        private void RollOffers()
        {
            offers.Clear();
            var weapons = balance.upgrades.FindAll(u => u.effect == PortalTowerBalance.Effect.UnlockWeapon && IsUnlocked(u));
            var upgrades = balance.upgrades.FindAll(u => u.effect != PortalTowerBalance.Effect.UnlockWeapon && !IsComplete(u) && IsUnlocked(u));
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
                RollOffers();
                if (offers.Count > 0) return true;
                LevelPoints--;
            }
            return false;
        }
        public void AwardHexExperience(Vector3 position)
        {
            int minimum = Mathf.Clamp(balance.minimumCrystalsPerEmptyHex, 1, 32);
            int maximum = Mathf.Clamp(balance.maximumCrystalsPerEmptyHex, minimum, 32);
            DropExperienceCrystals(UnityEngine.Random.Range(minimum, maximum + 1), position, true);
        }
        public void AwardEnemyExperience(Vector3 position) => DropExperienceCrystals(balance.experiencePerEnemy, position, false);

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

        private void DropExperienceCrystals(int amount, Vector3 position, bool spreadAcrossHex)
        {
            if (!CrystalTechnologyAvailable || expeditionEnded || amount <= 0) return;
            var source = experienceCrystalPrefab != null ? experienceCrystalPrefab.GetComponent<SpriteRenderer>() : null;
            if (source == null || source.sprite == null)
            {
                Debug.LogError("Portal tower requires an experience crystal sprite for ground drops.", this);
                return;
            }
            if (crystalResource == null)
            {
                // This type exists only in this World scene and never enters the saved resource catalog.
                crystalResource = ScriptableObject.CreateInstance<ResourceType>();
                crystalResource.name = "Run Experience Crystal";
                crystalResource.hideFlags = HideFlags.DontSave;
                crystalResource.resourceName = "Experience Crystal";
                crystalResource.notifyResourceChanges = false;
                crystalResource.resourceIcon = crystalResource.defaultCarrySprite = source.sprite;
                crystalResource.groundSprite = groundExperienceCrystal != null ? groundExperienceCrystal : source.sprite;
            }
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
                var drop = new GameObject("Experience Crystal", typeof(SpriteRenderer));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(drop, gameObject.scene);
                drop.transform.position = position + (Vector3)offset;
                drop.transform.localScale = experienceCrystalPrefab.transform.localScale;
                var visual = drop.GetComponent<SpriteRenderer>();
                visual.sprite = crystalResource.groundSprite; visual.color = source.color;
                visual.sharedMaterial = source.sharedMaterial;
                visual.sortingLayerID = source.sortingLayerID; visual.sortingOrder = 2;
                var item = drop.AddComponent<ResourceItem>();
                item.type = crystalResource; item.carrySprite = source.sprite; item.weight = .1f;
            }
        }

        public bool TryDeliverCrystal(ResourceType type, Vector3 portalPosition)
        {
            if (crystalResource == null || type != crystalResource) return false;
            AwardExperience(1, portalPosition);
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
                        float targetFill=Mathf.Clamp01((float)(incoming < CrystalDebt ? CrystalDebt - incoming : PreviewExperience(incoming))/RequiredExperience);
                        Instantiate(experienceCrystalPrefab,position,Quaternion.identity).Launch(position,bar,i*.06f,i==count-1,()=>CommitIncomingExperience(units),targetFill);
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
            if(Experience>=RequiredExperience)StartCoroutine(DelayedLevel(Experience+processedExperience));
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
            ProcessLevels(Experience+processedExperience);
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
                upgradeWindow.SetActive(false);
                RestoreInterface();
                GameSpeedControls.SetSimulationSpeed(previousSpeed);
            }
            return true;
        }
        public void EndExpedition()
        {
            expeditionEnded = true;
            PendingExperience=0;
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
            if (Instance != this) return;
            bool paused = LevelPoints > 0;
            Instance = null;
            if (paused) { RestoreInterface(); GameSpeedControls.SetSimulationSpeed(previousSpeed); }
        }
    }
}
