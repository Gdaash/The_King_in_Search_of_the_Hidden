using System;
using UnityEngine;

namespace InventoryEngine.GUI
{
    /// <summary>
    /// Отображает список ресурсов на GUI панели
    /// </summary>
    public class ListResourcesGUI : 
        MonoBehaviour,
        IListResourcesGUI
    {
        /// <summary>
        /// Префаб отображения картинки ресурса и количества
        /// </summary>
        [SerializeField, Tooltip("Префаб отображения картинки ресурса и количества")]
        protected ResourcesGUI _prefab;

        [SerializeField, Tooltip("Отображать правое значение, обычно требуемое количество")]
        protected bool isRequiredQuantity;
        [SerializeField, Tooltip("Цвет если левое значение больше или равно правому")]
        protected Color normalColor;
        [SerializeField, Tooltip("Цвет если левое значение меньше правого")]
        protected Color errorColor;

        protected IInventoryItems _items;
        protected IInventoryEvent _events;
        protected IRequiredItem _requiredItem;

        protected ResourcesGUI[] _resourcesGUI = new ResourcesGUI[] { };

        protected Action OnStart;
        protected Action OnChanged;
        protected Action OnLoaded;

        public virtual void Init(
            IInventoryItems items,
            IInventoryEvent events,
            IRequiredItem requiredItem)
        {
            _items = items;
            _events = events;
            _requiredItem = requiredItem;
        }

        public void SetUpdate()
        {
            UpdateResourcesLengthGUI();
            UpdateResourcesGUI();
        }
        protected virtual void UpdateResourcesGUI()
        {
            for (int q = 0; q < _items.Count; q++)
            {
                if (isRequiredQuantity)
                {
                    int quantity = _items.GetQuantity(q);
                    int requiredQuantity = _requiredItem.GetRequiredQuantity(_items[q]);

                    _resourcesGUI[q].UpdateCount(
                    _items[q],
                    requiredQuantity.ToString(),
                    quantity.ToString(),
                    quantity >= requiredQuantity ? normalColor : errorColor);
                }
                else
                {
                    _resourcesGUI[q].UpdateCount(
                    _items[q],
                    _items.GetQuantity(q).ToString(),
                    "",
                    null);
                }
            }
        }
        protected virtual void UpdateResourcesLengthGUI()
        {
            if (_resourcesGUI.Length < _items.Count)
            {
                ResourcesGUI[] old = _resourcesGUI;

                _resourcesGUI = new ResourcesGUI[_items.Count];
                for (int q = 0; q < _resourcesGUI.Length; q++)
                {
                    if (q < old.Length)
                    {
                        _resourcesGUI[q] = old[q];
                    }
                    else
                    {
                        _resourcesGUI[q] = CreateResourcesGUI();
                    }

                    if(!_resourcesGUI[q].gameObject.activeSelf)
                        _resourcesGUI[q].gameObject.SetActive(true);
                }
            }
            else
            {
                for (int q = 0; q < _resourcesGUI.Length; q++)
                {
                    _resourcesGUI[q].gameObject.SetActive(q < _items.Count);
                }
            }
        }

        protected virtual void OnEnable()
        {
            if (_events is null) return;

            _events.OnStart += SetUpdate;
            _events.OnChanged += SetUpdate;
            _events.OnLoaded += SetUpdate;

            SetUpdate();
        }
        protected virtual void OnDisable()
        {
            if (_events is null) return;

            _events.OnStart -= SetUpdate;
            _events.OnChanged -= SetUpdate;
            _events.OnLoaded -= SetUpdate;
        }

        protected virtual ResourcesGUI CreateResourcesGUI()
        {
            GameObject game = Instantiate(_prefab.gameObject, transform);
            game.name = "Slot";
            return game.GetComponent<ResourcesGUI>();
        }
    }
}
