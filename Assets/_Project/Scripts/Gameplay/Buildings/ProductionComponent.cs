using System;
using System.Collections.Generic;
using Panteon.Data;
using UnityEngine;

namespace Panteon.Gameplay.Buildings
{
    public sealed class ProductionComponent : MonoBehaviour
    {
        private readonly List<UnitDefinitionSO> _producibles = new List<UnitDefinitionSO>();
        public IReadOnlyList<UnitDefinitionSO> Producibles => _producibles;
        public bool HasProducts => _producibles.Count > 0;

        public void Initialize(IReadOnlyList<UnitDefinitionSO> definitions)
        {
            _producibles.Clear();
            if (definitions == null) return;
            foreach (var definition in definitions)
                if (definition != null) _producibles.Add(definition);
        }
    }
}
