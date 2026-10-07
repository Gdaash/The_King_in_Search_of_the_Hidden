using UnityEngine;
namespace GameFoundation.Quests
{
    /// <summary>Keep this observer on an active parent, outside the objects it hides.</summary>
    public sealed class ContentUnlockGate : MonoBehaviour
    {
        public ContentUnlockDefinition requiredUnlock;
        [Tooltip("Объекты, которые появятся после награды. Компонент разместить на их активном родителе.")]
        public GameObject[] targets;
        private void OnEnable() { ContentUnlocks.Changed += Refresh; Refresh(); }
        private void OnDisable() => ContentUnlocks.Changed -= Refresh;
        public void Refresh()
        {
            bool visible = ContentUnlocks.IsUnlocked(requiredUnlock);
            if (targets == null) return;
            foreach (var target in targets)
                if (target != null && target != gameObject && !transform.IsChildOf(target.transform)) target.SetActive(visible);
        }
    }
}
