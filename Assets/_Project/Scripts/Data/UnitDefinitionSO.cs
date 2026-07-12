using UnityEngine;

namespace Panteon.Data
{
    [CreateAssetMenu(menuName = "Panteon/Unit Definition", fileName = "SO_Unit")]
    public sealed class UnitDefinitionSO : ScriptableObject
    {
        [SerializeField] private string _displayName = "Soldier";
        [SerializeField] private Sprite _icon;
        [SerializeField] private GameObject _prefab;
        [SerializeField, Min(1)] private int _maxHP = 10;
        [SerializeField, Min(0)] private int _attackDamage = 5;
        [SerializeField, Min(0.1f)] private float _moveSpeed = 3f;
        [SerializeField, Min(0.1f)] private float _attackRange = 1f;
        [SerializeField, Min(0.05f)] private float _attackCooldown = 1f;

        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public GameObject Prefab => _prefab;
        public int MaxHP => _maxHP;
        public int AttackDamage => _attackDamage;
        public float MoveSpeed => _moveSpeed;
        public float AttackRange => _attackRange;
        public float AttackCooldown => _attackCooldown;
    }
}

