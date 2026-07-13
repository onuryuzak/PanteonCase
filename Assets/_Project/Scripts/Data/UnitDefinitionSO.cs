using UnityEngine;

namespace Panteon.Data
{
    [CreateAssetMenu(menuName = "Panteon/Unit Definition", fileName = "SO_Unit")]
    public sealed class UnitDefinitionSO : ScriptableObject
    {
        [SerializeField] private string _displayName = "Soldier";
        [SerializeField] private string _description = "Soldier for Desert";
        [SerializeField] private Sprite _icon;
        [SerializeField] private Rect _iconContentRect = new Rect(0f, 0f, 1f, 1f);
        [SerializeField, Range(0.5f, 2f)] private float _productionIconScale = 1f;
        [SerializeField] private GameObject _prefab;
        [SerializeField] private UnitVisualProfileSO _visualProfile;
        [SerializeField, Min(1)] private int _maxHP = 10;
        [SerializeField, Min(0)] private int _attackDamage = 5;
        [SerializeField, Min(0.1f)] private float _moveSpeed = 3f;
        [SerializeField] private UnitAttackRangeType _attackRangeType = UnitAttackRangeType.CloseRange;
        [SerializeField, Min(1f)] private float _rangedAttackRange = 4f;
        [SerializeField, Min(0.05f)] private float _attackCooldown = 1f;

        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public Rect IconContentRect => _iconContentRect.width > 0f && _iconContentRect.height > 0f
            ? _iconContentRect
            : new Rect(0f, 0f, 1f, 1f);
        public float ProductionIconScale => _productionIconScale;
        public GameObject Prefab => _prefab;
        public UnitVisualProfileSO VisualProfile => _visualProfile;
        public int MaxHP => _maxHP;
        public int AttackDamage => _attackDamage;
        public float MoveSpeed => _moveSpeed;
        public UnitAttackRangeType AttackRangeType => _attackRangeType;
        public float AttackRange => _attackRangeType == UnitAttackRangeType.CloseRange ? 1f : _rangedAttackRange;
        public float AttackCooldown => _attackCooldown;
    }
}

