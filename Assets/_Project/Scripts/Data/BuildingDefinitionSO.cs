using System.Collections.Generic;
using UnityEngine;

namespace Panteon.Data
{
    [CreateAssetMenu(menuName = "Panteon/Building Definition", fileName = "SO_Building")]
    public sealed class BuildingDefinitionSO : ScriptableObject
    {
        [SerializeField] private string _displayName = "Building";
        [SerializeField] private string _description = "Faction building";
        [SerializeField] private Sprite _icon;
        [SerializeField] private GameObject _prefab;
        [SerializeField] private Vector2Int _footprintSize = Vector2Int.one;
        [SerializeField, Min(1)] private int _maxHP = 100;
        [SerializeField] private bool _canProduce;
        [SerializeField] private List<UnitDefinitionSO> _producibles = new List<UnitDefinitionSO>();
        [SerializeField] private Vector2Int _spawnPointOffset = new Vector2Int(0, -1);

        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public GameObject Prefab => _prefab;
        public Vector2Int FootprintSize => _footprintSize;
        public int MaxHP => _maxHP;
        public bool CanProduce => _canProduce;
        public IReadOnlyList<UnitDefinitionSO> Producibles => _producibles;
        public Vector2Int SpawnPointOffset => _spawnPointOffset;
    }
}

