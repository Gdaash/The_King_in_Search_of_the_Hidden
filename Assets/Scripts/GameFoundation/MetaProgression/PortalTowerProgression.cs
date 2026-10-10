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
        private readonly Dictionary<string, int> ranks = new();
        private readonly List<PortalTowerBalance.Upgrade> offers = new();
        public IReadOnlyList<PortalTowerBalance.Upgrade> Offers => offers;
        public int ChoiceLevel => Level - LevelPoints + 1;
        public bool HasWeapon(PortalTowerBalance.Weapon weapon) => balance.IsWeaponEnabled(weapon) && balance.upgrades.Exists(u =>
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
        public int Level { get; private set; } = 1;
        public int Experience { get; private set; }
        public int PendingExperience { get; private set; }
        public int LevelPoints { get; private set; }
        public int RequiredExperience => balance.RequiredExperience(Level);
        public bool CanAttack => !expeditionEnded && HasWeapon(PortalTowerBalance.Weapon.Bolts);
        public int LightCount => Mathf.Clamp(1 + Mathf.RoundToInt(EffectTotal(PortalTowerBalance.Effect.ExtraLight)), 1, WorldFlashlightAvailability.MaximumCount);
        public int ProjectileCount => Count(PortalTowerBalance.Weapon.Bolts);
        public float DamageMultiplier => Multiplier(PortalTowerBalance.Weapon.Bolts, PortalTowerBalance.Effect.Damage);
        public float AttackSpeedMultiplier => Multiplier(PortalTowerBalance.Weapon.Bolts, PortalTowerBalance.Effect.AttackSpeed);
        public float RangeMultiplier => Multiplier(PortalTowerBalance.Weapon.Bolts, PortalTowerBalance.Effect.Range);
        public float Multiplier(PortalTowerBalance.Weapon weapon, PortalTowerBalance.Effect effect) => 1 + EffectTotal(effect, weapon);
        public int Count(PortalTowerBalance.Weapon weapon) => Mathf.Max(1, (balance.FindWeapon(weapon)?.count ?? 1) + Mathf.RoundToInt(EffectTotal(PortalTowerBalance.Effect.Projectiles, weapon)));
        public bool ExpeditionEnded => expeditionEnded;
        private void Awake()
        {
            Instance = this;
            if (upgradeWindow != null) upgradeWindow.SetActive(false);
        }
#if UNITY_EDITOR
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.K)) GrantEditorLevel();
        }
        public void GrantEditorLevel()
        {
            if (!Application.isPlaying || expeditionEnded || LevelPoints > 0) return;
            AddExperience(RequiredExperience - Experience);
        }
#endif
        public int Rank(string id) => ranks.TryGetValue(id, out int rank) ? rank : 0;
        public bool IsComplete(PortalTowerBalance.Upgrade upgrade) => upgrade.maximumRank > 0 && Rank(upgrade.id) >= upgrade.maximumRank;
        public bool IsUnlocked(PortalTowerBalance.Upgrade upgrade) => balance.IsWeaponEnabled(upgrade.weapon) && (upgrade.effect == PortalTowerBalance.Effect.UnlockWeapon
            ? !HasWeapon(upgrade.weapon) : upgrade.weapon == PortalTowerBalance.Weapon.None || HasWeapon(upgrade.weapon));
        public bool CanPurchase(PortalTowerBalance.Upgrade upgrade) => upgrade != null && LevelPoints > 0 && offers.Contains(upgrade) && !IsComplete(upgrade) && IsUnlocked(upgrade);
        private void RollOffers()
        {
            offers.Clear();
            var weapons = balance.upgrades.FindAll(u => u.effect == PortalTowerBalance.Effect.UnlockWeapon && IsUnlocked(u));
            var upgrades = balance.upgrades.FindAll(u => u.effect != PortalTowerBalance.Effect.UnlockWeapon && !IsComplete(u) && IsUnlocked(u));
            void Draw(List<PortalTowerBalance.Upgrade> pool, int count)
            {
                while (count-- > 0 && pool.Count > 0)
                {
                    int index = UnityEngine.Random.Range(0,pool.Count); offers.Add(pool[index]); pool.RemoveAt(index);
                }
            }
            if (WeaponCount == 0) Draw(weapons,3);
            else
            {
                if (ChoiceLevel % 5 == 0) Draw(weapons,1);
                Draw(upgrades,3-offers.Count);
            }
        }
        public void AwardHexExperience(Vector3 position) => AwardExperience(balance.experiencePerHex, position);
        public void AwardEnemyExperience(Vector3 position) => AwardExperience(balance.experiencePerEnemy, position);
        private void AwardExperience(int amount, Vector3 position)
        {
            if (expeditionEnded || amount <= 0) return;
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
                        float targetFill=Mathf.Clamp01((float)targetExperience/RequiredExperience);
                        Instantiate(experienceCrystalPrefab,position,Quaternion.identity).Launch(position,bar,i*.06f,i==count-1,()=>CommitIncomingExperience(units),targetFill);
                    }
                    return;
                }
            }
            AddExperience(amount);
        }
        private void CommitIncomingExperience(int amount)
        {
            if(expeditionEnded)return;
            PendingExperience=Mathf.Max(0,PendingExperience-amount);
            Experience+=amount;
            Changed?.Invoke();
            if(Experience>=RequiredExperience)StartCoroutine(DelayedLevel(Experience+processedExperience));
        }
        private IEnumerator DelayedLevel(int arrivedExperience)
        {
            yield return new WaitForSecondsRealtime(.5f);
            if(expeditionEnded)yield break;
            // Only experience that had arrived when this delay began can level up.
            ProcessLevels(arrivedExperience);
        }
        public void AddExperience(int amount)
        {
            if (amount <= 0 || balance == null || expeditionEnded) return;
            Experience += amount;
            ProcessLevels(Experience+processedExperience);
        }
        private int processedExperience;
        private void ProcessLevels(int availableExperience)
        {
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
                    RollOffers();
                    previousSpeed = GameSpeedControls.SimulationSpeed;
                    GameSpeedControls.SetSimulationSpeed(0);
                    BlockInterface();
                    upgradeWindow.SetActive(true);
                    EventSystem.current?.SetSelectedGameObject(null);
                }
                GameAudioController.PlayUI(GameAudioCue.ContentUnlock);
            }
            Changed?.Invoke();
        }
        public bool Purchase(string id)
        {
            var upgrade = balance.Find(id);
            if (!CanPurchase(upgrade)) return false;
            ranks[id] = Rank(id) + 1;
            LevelPoints--;
            if (LevelPoints > 0) RollOffers(); else offers.Clear();
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
            float value = 0;
            if (balance != null)
                foreach (var upgrade in balance.upgrades)
                    if (upgrade.effect == effect && upgrade.weapon == weapon) value += upgrade.value * Rank(upgrade.id);
            return value;
        }
        private void OnDestroy()
        {
            if (Instance != this) return;
            bool paused = LevelPoints > 0;
            Instance = null;
            if (paused) { RestoreInterface(); GameSpeedControls.SetSimulationSpeed(previousSpeed); }
        }
    }
}
