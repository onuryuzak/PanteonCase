using System;

namespace Panteon.Gameplay.Pathfinding
{
    public interface IHeapItem<T> : IComparable<T>
    {
        int HeapIndex { get; set; }
    }
}

