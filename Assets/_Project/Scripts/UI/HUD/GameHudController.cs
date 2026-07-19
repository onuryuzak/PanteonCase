using System.Collections.Generic;
using Panteon.Core;
using Panteon.Data;
using UnityEngine;

namespace Panteon.UI
{
    /// <summary>
    /// Connects HUD views to gameplay events.
    /// </summary>
    public sealed class GameHudController : MonoBehaviour, IInputBlocker
    {
        [SerializeField] private BuildingCatalogSO _catalog;
        [SerializeField] private RuntimeHudView _hud;
        [SerializeField] private Camera _boardCamera;
        [SerializeField] private Vector2Int _gridSize = new Vector2Int(24, 16);
        [SerializeField] private Vector2 _gridOrigin = new Vector2(-12f, -8f);
        [SerializeField, Min(0.1f)] private float _cellSize = 1f;
        [SerializeField, Min(0f)] private float _boardPaddingPercent = 0.02f;

        private readonly List<BuildingDefinitionSO> _buildings = new List<BuildingDefinitionSO>();
        private readonly List<IEntityPresentation> _selectedUnits = new List<IEntityPresentation>();
        private EventBus _eventBus;
        private IBuildingPlacementService _placementService;
        private IDamageable _selected;
        private IProductionBuilding _selectedBuilding;
        private HudViewFactory _viewFactory;
        private ProductionMenuView _productionView;
        private InformationPanelView _informationView;
        private BoardViewportController _boardViewport;
        private Rect _boardRect;
        private string _status = "Choose a building, then click a valid grid cell.";

        private void Start()
        {
            ResolveDependencies();
            BuildViews();
            if (_hud == null) return;
            SubscribeToEvents();
            RefreshStatus();
            RefreshSelection();
        }

        private void Update()
        {
            if (_hud != null) _boardRect = _hud.GetBoardScreenRect();
            _boardViewport?.Apply(_boardRect);
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            if (_productionView != null) _productionView.BuildingRequested -= HandleBuildingRequested;
            _productionView?.Dispose();
            _boardViewport?.Dispose();
            _viewFactory?.Dispose();
        }

        public bool IsPointerBlocked(Vector2 screenPosition)
        {
            if (_boardRect.width <= 0f || _boardRect.height <= 0f) return false;
            var guiPosition = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
            return !_boardRect.Contains(guiPosition);
        }

        private void ResolveDependencies()
        {
            ServiceLocator.Instance.Register<IInputBlocker>(this);
            ServiceLocator.Instance.TryGet(out _eventBus);
            ServiceLocator.Instance.TryGet(out _placementService);
            if (_boardCamera == null) _boardCamera = Camera.main;

            _buildings.Clear();
            if (_catalog == null) return;
            foreach (var building in _catalog.Buildings)
                if (building != null) _buildings.Add(building);
        }

        private void BuildViews()
        {
            if (_hud == null) _hud = GetComponentInChildren<RuntimeHudView>(true);
            if (_hud == null)
            {
                Debug.LogError("GameHudController requires a RuntimeHUD view under Main.", this);
                enabled = false;
                return;
            }

            _viewFactory = new HudViewFactory();
            _viewFactory.SetScale(2f);
            _productionView = new ProductionMenuView(_hud, _viewFactory);
            _informationView = new InformationPanelView(_hud, _viewFactory);
            _boardViewport = new BoardViewportController(_boardCamera, _gridSize, _gridOrigin, _cellSize, _boardPaddingPercent);

            _productionView.BuildingRequested += HandleBuildingRequested;
            _productionView.SetBuildings(_buildings);
            _productionView.RefreshContentLayout();
            _boardRect = _hud.GetBoardScreenRect();
        }

        private void SubscribeToEvents()
        {
            if (_eventBus == null) return;
            _eventBus.Subscribe<BuildingSelected>(HandleBuildingSelected);
            _eventBus.Subscribe<UnitSelected>(HandleUnitSelected);
            _eventBus.Subscribe<SelectionCleared>(HandleSelectionCleared);
            _eventBus.Subscribe<BuildingPlacementFailed>(HandlePlacementFailed);
            _eventBus.Subscribe<EntityHealthChanged>(HandleHealthChanged);
        }

        private void UnsubscribeFromEvents()
        {
            if (_eventBus == null) return;
            _eventBus.Unsubscribe<BuildingSelected>(HandleBuildingSelected);
            _eventBus.Unsubscribe<UnitSelected>(HandleUnitSelected);
            _eventBus.Unsubscribe<SelectionCleared>(HandleSelectionCleared);
            _eventBus.Unsubscribe<BuildingPlacementFailed>(HandlePlacementFailed);
            _eventBus.Unsubscribe<EntityHealthChanged>(HandleHealthChanged);
        }

        private void HandleBuildingRequested(BuildingDefinitionSO definition) =>
            _placementService?.EnterPlacementMode(definition);

        private void HandleBuildingSelected(BuildingSelected message)
        {
            _selectedUnits.Clear();
            _selected = _selectedBuilding = message.Building;
            _status = $"Selected {message.Building.DisplayName}";
            RefreshStatus();
            RefreshSelection();
        }

        private void HandleUnitSelected(UnitSelected message)
        {
            _selectedUnits.Clear();
            if (message.Units != null)
                foreach (var unit in message.Units)
                    if (unit != null && !unit.IsDead) _selectedUnits.Add(unit);

            _selected = _selectedUnits.Count > 0 ? _selectedUnits[0] : null;
            _selectedBuilding = null;
            if (_selectedUnits.Count > 1) _status = $"Selected {_selectedUnits.Count} units";
            else if (_selected is IEntityPresentation presentation) _status = $"Selected {presentation.DisplayName}";
            RefreshStatus();
            RefreshSelection();
        }

        private void HandleSelectionCleared(SelectionCleared _)
        {
            _selectedUnits.Clear();
            _selected = null;
            _selectedBuilding = null;
            RefreshSelection();
        }

        private void HandlePlacementFailed(BuildingPlacementFailed message)
        {
            _status = $"Invalid placement at {message.Cell}.";
            RefreshStatus();
        }

        private void HandleHealthChanged(EntityHealthChanged message)
        {
            if (ReferenceEquals(message.Entity, _selected)) RefreshSelection();
        }

        private void RequestProduction(IProductionBuilding building, UnitDefinitionSO unit) =>
            _eventBus?.Publish(new ProductionRequested(building, unit));

        private void RefreshSelection()
        {
            if (_selectedUnits.Count > 1) _informationView?.ShowUnits(_selectedUnits);
            else _informationView?.Show(_selected, _selectedBuilding, RequestProduction);
        }

        private void RefreshStatus() => _productionView?.SetStatus(_status);

    }
}
