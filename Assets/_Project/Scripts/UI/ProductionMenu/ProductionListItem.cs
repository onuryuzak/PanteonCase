using System;
using Panteon.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Panteon.UI.ProductionMenu
{
    public sealed class ProductionListItem : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private Text _label;
        [SerializeField] private Button _button;
        public RectTransform RectTransform => (RectTransform)transform;
        private BuildingDefinitionSO _definition;
        private Action<BuildingDefinitionSO> _selected;

        private void Awake()
        {
            if (_button != null) _button.onClick.AddListener(HandleClick);
        }

        public void Bind(BuildingDefinitionSO definition, Action<BuildingDefinitionSO> selected)
        {
            _definition = definition;
            _selected = selected;
            if (_icon != null) { _icon.sprite = definition != null ? definition.Icon : null; _icon.enabled = definition != null && definition.Icon != null; }
            if (_label != null) _label.text = definition != null ? definition.DisplayName : string.Empty;
        }

        public void Clear() => Bind(null, null);

        private void HandleClick()
        {
            if (_definition != null) _selected?.Invoke(_definition);
        }
    }
}
