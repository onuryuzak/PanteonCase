using Panteon.Core;
using Panteon.Gameplay.Combat;
using Panteon.Gameplay.Selection;
using UnityEngine;

namespace Panteon.Gameplay
{
    [RequireComponent(typeof(HealthComponent), typeof(SelectableComponent))]
    public abstract class Entity : MonoBehaviour, ISelectable
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

        protected void BindHealthBar(IDamageable source) => _healthBar?.Bind(source);
        protected void BindDamageFeedback(IDamageable source) => _damageFeedback?.Bind(source);

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
