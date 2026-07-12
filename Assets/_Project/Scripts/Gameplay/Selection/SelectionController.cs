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
        [SerializeField, Min(1f)] private float _dragThreshold = 8f;
        private readonly List<Unit> _selectedUnits = new List<Unit>();
        private readonly List<IEntityPresentation> _selectedPresentations = new List<IEntityPresentation>();
        private ISelectable _selectedEntity;
        private GridManager _grid;
        private EventBus _bus;
        private UnitFactory _unitFactory;
        private DragSelectionView _dragSelectionView;
        private Vector2 _dragStart;
        private bool _primaryPointerDown;
        private bool _isDragging;

        public void Configure(GridManager grid, EventBus bus, UnitFactory unitFactory)
        {
            if (_bus != null) _bus.Unsubscribe<EntityDied>(OnEntityDied);
            _grid = grid;
            _bus = bus;
            _unitFactory = unitFactory;
            _dragSelectionView = DragSelectionView.Ensure(gameObject);
            _bus?.Subscribe<EntityDied>(OnEntityDied);
        }

        private void OnDestroy()
        {
            if (_bus != null) _bus.Unsubscribe<EntityDied>(OnEntityDied);
            _dragSelectionView?.Hide();
        }

        private void Update()
        {
            if (_camera == null || _grid == null) return;
            HandlePrimaryPointer();
            if (!_primaryPointerDown && Input.GetMouseButtonDown(1) &&
                _selectedUnits.Count > 0 && !IsPointerOverUI()) IssueContextCommand();
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
            _selectedPresentations.Clear();
            _bus?.Publish(new SelectionCleared());
        }

        private void HandlePrimaryPointer()
        {
            if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
            {
                _primaryPointerDown = true;
                _isDragging = false;
                _dragStart = Input.mousePosition;
            }

            if (!_primaryPointerDown) return;
            var current = (Vector2)Input.mousePosition;
            if (Input.GetMouseButton(0))
            {
                if (!_isDragging && (current - _dragStart).sqrMagnitude >= _dragThreshold * _dragThreshold)
                    _isDragging = true;
                if (_isDragging) _dragSelectionView?.Show(CreateScreenRect(_dragStart, current));
            }

            if (!Input.GetMouseButtonUp(0)) return;
            if (_isDragging) SelectUnitsInRect(CreateScreenRect(_dragStart, current));
            else SelectAtPointer();
            ResetDragState();
        }

        private void SelectUnitsInRect(Rect screenRect)
        {
            ClearSelection();
            if (_unitFactory == null) return;

            foreach (var unit in _unitFactory.ActiveUnits)
            {
                if (unit == null || unit.IsDead || unit.Faction != Faction.Player) continue;
                var screenPoint = _camera.WorldToScreenPoint(unit.transform.position);
                if (screenPoint.z < 0f || !screenRect.Contains(screenPoint)) continue;
                unit.Select();
                _selectedUnits.Add(unit);
                _selectedPresentations.Add(unit);
            }

            if (_selectedUnits.Count > 0) _bus?.Publish(new UnitSelected(_selectedPresentations));
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
            var selectedEntityDied = ReferenceEquals(_selectedEntity, message.Entity);
            if (selectedEntityDied) _selectedEntity = null;

            var removedUnit = false;
            for (var i = _selectedUnits.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(_selectedUnits[i], message.Entity)) continue;
                _selectedUnits.RemoveAt(i);
                removedUnit = true;
            }

            if (removedUnit)
            {
                _selectedPresentations.Clear();
                foreach (var unit in _selectedUnits)
                    if (unit != null && !unit.IsDead) _selectedPresentations.Add(unit);
                if (_selectedPresentations.Count > 0) _bus?.Publish(new UnitSelected(_selectedPresentations));
                else _bus?.Publish(new SelectionCleared());
            }
            else if (selectedEntityDied) _bus?.Publish(new SelectionCleared());
        }

        private void OnDisable() => ResetDragState();

        private void ResetDragState()
        {
            _primaryPointerDown = false;
            _isDragging = false;
            _dragSelectionView?.Hide();
        }

        private static Rect CreateScreenRect(Vector2 start, Vector2 end) => Rect.MinMaxRect(
            Mathf.Min(start.x, end.x),
            Mathf.Min(start.y, end.y),
            Mathf.Max(start.x, end.x),
            Mathf.Max(start.y, end.y));

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
