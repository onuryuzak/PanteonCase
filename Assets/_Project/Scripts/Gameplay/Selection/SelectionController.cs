using System.Collections.Generic;
using Panteon.Core;
using Panteon.Data;
using Panteon.Gameplay.Buildings;
using Panteon.Gameplay.Grid;
using Panteon.Gameplay.Units;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Panteon.Gameplay.Selection
{
    public sealed class SelectionController : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private LayerMask _selectableMask = ~0;
        private readonly List<Unit> _selectedUnits = new List<Unit>();
        private readonly List<IEntityPresentation> _selectedPresentations = new List<IEntityPresentation>();
        private ISelectable _selectedEntity;
        private GridManager _grid;
        private EventBus _bus;

        public void Configure(GridManager grid, EventBus bus)
        {
            if (_bus != null) _bus.Unsubscribe<EntityDied>(OnEntityDied);
            _grid = grid;
            _bus = bus;
            _bus?.Subscribe<EntityDied>(OnEntityDied);
        }

        private void OnDestroy()
        {
            if (_bus != null) _bus.Unsubscribe<EntityDied>(OnEntityDied);
        }

        private void Update()
        {
            if (_camera == null || _grid == null || IsPointerOverUI()) return;
            if (Input.GetMouseButtonDown(0)) SelectAtPointer();
            if (Input.GetMouseButtonDown(1) && _selectedUnits.Count > 0) IssueContextCommand();
        }

        public void IssueGroupMove(IReadOnlyList<Unit> units, Vector2Int destination)
        {
            if (units == null || units.Count == 0) return;
            var offsets = GetSpiralOffsets(units.Count);
            for (var i = 0; i < units.Count; i++)
            {
                var target = destination + offsets[i];
                if (!_grid.IsWalkable(target)) target = destination;
                units[i].MoveTo(target);
            }
        }

        public void ClearSelection()
        {
            _selectedEntity?.Deselect();
            foreach (var unit in _selectedUnits) if (unit != null) unit.Deselect();
            _selectedEntity = null;
            _selectedUnits.Clear();
            _bus?.Publish(new SelectionCleared());
        }

        private void SelectAtPointer()
        {
            var hit = Physics2D.OverlapPoint(PointerWorld(), _selectableMask);
            ClearSelection();
            if (hit == null) return;
            var unit = hit.GetComponentInParent<Unit>();
            if (unit != null)
            {
                unit.Select();
                _selectedEntity = unit;
                _selectedUnits.Add(unit);
                _selectedPresentations.Clear();
                foreach (var selectedUnit in _selectedUnits) _selectedPresentations.Add(selectedUnit);
                _bus.Publish(new UnitSelected(_selectedPresentations));
                return;
            }
            var building = hit.GetComponentInParent<Building>();
            if (building != null)
            {
                building.Select();
                _selectedEntity = building;
                _bus.Publish(new BuildingSelected(building));
            }
        }

        private void IssueContextCommand()
        {
            var hit = Physics2D.OverlapPoint(PointerWorld(), _selectableMask);
            var damageable = hit != null ? hit.GetComponentInParent<IDamageable>() : null;
            if (damageable != null)
            {
                var issuedAttack = false;
                foreach (var unit in _selectedUnits)
                    issuedAttack |= unit.AttackTarget(damageable);
                if (issuedAttack) return;
            }
            IssueGroupMove(_selectedUnits, _grid.WorldToCell(PointerWorld()));
        }

        private void OnEntityDied(EntityDied message)
        {
            if (ReferenceEquals(_selectedEntity, message.Entity)) _selectedEntity = null;
            for (var i = _selectedUnits.Count - 1; i >= 0; i--)
                if (ReferenceEquals(_selectedUnits[i], message.Entity)) _selectedUnits.RemoveAt(i);
        }

        private Vector3 PointerWorld()
        {
            var world = _camera.ScreenToWorldPoint(Input.mousePosition);
            world.z = 0f;
            return world;
        }

        private static List<Vector2Int> GetSpiralOffsets(int count)
        {
            var result = new List<Vector2Int>(count) { Vector2Int.zero };
            for (var radius = 1; result.Count < count; radius++)
            {
                for (var x = -radius; x <= radius && result.Count < count; x++) result.Add(new Vector2Int(x, -radius));
                for (var y = -radius + 1; y <= radius && result.Count < count; y++) result.Add(new Vector2Int(radius, y));
                for (var x = radius - 1; x >= -radius && result.Count < count; x--) result.Add(new Vector2Int(x, radius));
                for (var y = radius - 1; y > -radius && result.Count < count; y--) result.Add(new Vector2Int(-radius, y));
            }
            return result;
        }

        private static bool IsPointerOverUI()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return true;
            IInputBlocker blocker;
            return ServiceLocator.Instance.TryGet(out blocker) && blocker.IsPointerBlocked(Input.mousePosition);
        }
    }
}
