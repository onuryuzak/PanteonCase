using Panteon.Gameplay.Buildings;
using UnityEngine;

namespace Panteon.Gameplay.Grid
{
    public sealed class GridCell
    {
        public Vector2Int Position { get; }
        public bool IsTerrainWalkable { get; set; }
        public Building Occupant { get; set; }
        public bool IsWalkable => IsTerrainWalkable && Occupant == null;

        public GridCell(Vector2Int position, bool isTerrainWalkable = true)
        {
            Position = position;
            IsTerrainWalkable = isTerrainWalkable;
        }
    }
}

