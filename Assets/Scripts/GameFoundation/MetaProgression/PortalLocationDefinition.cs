using System.Collections.Generic;
using UnityEngine;

namespace GameFoundation.MetaProgression
{
    [CreateAssetMenu(menuName = "Game Foundation/Portal Location", fileName = "Portal Location")]
    public sealed class PortalLocationDefinition : ScriptableObject
    {
        [System.Serializable]
        public sealed class ResourceHint
        {
            public ResourceType resource;
            public string abundanceKey;
            public string fallbackAbundance;
        }

        [SerializeField] private string locationId;
        [SerializeField, Range(1, 5)] private int difficulty = 1;
        [SerializeField, Min(0)] private int activationCost;
        [SerializeField] private string nameKey;
        [SerializeField] private string fallbackName;
        [SerializeField] private string descriptionKey;
        [SerializeField, TextArea] private string fallbackDescription;
        [SerializeField] private List<ResourceHint> resources = new();

        public string LocationId => locationId;
        public int Difficulty => difficulty;
        public int ActivationCost => activationCost;
        public string NameKey => nameKey;
        public string FallbackName => fallbackName;
        public string DescriptionKey => descriptionKey;
        public string FallbackDescription => fallbackDescription;
        public IReadOnlyList<ResourceHint> Resources => resources;
    }
}
