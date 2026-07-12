using System;

namespace Panteon.Core
{
    public interface IDamageable
    {
        Faction Faction { get; }
        int CurrentHP { get; }
        int MaxHP { get; }
        bool IsDead { get; }
        event Action<int, int> OnHealthChanged;
        event Action OnDied;
        void TakeDamage(int amount);
    }
}

