using System.Collections;
using System.Collections.Generic;
using Panteon.Core;
using Panteon.Data;
using Panteon.Gameplay.Buildings;
using Panteon.Gameplay.Feedback;
using Panteon.Gameplay.Grid;
using Panteon.Gameplay.Pathfinding;
using Panteon.Gameplay.Selection;
using Panteon.Gameplay.Units;
using UnityEngine;

namespace Panteon.Gameplay
{
    [DefaultExecutionOrder(-1000)]
    public sealed class Bootstrap : MonoBehaviour
    {
        [SerializeField] private PoolManager _poolManager;
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private BuildingPlacementController _placementController;
        [SerializeField] private SelectionController _selectionController;
        [SerializeField] private BuildingCatalogSO _catalog;
        [SerializeField] private Transform _buildingsRoot;
        [SerializeField] private Transform _unitsRoot;
        [SerializeField, Min(0)] private int _buildingPrewarmCount = 1;
        [SerializeField, Min(0)] private int _unitPrewarmCount = 4;
        [SerializeField, Min(1)] private int _prewarmPerFrame = 4;
        private ProductionService _productionService;

        private void Awake()
        {
            if (_poolManager == null) _poolManager = GetComponentInChildren<PoolManager>(true);
            if (_gridManager == null) _gridManager = FindObjectOfType<GridManager>();
            if (_placementController == null) _placementController = FindObjectOfType<BuildingPlacementController>();
            if (_selectionController == null) _selectionController = FindObjectOfType<SelectionController>();
            if (_buildingsRoot == null) _buildingsRoot = GameObject.Find("BuildingsRoot")?.transform;
            if (_unitsRoot == null) _unitsRoot = GameObject.Find("UnitsRoot")?.transform;
            var locator = ServiceLocator.Instance;
            locator.Clear();
            var bus = new EventBus();
            locator.Register(bus);
            if (_poolManager != null) locator.Register(_poolManager);
            if (_gridManager == null || _poolManager == null) return;
            locator.Register(_gridManager);
            GameplayFeedbackController.Ensure(gameObject).Configure(bus, _gridManager.CellSize);
            IPathfinder pathfinder = new AStarPathfinder();
            locator.Register(pathfinder);
            var buildingFactory = new BuildingFactory(_poolManager, _gridManager, bus, _buildingsRoot);
            var unitFactory = new UnitFactory(_poolManager, _gridManager, pathfinder, bus, _unitsRoot);
            locator.Register(buildingFactory);
            locator.Register(unitFactory);
            _productionService = new ProductionService(unitFactory, bus);
            locator.Register(_productionService);
            if (_placementController != null) locator.Register<IBuildingPlacementService>(_placementController);
            if (_selectionController != null) locator.Register(_selectionController);
            _placementController?.Configure(_gridManager, buildingFactory, bus);
            _selectionController?.Configure(_gridManager, bus, unitFactory);
            FindObjectOfType<CameraFitToAspect>()?.Configure(_gridManager, bus);
            StartCoroutine(PrewarmCatalogPrefabs());
        }

        private IEnumerator PrewarmCatalogPrefabs()
        {
            if (_catalog == null || _poolManager == null) yield break;

            var prewarmed = new HashSet<GameObject>();
            foreach (var building in _catalog.Buildings)
            {
                if (building == null || building.Prefab == null || !prewarmed.Add(building.Prefab)) continue;
                yield return _poolManager.PrewarmAsync(building.Prefab, _buildingPrewarmCount, _prewarmPerFrame, _buildingsRoot);

                if (!building.CanProduce || building.Producibles == null) continue;
                foreach (var unit in building.Producibles)
                {
                    if (unit == null || unit.Prefab == null || !prewarmed.Add(unit.Prefab)) continue;
                    yield return _poolManager.PrewarmAsync(unit.Prefab, _unitPrewarmCount, _prewarmPerFrame, _unitsRoot);
                }
            }
        }

        private void OnDestroy()
        {
            _productionService?.Dispose();
            ServiceLocator.Instance.Clear();
        }
    }
}
