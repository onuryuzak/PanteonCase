using Panteon.Gameplay.Buildings;
using UnityEngine;

namespace Panteon.Gameplay.Grid
{
    public sealed class GridOccupant : MonoBehaviour
    {
        private GridManager _grid;
        private Building _building;
        private Vector2Int _origin;
        private Vector2Int _footprint;
        private bool _occupied;

        public void Occupy(GridManager grid, Building building, Vector2Int origin, Vector2Int footprint)
        {
            Release();
            _grid = grid;
            _building = building;
            _origin = origin;
            _footprint = footprint;
            _grid.Occupy(_origin, _footprint, _building);
            _occupied = true;
        }

        public void Release()
        {
            if (!_occupied) return;
            _grid.Free(_origin, _footprint, _building);
            _occupied = false;
        }
    }
}
