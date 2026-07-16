using UnityEngine;

namespace Panteon.Data
{
    public enum UnitAttackRangeType { CloseRange, Ranged }

    [CreateAssetMenu(menuName = "Panteon/Unit Visual Profile", fileName = "SO_UnitVisual")]
    public sealed class UnitVisualProfileSO : ScriptableObject
    {
        [SerializeField] private RuntimeAnimatorController _controller;
        [SerializeField] private Sprite _referenceSprite;
        [SerializeField, Min(0.1f)] private float _visualHeightInCells = 0.9f;
        [SerializeField, Range(0.01f, 1f)] private float _contentHeightRatio = 1f;
        [SerializeField, Min(0f)] private float _attackImpactDelay = 0.2f;
        [SerializeField, Min(0f)] private float _hitAnimationDuration = 0.3f;
        [SerializeField] private Sprite _projectileSprite;
        [SerializeField] private bool _usesAttackFire;

        public RuntimeAnimatorController Controller => _controller;
        public Sprite ReferenceSprite => _referenceSprite;
        public float VisualHeightInCells => _visualHeightInCells;
        public float ContentHeightRatio => _contentHeightRatio;
        public float AttackImpactDelay => _attackImpactDelay;
        public float HitAnimationDuration => _hitAnimationDuration;
        public Sprite ProjectileSprite => _projectileSprite;
        public bool UsesAttackFire => _usesAttackFire;
    }
}
