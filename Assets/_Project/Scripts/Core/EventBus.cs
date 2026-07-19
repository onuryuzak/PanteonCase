using System;
using System.Collections.Generic;

namespace Panteon.Core
{
    // Gameplay and HUD talk through this bus instead of referencing each other.
    public sealed class EventBus
    {
        private readonly Dictionary<Type, Delegate> _subscribers = new Dictionary<Type, Delegate>();

        public void Subscribe<T>(Action<T> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            Delegate existing;
            _subscribers.TryGetValue(typeof(T), out existing);
            _subscribers[typeof(T)] = Delegate.Combine(existing, handler);
        }

        public void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null) return;
            Delegate existing;
            if (!_subscribers.TryGetValue(typeof(T), out existing)) return;
            var remaining = Delegate.Remove(existing, handler);
            if (remaining == null) _subscribers.Remove(typeof(T));
            else _subscribers[typeof(T)] = remaining;
        }

        public void Publish<T>(T message)
        {
            Delegate existing;
            // Keep this synchronous. The HUD expects selection to be current right away.
            if (_subscribers.TryGetValue(typeof(T), out existing))
                ((Action<T>)existing)?.Invoke(message);
        }

        public void Clear() => _subscribers.Clear();
    }
}

