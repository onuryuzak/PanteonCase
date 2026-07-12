using System;
using System.Collections.Generic;

namespace Panteon.Core
{
    public sealed class ServiceLocator
    {
        private static ServiceLocator _instance;
        public static ServiceLocator Instance => _instance ?? (_instance = new ServiceLocator());

        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        private ServiceLocator() { }

        public void Register<T>(T service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            _services[typeof(T)] = service;
        }

        public T Get<T>()
        {
            T service;
            if (!TryGet(out service))
                throw new InvalidOperationException($"Service {typeof(T).Name} has not been registered.");
            return service;
        }

        public bool TryGet<T>(out T service)
        {
            object value;
            if (_services.TryGetValue(typeof(T), out value) && value is T)
            {
                service = (T)value;
                return true;
            }

            service = default(T);
            return false;
        }

        public void Clear() => _services.Clear();
    }
}

