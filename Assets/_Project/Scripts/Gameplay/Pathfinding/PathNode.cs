using UnityEngine;

namespace Panteon.Gameplay.Pathfinding
{
    public sealed class PathNode : IHeapItem<PathNode>
    {
        public Vector2Int Cell;
        public int GCost;
        public int HCost;
        public int FCost => GCost + HCost;
        public PathNode Parent;
        public int HeapIndex { get; set; } = -1;

        public int CompareTo(PathNode other)
        {
            var result = FCost.CompareTo(other.FCost);
            return result != 0 ? result : HCost.CompareTo(other.HCost);
        }
    }
}

