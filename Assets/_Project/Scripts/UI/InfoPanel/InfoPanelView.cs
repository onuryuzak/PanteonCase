using System;
using System.Collections.Generic;
using Panteon.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Panteon.UI.InfoPanel
{
    public sealed class InfoPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private Image _icon;
        [SerializeField] private Text _nameLabel;
        [SerializeField] private Slider _health;
        [SerializeField] private GameObject _productionRoot;
        [SerializeField] private Transform _productionContent;
        [SerializeField] private Button _productionButtonPrefab;
        private readonly List<Button> _buttons = new List<Button>();

        public void Show(string displayName, Sprite icon, int current, int max)
        {
            if (_panel != null) _panel.SetActive(true);
            if (_nameLabel != null) _nameLabel.text = displayName;
            if (_icon != null) { _icon.sprite = icon; _icon.enabled = icon != null; }
            SetHealth(current, max);
        }

        public void SetHealth(int current, int max)
        {
            if (_health == null) return;
            _health.maxValue = Mathf.Max(1, max);
            _health.value = current;
        }

        public void SetProduction(IReadOnlyList<UnitDefinitionSO> definitions, Action<UnitDefinitionSO> selected)
        {
            var hasProducts = definitions != null && definitions.Count > 0;
            if (_productionRoot != null) _productionRoot.SetActive(hasProducts);
            for (var i = 0; i < _buttons.Count; i++)
            {
                var active = i < (definitions?.Count ?? 0);
                _buttons[i].gameObject.SetActive(active);
                if (!active) _buttons[i].onClick.RemoveAllListeners();
            }
            if (!hasProducts || _productionButtonPrefab == null) return;
            for (var i = 0; i < definitions.Count; i++)
            {
                if (i >= _buttons.Count) _buttons.Add(Instantiate(_productionButtonPrefab, _productionContent));
                var definition = definitions[i];
                var button = _buttons[i];
                button.gameObject.SetActive(true);
                button.onClick.RemoveAllListeners();
                if (selected != null) button.onClick.AddListener(() => selected(definition));
                var image = button.GetComponent<Image>();
                if (image != null && definition.Icon != null) image.sprite = definition.Icon;
            }
        }

        public void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
            SetProduction(null, null);
        }
    }
}
