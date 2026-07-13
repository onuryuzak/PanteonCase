using Panteon.Core;
using Panteon.Data;
using Panteon.Gameplay.Grid;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Panteon.Gameplay.Buildings
{
    public sealed class BuildingPlacementController : MonoBehaviour, IBuildingPlacementService
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private SpriteRenderer _ghost;
        private GridManager _grid;
        private BuildingFactory _factory;
        private EventBus _bus;
        private BuildingDefinitionSO _activeDefinition;
        private Vector2Int _hoveredCell;
        private bool _valid;

        public bool IsPlacing => _activeDefinition != null;

        public void Configure(GridManager grid, BuildingFactory factory, EventBus bus)
        { _grid = grid; _factory = factory; _bus = bus; }

        public void EnterPlacementMode(BuildingDefinitionSO definition)
        {
            if (definition == null) return;
            _activeDefinition = definition;
            if (_ghost != null)
            {
                _ghost.sprite = definition.Icon;
                ApplyGhostLayout(definition);
                _ghost.gameObject.SetActive(true);
            }
        }

        public void CancelPlacement()
        {
            _activeDefinition = null;
            if (_ghost != null) _ghost.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!IsPlacing || _grid == null || _camera == null) return;
            var world = _camera.ScreenToWorldPoint(Input.mousePosition);
            _hoveredCell = _grid.WorldToCell(world);
            _valid = _factory != null && _factory.CanPlace(_activeDefinition, _hoveredCell);
            if (_ghost != null)
            {
                ApplyGhostLayout(_activeDefinition);
                _ghost.color = _valid ? new Color(0.2f, 1f, 0.35f, 0.65f) : new Color(1f, 0.15f, 0.15f, 0.75f);
            }

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)) CancelPlacement();
            else if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
            {
                if (_valid)
                {
                    _factory.Create(_activeDefinition, _hoveredCell);
                    CancelPlacement();
                }
                else _bus.Publish(new BuildingPlacementFailed(_hoveredCell));
            }
        }

        private static bool IsPointerOverUI()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return true;
            IInputBlocker blocker;
            return ServiceLocator.Instance.TryGet(out blocker) && blocker.IsPointerBlocked(Input.mousePosition);
        }

        private void ApplyGhostLayout(BuildingDefinitionSO definition)
        {
            if (_ghost == null || definition == null) return;
            float scale;
            Vector3 offset;
            if (!BuildingVisualScaleUtility.TryGetWorldLayout(definition, out scale, out offset)) return;
            _ghost.transform.localScale = new Vector3(scale, scale, 1f);
            if (_grid != null)
                _ghost.transform.position = _grid.AreaToWorldCenter(_hoveredCell, definition.FootprintSize) + offset;
        }
    }
}
