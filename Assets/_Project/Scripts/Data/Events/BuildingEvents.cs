using UnityEngine;

namespace Panteon.Data
{
    public readonly struct BuildingPlaced
    {
        public readonly IProductionBuilding Building;

        public BuildingPlaced(IProductionBuilding building) => Building = building;
    }

    public readonly struct BuildingPlacementFailed
    {
        public readonly Vector2Int Cell;

        public BuildingPlacementFailed(Vector2Int cell) => Cell = cell;
    }
}
