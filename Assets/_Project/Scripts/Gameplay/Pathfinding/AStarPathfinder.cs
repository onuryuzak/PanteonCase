using System.Collections.Generic;
using Panteon.Gameplay.Grid;
using UnityEngine;

namespace Panteon.Gameplay.Pathfinding
{
    public sealed class AStarPathfinder : IPathfinder
    {
        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal, GridManager grid)
        {
            if (grid == null || !grid.IsWalkable(start) || !grid.IsWalkable(goal)) return null;
            if (start == goal) return new List<Vector2Int> { start };

            var open = new BinaryMinHeap<PathNode>(grid.CellCount);
            var lookup = new Dictionary<Vector2Int, PathNode>();
            var closed = new HashSet<Vector2Int>();
            var startNode = new PathNode { Cell = start, GCost = 0, HCost = Heuristic(start, goal) };
            open.Add(startNode);
            lookup.Add(start, startNode);

            while (open.Count > 0)
            {
                var current = open.RemoveFirst();
                lookup.Remove(current.Cell);
                if (current.Cell == goal) return Retrace(current);
                closed.Add(current.Cell);

                foreach (var cell in grid.GetNeighbors(current.Cell))
                {
                    if (closed.Contains(cell) || !grid.IsWalkable(cell)) continue;
                    var tentative = current.GCost + StepCost(current.Cell, cell);
                    PathNode neighbor;
                    var isOpen = lookup.TryGetValue(cell, out neighbor);
                    if (isOpen && tentative >= neighbor.GCost) continue;
                    if (!isOpen) neighbor = new PathNode { Cell = cell };
                    neighbor.GCost = tentative;
                    neighbor.HCost = Heuristic(cell, goal);
                    neighbor.Parent = current;
                    if (isOpen) open.UpdateItem(neighbor);
                    else { lookup.Add(cell, neighbor); open.Add(neighbor); }
                }
            }
            return null;
        }

        private static int StepCost(Vector2Int a, Vector2Int b) => a.x != b.x && a.y != b.y ? 14 : 10;

        private static int Heuristic(Vector2Int a, Vector2Int b)
        {
            var dx = Mathf.Abs(a.x - b.x);
            var dy = Mathf.Abs(a.y - b.y);
            return 14 * Mathf.Min(dx, dy) + 10 * Mathf.Abs(dx - dy);
        }

        private static List<Vector2Int> Retrace(PathNode end)
        {
            var path = new List<Vector2Int>();
            for (var node = end; node != null; node = node.Parent) path.Add(node.Cell);
            path.Reverse();
            return path;
        }
    }
}
