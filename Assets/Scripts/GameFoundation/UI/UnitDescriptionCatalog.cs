using UnityEngine;

namespace GameFoundation.UI
{
    [CreateAssetMenu(menuName = "Game Foundation/UI/Unit description catalog")]
    public sealed class UnitDescriptionCatalog : ScriptableObject
    {
        public UnitDescriptionDefinition[] units;
        public UnitDescriptionDefinition Find(GameObject prefab)
        {
            if (units == null || prefab == null) return null;
            foreach (var unit in units)
                if (unit != null && unit.unitPrefab == prefab) return unit;
            return null;
        }
        public UnitDescriptionDefinition FindEnemyId(string enemyId)
        {
            if (units == null || string.IsNullOrEmpty(enemyId)) return null;
            foreach (var unit in units)
                if (unit != null && unit.unitPrefab != null && unit.unitPrefab.TryGetComponent<GameFoundation.Bestiary.EnemyIdentity>(out var identity) && identity.Id == enemyId) return unit;
            return null;
        }
        public UnitDescriptionDefinition Find(string resourceId)
        {
            if (units == null || string.IsNullOrEmpty(resourceId)) return null;
            foreach (var unit in units)
                if (unit != null && unit.resource != null && unit.resource.Id == resourceId) return unit;
            return null;
        }
        public UnitDescriptionDefinition Find(ResourceType resource)
        {
            if (units == null || resource == null) return null;
            foreach (var unit in units)
                if (unit != null && unit.resource == resource) return unit;
            return null;
        }
    }
}
