using System;
using UnityEngine;
using UnityEngine.UI;

namespace Panteon.UI
{
    public sealed class SelectedUnitRowView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private Text _name;

        public RectTransform Root => (RectTransform)transform;
        public Image Icon => _icon;
        public Text Name => _name;

        public void ValidateReferences()
        {
            if (_icon == null || _name == null)
                throw new InvalidOperationException($"{name} has incomplete selected-unit row references.");
        }
    }
}
