using System;
using System.Collections.Generic;
using Panteon.Gameplay.Buildings;
using UnityEngine;

namespace Panteon.Gameplay.Grid
{
    public sealed class GridManager : MonoBehaviour
    {
        [SerializeField] private Vector2Int _size = new Vector2Int(24, 16);
        [SerializeField, Min(0.1f)] private float _cellSize = 1f;
        [SerializeField] private Vector2 _origin = new Vector2(-12f, -8f);

        private readonly Dictionary<Vector2Int, GridCell> _cells = new Dictionary<Vector2Int, GridCell>();
        public int CellCount => _cells.Count;
        public Vector2Int Size => _size;
        public float CellSize => _cellSize;
        public int Revision { get; private set; }

        private void Awake() => Initialize(_size, _cellSize, _origin);

        public void Initialize(Vector2Int size, float cellSize = 1f, Vector2? origin = null)
        {
            if (size.x <= 0 || size.y <= 0) throw new ArgumentOutOfRangeException(nameof(size));
            _size = size;
            _cellSize = Mathf.Max(0.1f, cellSize);
            _origin = origin ?? Vector2.zero;
            _cells.Clear();
            for (var x = 0; x < _size.x; x++)
            for (var y = 0; y < _size.y; y++)
                _cells.Add(new Vector2Int(x, y), new GridCell(new Vector2Int(x, y)));
            Revision++;
        }

        public bool Contains(Vector2Int cell) => _cells.ContainsKey(cell);

        public bool IsWalkable(Vector2Int cell)
        {
            GridCell value;
            return _cells.TryGetValue(cell, out value) && value.IsWalkable;
        }

        public bool IsAreaValid(Vector2Int origin, Vector2Int footprint)
        {
            if (footprint.x <= 0 || footprint.y <= 0) return false;
            for (var x = 0; x < footprint.x; x++)
            for (var y = 0; y < footprint.y; y++)
                if (!IsWalkable(origin + new Vector2Int(x, y))) return false;
            return true;
        }

        public void Occupy(Vector2Int origin, Vector2Int footprint, Building building)
        {
            if (building == null) throw new ArgumentNullException(nameof(building));
            if (!IsAreaValid(origin, footprint)) throw new InvalidOperationException("Cannot occupy an invalid grid area.");
            ForEach(origin, footprint, cell => cell.Occupant = building);
            Revision++;
        }

        public void Free(Vector2Int origin, Vector2Int footprint, Building expectedOccupant = null)
        {
            ForEach(origin, footprint, cell =>
            {
                if (expectedOccupant == null || cell.Occupant == expectedOccupant) cell.Occupant = null;
            });
            Revision++;
        }

        public IEnumerable<Vector2Int> GetNeighbors(Vector2Int cell)
        {
            for (var x = -1; x <= 1; x++)
            for (var y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;
                var next = cell + new Vector2Int(x, y);
                if (!Contains(next)) continue;
                if (x != 0 && y != 0 &&
                    (!IsWalkable(cell + new Vector2Int(x, 0)) || !IsWalkable(cell + new Vector2Int(0, y))))
                    continue;
                yield return next;
            }
        }

        public Vector3 CellToWorld(Vector2Int cell) =>
            new Vector3(_origin.x + (cell.x + 0.5f) * _cellSize, _origin.y + (cell.y + 0.5f) * _cellSize, 0f);

        public Vector3 AreaToWorldCenter(Vector2Int origin, Vector2Int footprint) =>
            new Vector3(
                _origin.x + (origin.x + footprint.x * 0.5f) * _cellSize,
                _origin.y + (origin.y + footprint.y * 0.5f) * _cellSize,
                0f);

        public Vector2Int WorldToCell(Vector3 world) => new Vector2Int(
            Mathf.FloorToInt((world.x - _origin.x) / _cellSize),
            Mathf.FloorToInt((world.y - _origin.y) / _cellSize));

        private void ForEach(Vector2Int origin, Vector2Int footprint, Action<GridCell> action)
        {
            for (var x = 0; x < footprint.x; x++)
            for (var y = 0; y < footprint.y; y++)
            {
                GridCell cell;
                if (_cells.TryGetValue(origin + new Vector2Int(x, y), out cell)) action(cell);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.7f, 0.8f, 0.2f);
            for (var x = 0; x < _size.x; x++)
            for (var y = 0; y < _size.y; y++)
                Gizmos.DrawWireCube(CellToWorld(new Vector2Int(x, y)), Vector3.one * (_cellSize * 0.96f));
        }
    }
}
