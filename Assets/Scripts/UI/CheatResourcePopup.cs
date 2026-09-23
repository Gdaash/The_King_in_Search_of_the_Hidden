using System;
using System.Collections.Generic;
using GameFoundation.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class CheatResourcePopup : MonoBehaviour
{
    [Serializable]
    public sealed class ResourceRow
    {
        public ResourceType resource;
        public Image icon;
        public Text nameText;
        public Text amountText;
        public Button subtractTen;
        public Button subtractFive;
        public Button subtractOne;
        public Button addOne;
        public Button addFive;
        public Button addTen;
    }

    [SerializeField] private GameObject window;
    [SerializeField] private Button closeButton;
    [SerializeField] private List<ResourceRow> rows = new List<ResourceRow>();

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        foreach (ResourceRow row in rows)
        {
            if (row == null || row.resource == null) continue;
            Bind(row.subtractTen, row.resource, -10);
            Bind(row.subtractFive, row.resource, -5);
            Bind(row.subtractOne, row.resource, -1);
            Bind(row.addOne, row.resource, 1);
            Bind(row.addFive, row.resource, 5);
            Bind(row.addTen, row.resource, 10);

            if (row.icon != null)
            {
                ResourceIconSizing.Apply(row.icon, row.resource.resourceIcon);
                row.icon.enabled = row.resource.resourceIcon != null;
            }
            if (row.nameText != null)
                row.nameText.text = row.resource.resourceName;
        }

        if (window != null)
            window.SetActive(false);
    }

    private void OnEnable()
    {
        GlobalResourceManager.OnResourceChanged += OnResourceChanged;
    }

    private void OnDisable()
    {
        GlobalResourceManager.OnResourceChanged -= OnResourceChanged;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.L) && !IsEditingText())
        {
            if (window != null) window.SetActive(!window.activeSelf);
            Refresh();
        }
        else if (window != null && window.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }

        // MainMenu has no resource manager until a save slot has been opened.
        if (window != null && window.activeSelf && _lastManager != GlobalResourceManager.Instance)
            Refresh();
    }

    private GlobalResourceManager _lastManager;

    private static bool IsEditingText()
    {
        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        return selected != null &&
               (selected.GetComponent<InputField>() != null || selected.GetComponent<TMP_InputField>() != null);
    }

    private void Bind(Button button, ResourceType resource, int delta)
    {
        if (button != null)
            button.onClick.AddListener(() => Change(resource, delta));
    }

    private void Change(ResourceType resource, int delta)
    {
        GlobalResourceManager manager = GlobalResourceManager.Instance;
        if (manager == null || resource == null) return;

        int current = manager.GetResourceAmount(resource);
        if (delta < 0 && current < -delta) return;

        int next = (int)Math.Min(int.MaxValue, (long)current + delta);
        manager.SetResourceAmount(resource, next);
        Refresh();
    }

    private void OnResourceChanged(ResourceType _, int __)
    {
        if (window != null && window.activeSelf)
            Refresh();
    }

    private void Refresh()
    {
        _lastManager = GlobalResourceManager.Instance;
        foreach (ResourceRow row in rows)
        {
            if (row == null) continue;
            bool available = _lastManager != null && row.resource != null;
            int amount = available ? _lastManager.GetResourceAmount(row.resource) : 0;
            if (row.amountText != null)
                row.amountText.text = available ? amount.ToString() : "—";
            if (row.nameText != null && row.resource != null)
            {
                string key = "resource." + row.resource.name + ".name";
                string translated = LocalizationService.Instance != null ? LocalizationService.Instance.Get(key) : key;
                row.nameText.text = translated != key ? translated : row.resource.resourceName;
            }

            SetInteractable(row.subtractTen, available && amount >= 10);
            SetInteractable(row.subtractFive, available && amount >= 5);
            SetInteractable(row.subtractOne, available && amount >= 1);
            SetInteractable(row.addOne, available && amount < int.MaxValue);
            SetInteractable(row.addFive, available && amount <= int.MaxValue - 5);
            SetInteractable(row.addTen, available && amount <= int.MaxValue - 10);
        }
    }

    private static void SetInteractable(Button button, bool value)
    {
        if (button != null) button.interactable = value;
    }

    private void Close()
    {
        if (window != null) window.SetActive(false);
    }
}
