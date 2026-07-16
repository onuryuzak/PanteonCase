using System.Collections.Generic;
using Panteon.Core;
using UnityEngine;

namespace Panteon.Data
{
    public readonly struct BuildingSelected
    {
        public readonly IProductionBuilding Building;
        public BuildingSelected(IProductionBuilding building) => Building = building;
    }

    public readonly struct UnitSelected
    {
        public readonly IReadOnlyList<IEntityPresentation> Units;
        public UnitSelected(IReadOnlyList<IEntityPresentation> units) => Units = units;
    }

    public readonly struct SelectionCleared { }

    public readonly struct BuildingPlaced
    {
        public readonly IProductionBuilding Building;
        public BuildingPlaced(IProductionBuilding building) => Building = building;
    }

    public readonly struct BuildingPlacementFailed
    {
        public readonly Vector2Int Cell;
        public BuildingPlacementFailed(Vector2Int cell) => Cell = cell;
    }

    public readonly struct EntityHealthChanged
    {
        public readonly IDamageable Entity;
        public readonly int Current;
        public readonly int Max;
        public EntityHealthChanged(IDamageable entity, int current, int max)
        { Entity = entity; Current = current; Max = max; }
    }

    public readonly struct EntityDied
    {
        public readonly IDamageable Entity;
        public EntityDied(IDamageable entity) => Entity = entity;
    }

    public readonly struct CameraShakeRequested
    {
        public readonly float Amplitude;
        public readonly float Duration;

        public CameraShakeRequested(float amplitude, float duration)
        {
            Amplitude = Mathf.Max(0f, amplitude);
            Duration = Mathf.Max(0f, duration);
        }
    }

    public enum CommandFeedbackType
    {
        Move,
        Attack
    }

    public readonly struct CommandFeedbackRequested
    {
        public readonly Vector3 WorldPosition;
        public readonly CommandFeedbackType Type;

        public CommandFeedbackRequested(Vector3 worldPosition, CommandFeedbackType type)
        {
            WorldPosition = worldPosition;
            Type = type;
        }
    }

    public readonly struct ProductionRequested
    {
        public readonly IProductionBuilding Source;
        public readonly UnitDefinitionSO UnitDefinition;
        public ProductionRequested(IProductionBuilding source, UnitDefinitionSO unitDefinition)
        { Source = source; UnitDefinition = unitDefinition; }
    }

    public readonly struct UnitSpawned
    {
        public readonly IEntityPresentation Unit;
        public UnitSpawned(IEntityPresentation unit) => Unit = unit;
    }
}
