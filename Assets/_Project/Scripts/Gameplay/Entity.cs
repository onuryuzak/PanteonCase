using Panteon.Core;
using Panteon.Gameplay.Combat;
using Panteon.Gameplay.Selection;
using UnityEngine;

namespace Panteon.Gameplay
{
    [RequireComponent(typeof(HealthComponent), typeof(SelectableComponent))]
    // Shared setup used by both Unit and Building.
    public abstract class Entity : MonoBehaviour, ISelectable, ICombatFeedbackReceiver
    {
        protected HealthComponent Health { get; private set; }
        private SelectableComponent _selection;
        private HealthBarView _healthBar;
        private DamageFeedbackView _damageFeedback;
        public Vector2Int GridPosition { get; protected set; }
        public bool IsSelected => _selection != null && _selection.IsSelected;

        protected virtual void Awake()
        {
            Health = GetComponent<HealthComponent>();
            _selection = GetComponent<SelectableComponent>();
            if (_selection == null) _selection = gameObject.AddComponent<SelectableComponent>();
            _healthBar = HealthBarView.Ensure(gameObject);
            _damageFeedback = DamageFeedbackView.Ensure(gameObject);
        }

        protected void BindHealthBar(IDamageable source, bool alwaysVisible = false) =>
            _healthBar?.Bind(source, alwaysVisible);
        protected void BindDamageFeedback(IDamageable source) => _damageFeedback?.Bind(source);
        protected void PlaySpawnFeedback() => _damageFeedback?.PlaySpawn();
        protected void PlayRevealSpawnFeedback() => _damageFeedback?.PlayRevealSpawn();
        protected void PlayDeathFeedback(System.Action completed) => _damageFeedback?.PlayDeath(completed);
        protected void ResetFeedback() => _damageFeedback?.ResetFeedback();

        public void PrepareImpact(Vector3 sourceWorldPosition) =>
            _damageFeedback?.PrepareImpact(sourceWorldPosition);

        public virtual void Select()
        {
            _selection.Select();
        }

        public virtual void Deselect()
        {
            _selection.Deselect();
        }
    }
}
