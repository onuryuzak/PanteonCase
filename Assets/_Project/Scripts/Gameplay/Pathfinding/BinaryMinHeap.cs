using System;

namespace Panteon.Gameplay.Pathfinding
{
    // Minimal heap implementation used only by A*.
    public sealed class BinaryMinHeap<T> where T : IHeapItem<T>
    {
        private T[] _items;
        public int Count { get; private set; }

        public BinaryMinHeap(int capacity) => _items = new T[Math.Max(1, capacity)];

        public void Add(T item)
        {
            if (Count == _items.Length) Array.Resize(ref _items, _items.Length * 2);
            item.HeapIndex = Count;
            _items[Count] = item;
            Count++;
            SortUp(item);
        }

        public T RemoveFirst()
        {
            if (Count == 0) throw new InvalidOperationException("Heap is empty.");
            // The cheapest node is always at index zero.
            var first = _items[0];
            Count--;
            if (Count > 0)
            {
                _items[0] = _items[Count];
                _items[0].HeapIndex = 0;
                SortDown(_items[0]);
            }
            _items[Count] = default(T);
            first.HeapIndex = -1;
            return first;
        }

        public void UpdateItem(T item) => SortUp(item);
        public bool Contains(T item) => item.HeapIndex >= 0 && item.HeapIndex < Count && Equals(_items[item.HeapIndex], item);

        private void SortDown(T item)
        {
            while (true)
            {
                var left = item.HeapIndex * 2 + 1;
                var right = left + 1;
                var best = item.HeapIndex;
                if (left < Count && _items[left].CompareTo(_items[best]) < 0) best = left;
                if (right < Count && _items[right].CompareTo(_items[best]) < 0) best = right;
                if (best == item.HeapIndex) return;
                Swap(item, _items[best]);
            }
        }

        private void SortUp(T item)
        {
            while (item.HeapIndex > 0)
            {
                var parentIndex = (item.HeapIndex - 1) / 2;
                var parent = _items[parentIndex];
                if (item.CompareTo(parent) >= 0) return;
                Swap(item, parent);
            }
        }

        private void Swap(T a, T b)
        {
            var aIndex = a.HeapIndex;
            var bIndex = b.HeapIndex;
            _items[aIndex] = b;
            _items[bIndex] = a;
            a.HeapIndex = bIndex;
            b.HeapIndex = aIndex;
        }
    }
}

