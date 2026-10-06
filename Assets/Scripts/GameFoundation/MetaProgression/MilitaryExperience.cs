using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PlayerPrefs = GameFoundation.Saves.SaveSlotPrefs;

namespace GameFoundation.MetaProgression
{
    [Serializable]
    public sealed class MilitaryProfile
    {
        public string id;
        public string type;
        public int experience;
        // Kept as a percentage so level bonuses to maximum health remain compatible.
        public float healthPercent = 1f;
    }
    [Serializable] internal sealed class MilitaryProfileSave { public List<MilitaryProfile> profiles = new(); }

    public static class MilitaryExperienceService
    {
        private const string SaveKey = "military.experience";
        private static MilitaryProfileSave save;
        private static int loadedSlot = -1;
        private static readonly HashSet<string> deployed = new();

        private static MilitaryProfileSave SaveData
        {
            get
            {
                if (save != null && loadedSlot == PlayerPrefs.SelectedSlot) return save;
                loadedSlot = PlayerPrefs.SelectedSlot;
                deployed.Clear();
                save = PlayerPrefs.HasKey(SaveKey) ? JsonUtility.FromJson<MilitaryProfileSave>(PlayerPrefs.GetString(SaveKey)) : null;
                return save ??= new MilitaryProfileSave();
            }
        }

        public static IReadOnlyList<MilitaryProfile> GetStored(ResourceType resource, int requiredCount)
        {
            if (resource == null) return Array.Empty<MilitaryProfile>();
            List<MilitaryProfile> matching = SaveData.profiles.Where(profile => profile.type == resource.Id).ToList();
            while (matching.Count < requiredCount)
            {
                MilitaryProfile profile = new() { id = Guid.NewGuid().ToString("N"), type = resource.Id, healthPercent = 1f };
                SaveData.profiles.Add(profile); matching.Add(profile);
            }
            Persist();
            return matching.Where(profile => !deployed.Contains(profile.id)).Take(requiredCount).ToList();
        }

        public static void Deploy(MilitaryProfile profile) { if (profile != null) deployed.Add(profile.id); }
        public static void Return(MilitaryProfile profile) { if (profile != null) deployed.Remove(profile.id); }
        public static void Remove(MilitaryProfile profile)
        {
            if (profile == null) return;
            deployed.Remove(profile.id); SaveData.profiles.RemoveAll(item => item.id == profile.id); Persist();
        }
        public static void RemoveStored(ResourceType resource)
        {
            if (resource == null) return;
            MilitaryProfile profile = SaveData.profiles.Where(item => item.type == resource.Id && !deployed.Contains(item.id))
                .OrderBy(item => item.experience).FirstOrDefault();
            Remove(profile);
        }
        public static void Award(MilitaryProfile profile, int amount)
        {
            if (profile == null || amount <= 0) return;
            profile.experience = Mathf.Max(0, profile.experience + amount); Persist();
        }
        public static float HealthPercent(MilitaryProfile profile) => profile == null || profile.healthPercent <= 0f
            ? 1f : Mathf.Clamp01(profile.healthPercent);
        public static void StoreHealth(MilitaryProfile profile, float normalizedHealth)
        {
            if (profile == null) return;
            profile.healthPercent = Mathf.Clamp01(normalizedHealth);
            Persist();
        }
        public static void HealAll()
        {
            foreach (MilitaryProfile profile in SaveData.profiles) profile.healthPercent = 1f;
            Persist();
        }
        public static int Stars(MilitaryProfile profile) => profile == null ? 0 : Stars(profile.experience);
        public static int ExperienceForNextStar(int currentStars) => 50 + 25 * Mathf.Clamp(currentStars, 0, 9);
        public static int Stars(int experience)
        {
            int stars = 0, spent = 0;
            while (stars < 10 && experience >= spent + ExperienceForNextStar(stars))
            {
                spent += ExperienceForNextStar(stars);
                stars++;
            }
            return stars;
        }
        private static void Persist() { PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(SaveData)); PlayerPrefs.Save(); }
    }

    public sealed class MilitaryExperience : MonoBehaviour
    {
        // XP is intentionally stored on the persistent profile, never on a battle instance.
        public const int KillExperience = 10;
        public const int AssistExperience = 3;
        private MilitaryProfile profile;
        public MilitaryProfile Profile => profile;
        public int Stars => MilitaryExperienceService.Stars(profile);
        public float StatMultiplier => 1f + Stars * .1f;

        public void Initialize(MilitaryProfile value)
        {
            profile = value;
            MilitaryExperienceService.Deploy(profile);
            Health health = GetComponent<Health>();
            if (health != null) health.SetNormalizedHealth(MilitaryExperienceService.HealthPercent(profile));
        }
        public void AwardKill() => Award(KillExperience);
        public void AwardAssist() => Award(AssistExperience);
        public void Award(int amount) => MilitaryExperienceService.Award(profile, amount);
        public void CaptureHealth()
        {
            Health health = GetComponent<Health>();
            if (health != null) MilitaryExperienceService.StoreHealth(profile, health.NormalizedHealth);
        }
        public void ReturnToBase() => MilitaryExperienceService.Return(profile);
        public void Die() => MilitaryExperienceService.Remove(profile);
        public static float Multiplier(Component component) => component != null ? component.GetComponent<MilitaryExperience>()?.StatMultiplier ?? 1f : 1f;
    }
}
