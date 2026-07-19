using System.Collections.Generic;
using UnityEngine;

namespace Panteon.Data
{
    [CreateAssetMenu(menuName = "Panteon/Building Catalog", fileName = "SO_BuildingCatalog")]
    // This is the single list used by the production menu and pool prewarming.
    public sealed class BuildingCatalogSO : ScriptableObject
    {
        [SerializeField] private List<BuildingDefinitionSO> _buildings = new List<BuildingDefinitionSO>();
        public IReadOnlyList<BuildingDefinitionSO> Buildings => _buildings;
    }
}

