using System;
using System.Collections.Generic;
using UnityEngine;

namespace Panteon.Core
{
    public sealed class ObjectPool
    {
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly Stack<GameObject> _inactive = new Stack<GameObject>();
        private readonly List<IPoolable> _poolableBuffer = new List<IPoolable>();

        public ObjectPool(GameObject prefab, Transform parent, int prewarm = 0)
        {
            _prefab = prefab != null ? prefab : throw new ArgumentNullException(nameof(prefab));
            _parent = parent;
            for (var i = 0; i < prewarm; i++) _inactive.Push(CreateNew());
        }

        public GameObject Get()
        {
            var instance = _inactive.Count > 0 ? _inactive.Pop() : CreateNew();
            instance.SetActive(true);
            NotifyPoolables(instance, item => item.OnTakenFromPool());
            return instance;
        }

        public void Return(GameObject instance)
        {
            if (instance == null) return;
            NotifyPoolables(instance, item => item.OnReturnedToPool());
            instance.SetActive(false);
            instance.transform.SetParent(_parent, false);
            _inactive.Push(instance);
        }

        public void Prewarm(int count)
        {
            for (var i = 0; i < count; i++) _inactive.Push(CreateNew());
        }

        private GameObject CreateNew()
        {
            var instance = UnityEngine.Object.Instantiate(_prefab, _parent);
            instance.SetActive(false);
            return instance;
        }

        private void NotifyPoolables(GameObject instance, Action<IPoolable> callback)
        {
            _poolableBuffer.Clear();
            instance.GetComponentsInChildren(true, _poolableBuffer);
            foreach (var listener in _poolableBuffer) callback(listener);
            _poolableBuffer.Clear();
        }
    }
}
