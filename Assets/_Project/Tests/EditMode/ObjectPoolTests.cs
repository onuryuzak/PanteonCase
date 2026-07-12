using NUnit.Framework;
using Panteon.Core;
using UnityEngine;

namespace Panteon.Tests
{
    public sealed class ObjectPoolTests
    {
        [Test]
        public void ReturnThenGet_ReusesInstance_AndPoolGrows()
        {
            var prefab = new GameObject("Prefab");
            var parent = new GameObject("PoolRoot");
            var pool = new ObjectPool(prefab, parent.transform, 1);
            var first = pool.Get();
            var grown = pool.Get();
            Assert.That(grown, Is.Not.SameAs(first));
            pool.Return(first);
            Assert.That(pool.Get(), Is.SameAs(first));
            Object.DestroyImmediate(grown);
            Object.DestroyImmediate(parent);
            Object.DestroyImmediate(prefab);
        }
    }
}

