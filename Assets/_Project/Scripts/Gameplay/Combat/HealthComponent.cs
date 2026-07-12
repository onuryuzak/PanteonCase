using System;
using Panteon.Core;
using UnityEngine;

namespace Panteon.Gameplay.Combat
{
    public sealed class HealthComponent : MonoBehaviour, IDamageable
    {
        [SerializeField] private Faction _faction = Faction.Player;
        public Faction Faction => _faction;
        public int CurrentHP { get; private set; }
        public int MaxHP { get; private set; }
        public bool IsDead => CurrentHP <= 0;
        public event Action<int, int> OnHealthChanged;
        public event Action OnDied;

        public void Initialize(int maxHP, Faction faction = Faction.Player)
        {
            if (maxHP <= 0) throw new ArgumentOutOfRangeException(nameof(maxHP));
            _faction = faction;
            MaxHP = maxHP;
            CurrentHP = maxHP;
            OnHealthChanged?.Invoke(CurrentHP, MaxHP);
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || IsDead) return;
            CurrentHP = Mathf.Max(0, CurrentHP - amount);
            OnHealthChanged?.Invoke(CurrentHP, MaxHP);
            if (CurrentHP == 0) OnDied?.Invoke();
        }
    }
}

