using System.Collections.Generic;
using Panteon.Core;
using UnityEngine;

namespace Panteon.Data
{
    // Shared contracts let UI consume gameplay state without referencing Gameplay assembly types.
    public interface IEntityPresentation : IDamageable, ISelectable
    {
        string DisplayName { get; }
        Sprite Icon { get; }
    }

    public interface IUnitPresentation : IEntityPresentation
    {
        string Description { get; }
        int AttackDamage { get; }
    }

    public interface IProductionBuilding : IEntityPresentation
    {
        bool CanProduce { get; }
        IReadOnlyList<UnitDefinitionSO> Producibles { get; }
        bool TryGetSpawnCell(out Vector2Int cell);
    }

    public interface IBuildingPlacementService
    {
        bool IsPlacing { get; }
        void EnterPlacementMode(BuildingDefinitionSO definition);
        void CancelPlacement();
    }

    public interface IInputBlocker
    {
        bool IsPointerBlocked(Vector2 screenPosition);
    }
}
