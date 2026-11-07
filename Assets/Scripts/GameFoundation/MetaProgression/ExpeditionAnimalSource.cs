using UnityEngine;

namespace GameFoundation.MetaProgression
{
    public sealed class ExpeditionAnimalSource : MonoBehaviour
    {
        public enum Kind { Chicken, Boar }
        [SerializeField] private Kind animal;
        public Kind Animal => animal;
    }
}
