using System;
using UnityEngine;

namespace Panteon.Data
{
    public enum UnitAttackRangeType { CloseRange, Ranged }

    [Serializable]
    public sealed class DirectionalAnimationStates
    {
        [SerializeField] private string _up;
        [SerializeField] private string _upRight;
        [SerializeField] private string _right;
        [SerializeField] private string _downRight;
        [SerializeField] private string _down;

        public string Resolve(Vector2 direction)
        {
            var x = Mathf.Abs(direction.x);
            var y = direction.y;
            if (x < 0.25f) return y >= 0f ? First(_up, _right) : First(_down, _right);
            if (y > x * 0.45f) return First(_upRight, _right);
            if (y < -x * 0.45f) return First(_downRight, _right);
            return _right;
        }

        private static string First(string preferred, string fallback) =>
            string.IsNullOrWhiteSpace(preferred) ? fallback : preferred;
    }

    [CreateAssetMenu(menuName = "Panteon/Unit Visual Profile", fileName = "SO_UnitVisual")]
    public sealed class UnitVisualProfileSO : ScriptableObject
    {
        [SerializeField] private RuntimeAnimatorController _controller;
        [SerializeField] private Sprite _referenceSprite;
        [SerializeField] private string _idleState;
        [SerializeField] private string _moveState;
        [SerializeField] private DirectionalAnimationStates _attackStates = new DirectionalAnimationStates();
        [SerializeField] private string _secondaryAttackState;
        [SerializeField] private DirectionalAnimationStates _hitStates = new DirectionalAnimationStates();
        [SerializeField, Min(0.1f)] private float _visualHeightInCells = 0.9f;
        [SerializeField, Range(0.01f, 1f)] private float _contentHeightRatio = 1f;
        [SerializeField, Min(0f)] private float _attackImpactDelay = 0.2f;
        [SerializeField, Min(0f)] private float _hitAnimationDuration = 0.3f;
        [SerializeField] private Sprite _projectileSprite;
        [SerializeField] private bool _usesAttackFire;

        public RuntimeAnimatorController Controller => _controller;
        public Sprite ReferenceSprite => _referenceSprite;
        public string IdleState => _idleState;
        public string MoveState => _moveState;
        public string SecondaryAttackState => _secondaryAttackState;
        public float VisualHeightInCells => _visualHeightInCells;
        public float ContentHeightRatio => _contentHeightRatio;
        public float AttackImpactDelay => _attackImpactDelay;
        public float HitAnimationDuration => _hitAnimationDuration;
        public Sprite ProjectileSprite => _projectileSprite;
        public bool UsesAttackFire => _usesAttackFire;
        public string ResolveAttackState(Vector2 direction) => _attackStates.Resolve(direction);
        public string ResolveHitState(Vector2 direction) => _hitStates.Resolve(direction);
    }
}
