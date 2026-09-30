using UnityEngine;

namespace GameFoundation.UI
{
    [CreateAssetMenu(menuName = "Game Foundation/UI/Unit description catalog")]
    public sealed class UnitDescriptionCatalog : ScriptableObject
    {
        public UnitDescriptionDefinition[] units;
        public UnitDescriptionDefinition Find(string resourceId)
        {
            if (units == null || string.IsNullOrEmpty(resourceId)) return null;
            foreach (var unit in units)
                if (unit != null && unit.resource != null && unit.resource.name == resourceId) return unit;
            return null;
        }
    }
}
