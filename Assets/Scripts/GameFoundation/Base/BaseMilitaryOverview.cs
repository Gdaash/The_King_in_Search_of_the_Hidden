using System;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    public sealed class BaseMilitaryOverview : MonoBehaviour
    {
        [Serializable]
        private sealed class MilitaryCount
        {
            public ResourceType resource;
            public Image icon;
            public Text amount;
        }

        [SerializeField] private MilitaryCount swordsmen = new MilitaryCount();
        [SerializeField] private MilitaryCount archers = new MilitaryCount();

        private void OnEnable()
        {
            GlobalResourceManager.OnResourceChanged += OnResourceChanged;
            Refresh();
        }

        private void OnDisable() => GlobalResourceManager.OnResourceChanged -= OnResourceChanged;

        private void OnResourceChanged(ResourceType _, int __) => Refresh();

        private void Refresh()
        {
            Refresh(swordsmen);
            Refresh(archers);
        }

        private static void Refresh(MilitaryCount entry)
        {
            if (entry == null) return;
            if (entry.icon != null && entry.resource != null)
                ResourceIconSizing.Apply(entry.icon, entry.resource.resourceIcon);
            if (entry.amount != null)
                entry.amount.text = GlobalResourceManager.Instance != null && entry.resource != null
                    ? GlobalResourceManager.Instance.GetResourceAmount(entry.resource).ToString()
                    : "0";
        }
    }
}
