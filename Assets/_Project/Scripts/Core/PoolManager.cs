using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Panteon.Core
{
    // Prefab is the key, so returned objects always reach the right pool.
    public sealed class PoolManager : MonoBehaviour
    {
        private readonly Dictionary<GameObject, ObjectPool> _pools = new Dictionary<GameObject, ObjectPool>();

        public GameObject Get(GameObject prefab, Transform parent = null)
        {
            return GetOrCreatePool(prefab, parent).Get();
        }

        public void Return(GameObject instance, GameObject prefab)
        {
            ObjectPool pool;
            if (_pools.TryGetValue(prefab, out pool)) pool.Return(instance);
            else Destroy(instance);
        }

        public IEnumerator PrewarmAsync(GameObject prefab, int count, int perFrame = 4, Transform parent = null)
        {
            if (prefab == null || count <= 0) yield break;
            var pool = GetOrCreatePool(prefab, parent);
            var batchSize = Mathf.Max(1, perFrame);
            // Split prewarming across frames; doing the full catalog at once causes a hitch.
            for (var remaining = count; remaining > 0;)
            {
                var batch = Mathf.Min(batchSize, remaining);
                pool.Prewarm(batch);
                remaining -= batch;
                if (remaining > 0) yield return null;
            }
        }

        private ObjectPool GetOrCreatePool(GameObject prefab, Transform parent = null)
        {
            ObjectPool pool;
            if (_pools.TryGetValue(prefab, out pool)) return pool;
            pool = new ObjectPool(prefab, parent != null ? parent : transform);
            _pools.Add(prefab, pool);
            return pool;
        }
    }
}
