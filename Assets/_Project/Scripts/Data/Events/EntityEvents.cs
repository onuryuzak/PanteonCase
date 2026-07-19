using Panteon.Core;

namespace Panteon.Data
{
    public readonly struct EntityHealthChanged
    {
        public readonly IDamageable Entity;
        public readonly int Current;
        public readonly int Max;

        public EntityHealthChanged(IDamageable entity, int current, int max)
        {
            Entity = entity;
            Current = current;
            Max = max;
        }
    }

    public readonly struct EntityDied
    {
        public readonly IDamageable Entity;

        public EntityDied(IDamageable entity) => Entity = entity;
    }
}
