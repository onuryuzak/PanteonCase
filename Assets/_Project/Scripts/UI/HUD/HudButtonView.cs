using System;
using UnityEngine;
using UnityEngine.UI;

namespace Panteon.UI
{
    public sealed class HudButtonView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private Text _label;

        public RectTransform Root => (RectTransform)transform;
        public Button Button => _button;
        public Image Icon => _icon;
        public Text Label => _label;

        public void ValidateReferences()
        {
            if (_button == null || _icon == null || _label == null)
                throw new InvalidOperationException($"{name} has incomplete HUD button references.");
        }
    }
}
