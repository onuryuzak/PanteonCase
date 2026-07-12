using Panteon.Core;
using Panteon.Data;
using UnityEngine;

namespace Panteon.UI.InfoPanel
{
    public sealed class InfoPanelController : MonoBehaviour
    {
        [SerializeField] private InfoPanelView _view;
        private EventBus _bus;
        private IDamageable _selected;
        private IProductionBuilding _selectedBuilding;

        private void Start()
        {
            EventBus bus;
            if (ServiceLocator.Instance.TryGet(out bus)) Configure(bus);
        }

        public void Configure(EventBus bus)
        {
            if (_bus != null) Unsubscribe();
            _bus = bus;
            _bus.Subscribe<BuildingSelected>(OnBuildingSelected);
            _bus.Subscribe<UnitSelected>(OnUnitSelected);
            _bus.Subscribe<SelectionCleared>(OnSelectionCleared);
            _bus.Subscribe<EntityHealthChanged>(OnHealthChanged);
        }

        private void OnDestroy() => Unsubscribe();

        private void OnBuildingSelected(BuildingSelected message)
        {
            _selected = _selectedBuilding = message.Building;
            _view.Show(message.Building.DisplayName, message.Building.Icon, message.Building.CurrentHP, message.Building.MaxHP);
            _view.SetProduction(message.Building.CanProduce ? message.Building.Producibles : null, RequestProduction);
        }

        private void OnUnitSelected(UnitSelected message)
        {
            if (message.Units == null || message.Units.Count == 0) return;
            var unit = message.Units[0];
            _selected = unit;
            _selectedBuilding = null;
            _view.Show(unit.DisplayName, unit.Icon, unit.CurrentHP, unit.MaxHP);
            _view.SetProduction(null, null);
        }

        private void OnSelectionCleared(SelectionCleared _) { _selected = null; _selectedBuilding = null; _view.Hide(); }
        private void OnHealthChanged(EntityHealthChanged message)
        { if (ReferenceEquals(message.Entity, _selected)) _view.SetHealth(message.Current, message.Max); }
        private void RequestProduction(UnitDefinitionSO definition)
        { if (_selectedBuilding != null) _bus.Publish(new ProductionRequested(_selectedBuilding, definition)); }

        private void Unsubscribe()
        {
            if (_bus == null) return;
            _bus.Unsubscribe<BuildingSelected>(OnBuildingSelected);
            _bus.Unsubscribe<UnitSelected>(OnUnitSelected);
            _bus.Unsubscribe<SelectionCleared>(OnSelectionCleared);
            _bus.Unsubscribe<EntityHealthChanged>(OnHealthChanged);
            _bus = null;
        }
    }
}
