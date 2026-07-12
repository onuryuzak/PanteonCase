using System;
using System.Collections.Generic;
using Panteon.Core;
using Panteon.Data;
using Panteon.Gameplay.Grid;
using UnityEngine;

namespace Panteon.Gameplay.Buildings
{
    public sealed class BuildingFactory
    {
        private readonly PoolManager _pool;
        private readonly GridManager _grid;
        private readonly EventBus _bus;
        private readonly Transform _root;
        private readonly Dictionary<Building, GameObject> _prefabs = new Dictionary<Building, GameObject>();

        public BuildingFactory(PoolManager pool, GridManager grid, EventBus bus, Transform root)
        {
            _pool = pool;
            _grid = grid;
            _bus = bus;
            _root = root;
        }

        public Building Create(BuildingDefinitionSO definition, Vector2Int cell)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (definition.Prefab == null) throw new InvalidOperationException($"{definition.name} has no prefab.");
            if (!CanPlace(definition, cell))
            {
                _bus.Publish(new BuildingPlacementFailed(cell));
                return null;
            }

            var instance = _pool.Get(definition.Prefab, _root);
            instance.transform.SetParent(_root, false);
            instance.transform.position = _grid.AreaToWorldCenter(cell, definition.FootprintSize);
            var building = instance.GetComponent<Building>();
            if (building == null) throw new InvalidOperationException("Building prefab needs a Building component.");
            _prefabs[building] = definition.Prefab;
            building.Initialize(definition, cell, _grid, _bus, Return);
            instance.GetComponent<BuildingView>()?.Render(definition);
            building.OccupyGrid();
            _bus.Publish(new BuildingPlaced(building));
            return building;
        }

        public bool CanPlace(BuildingDefinitionSO definition, Vector2Int cell)
        {
            return definition != null && _grid.IsAreaValid(cell, definition.FootprintSize);
        }

        private void Return(Building building)
        {
            GameObject prefab;
            if (_prefabs.TryGetValue(building, out prefab))
            {
                _prefabs.Remove(building);
                _pool.Return(building.gameObject, prefab);
            }
        }
    }
}
