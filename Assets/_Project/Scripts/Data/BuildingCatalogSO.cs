using System.Collections.Generic;
using UnityEngine;

namespace Panteon.Data
{
    [CreateAssetMenu(menuName = "Panteon/Building Catalog", fileName = "SO_BuildingCatalog")]
    public sealed class BuildingCatalogSO : ScriptableObject
    {
        [SerializeField] private List<BuildingDefinitionSO> _buildings = new List<BuildingDefinitionSO>();
        public IReadOnlyList<BuildingDefinitionSO> Buildings => _buildings;
    }
}

