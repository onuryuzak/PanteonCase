using System;

namespace Panteon.Gameplay.Units
{
    public enum UnitState { Idle, Moving, Attacking, Dead }

    public sealed class UnitStateMachine
    {
        public UnitState Current { get; private set; } = UnitState.Idle;
        public event Action<UnitState, UnitState> OnStateChanged;

        public void ChangeState(UnitState next)
        {
            if (Current == next) return;
            var previous = Current;
            Current = next;
            OnStateChanged?.Invoke(previous, next);
        }
    }
}

