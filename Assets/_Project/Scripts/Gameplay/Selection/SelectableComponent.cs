using Panteon.Core;
using UnityEngine;

namespace Panteon.Gameplay.Selection
{
    public sealed class SelectableComponent : MonoBehaviour, ISelectable
    {
        [SerializeField] private GameObject _selectionVisual;
        public bool IsSelected { get; private set; }

        private void Awake()
        {
            if (_selectionVisual != null) _selectionVisual.SetActive(false);
        }

        public void Select()
        {
            IsSelected = true;
            if (_selectionVisual != null) _selectionVisual.SetActive(true);
        }

        public void Deselect()
        {
            IsSelected = false;
            if (_selectionVisual != null) _selectionVisual.SetActive(false);
        }
    }
}
