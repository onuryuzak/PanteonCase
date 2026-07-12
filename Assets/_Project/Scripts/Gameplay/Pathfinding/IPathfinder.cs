using System.Collections.Generic;
using Panteon.Gameplay.Grid;
using UnityEngine;

namespace Panteon.Gameplay.Pathfinding
{
    public interface IPathfinder
    {
        List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal, GridManager grid);
    }
}

