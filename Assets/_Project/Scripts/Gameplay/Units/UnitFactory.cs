using System;
using System.Collections.Generic;
using Panteon.Core;
using Panteon.Data;
using Panteon.Gameplay.Grid;
using Panteon.Gameplay.Pathfinding;
using UnityEngine;

namespace Panteon.Gameplay.Units
{
    public sealed class UnitFactory
    {
        private readonly PoolManager _pool;
        private readonly GridManager _grid;
        private readonly IPathfinder _pathfinder;
        private readonly EventBus _bus;
        private readonly Transform _root;
        private readonly Dictionary<Unit, GameObject> _prefabs = new Dictionary<Unit, GameObject>();

        public UnitFactory(PoolManager pool, GridManager grid, IPathfinder pathfinder, EventBus bus, Transform root)
        { _pool = pool; _grid = grid; _pathfinder = pathfinder; _bus = bus; _root = root; }

        public Unit Create(UnitDefinitionSO definition, Vector2Int cell, Faction faction = Faction.Player)
        {
            if (definition == null || definition.Prefab == null) throw new InvalidOperationException("Unit definition and prefab are required.");
            var spawn = FindSpawnCell(cell);
            if (!spawn.HasValue) return null;
            var instance = _pool.Get(definition.Prefab, _root);
            instance.transform.SetParent(_root, false);
            instance.transform.position = _grid.CellToWorld(spawn.Value);
            var unit = instance.GetComponent<Unit>();
            if (unit == null) throw new InvalidOperationException("Unit prefab needs a Unit component.");
            _prefabs[unit] = definition.Prefab;
            unit.Initialize(definition, spawn.Value, _grid, _pathfinder, _bus, Return, faction);
            _bus.Publish(new UnitSpawned(unit));
            return unit;
        }

        private Vector2Int? FindSpawnCell(Vector2Int preferred)
        {
            if (IsUnitCellFree(preferred)) return preferred;

            // Breadth-first search keeps every additional unit as close as possible
            // to its barracks' designated spawn cell without stacking units.
            var frontier = new Queue<Vector2Int>();
            var visited = new HashSet<Vector2Int> { preferred };
            frontier.Enqueue(preferred);
            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                foreach (var candidate in _grid.GetNeighbors(current))
                {
                    if (!visited.Add(candidate)) continue;
                    if (!_grid.IsWalkable(candidate)) continue;
                    if (IsUnitCellFree(candidate)) return candidate;
                    frontier.Enqueue(candidate);
                }
            }
            return null;
        }

        private bool IsUnitCellFree(Vector2Int cell)
        {
            if (!_grid.IsWalkable(cell)) return false;
            foreach (var unit in _prefabs.Keys)
            {
                if (unit == null || unit.IsDead) continue;
                if (unit.GridPosition == cell) return false;
            }
            return true;
        }

        private void Return(Unit unit)
        {
            GameObject prefab;
            if (_prefabs.TryGetValue(unit, out prefab))
            { _prefabs.Remove(unit); _pool.Return(unit.gameObject, prefab); }
        }
    }
}
