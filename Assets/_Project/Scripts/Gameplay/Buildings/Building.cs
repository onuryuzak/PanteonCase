using System;
using Panteon.Core;
using Panteon.Data;
using Panteon.Gameplay.Grid;
using UnityEngine;

namespace Panteon.Gameplay.Buildings
{
    public class Building : Entity, IProductionBuilding, IPoolable
    {
        private GridManager _grid;
        private EventBus _bus;
        private Action<Building> _returnToPool;
        private ProductionComponent _production;
        private GridOccupant _gridOccupant;

        public BuildingDefinitionSO Definition { get; private set; }
        public string DisplayName => Definition.DisplayName;
        public Sprite Icon => Definition.Icon;
        public bool CanProduce => _production != null && _production.HasProducts;
        public System.Collections.Generic.IReadOnlyList<UnitDefinitionSO> Producibles => _production != null ? _production.Producibles : Definition.Producibles;
        public Faction Faction => Health.Faction;
        public int CurrentHP => Health.CurrentHP;
        public int MaxHP => Health.MaxHP;
        public bool IsDead => Health.IsDead;
        public event Action<int, int> OnHealthChanged
        { add => Health.OnHealthChanged += value; remove => Health.OnHealthChanged -= value; }
        public event Action OnDied
        { add => Health.OnDied += value; remove => Health.OnDied -= value; }

        protected override void Awake()
        {
            base.Awake();
            _production = GetComponent<ProductionComponent>();
            _gridOccupant = GetComponent<GridOccupant>();
            if (_gridOccupant == null) _gridOccupant = gameObject.AddComponent<GridOccupant>();
        }

        public void Initialize(BuildingDefinitionSO definition, Vector2Int cell, GridManager grid,
            EventBus bus, Action<Building> returnToPool)
        {
            Definition = definition != null ? definition : throw new ArgumentNullException(nameof(definition));
            GridPosition = cell;
            _grid = grid;
            _bus = bus;
            _returnToPool = returnToPool;
            Health.OnHealthChanged -= HandleHealthChanged;
            Health.OnDied -= HandleDied;
            Health.OnHealthChanged += HandleHealthChanged;
            Health.OnDied += HandleDied;
            Health.Initialize(definition.MaxHP);
            BindHealthBar(this, true);
            BindDamageFeedback(this);
            transform.localScale = new Vector3(definition.FootprintSize.x, definition.FootprintSize.y, 1f);
            if (definition.CanProduce && _production == null)
                _production = gameObject.AddComponent<ProductionComponent>();
            _production?.Initialize(definition.CanProduce ? definition.Producibles : null);
            name = $"BLD_{definition.DisplayName.Replace(" ", string.Empty)}_{GetInstanceID():000}";
        }

        public void TakeDamage(int amount) => Health.TakeDamage(amount);
        public void OccupyGrid()
        {
            _gridOccupant.Occupy(_grid, this, GridPosition, Definition.FootprintSize);
        }

        public bool TryGetSpawnCell(out Vector2Int cell)
        {
            var preferred = GridPosition + Definition.SpawnPointOffset;
            if (_grid != null && _grid.IsWalkable(preferred))
            {
                cell = preferred;
                return true;
            }

            var bestCell = default(Vector2Int);
            var bestDistance = int.MaxValue;
            var footprint = Definition.FootprintSize;

            // Evaluate every orthogonal edge around the footprint. The preferred
            // SO offset still wins when open; otherwise the closest open edge wins.
            for (var x = 0; x < footprint.x; x++)
            {
                ConsiderSpawnCandidate(GridPosition + new Vector2Int(x, -1), preferred, ref bestCell, ref bestDistance);
                ConsiderSpawnCandidate(GridPosition + new Vector2Int(x, footprint.y), preferred, ref bestCell, ref bestDistance);
            }
            for (var y = 0; y < footprint.y; y++)
            {
                ConsiderSpawnCandidate(GridPosition + new Vector2Int(-1, y), preferred, ref bestCell, ref bestDistance);
                ConsiderSpawnCandidate(GridPosition + new Vector2Int(footprint.x, y), preferred, ref bestCell, ref bestDistance);
            }

            cell = bestCell;
            return bestDistance != int.MaxValue;
        }
        public void OnTakenFromPool() { }

        public void OnReturnedToPool()
        {
            Deselect();
            _gridOccupant?.Release();
            Health.OnHealthChanged -= HandleHealthChanged;
            Health.OnDied -= HandleDied;
        }

        private void HandleHealthChanged(int current, int max) =>
            _bus?.Publish(new EntityHealthChanged(this, current, max));

        private void HandleDied()
        {
            _gridOccupant?.Release();
            _bus?.Publish(new EntityDied(this));
            _returnToPool?.Invoke(this);
        }

        private void ConsiderSpawnCandidate(Vector2Int candidate, Vector2Int preferred,
            ref Vector2Int bestCell, ref int bestDistance)
        {
            if (_grid == null || !_grid.IsWalkable(candidate)) return;
            var delta = candidate - preferred;
            var distance = delta.x * delta.x + delta.y * delta.y;
            if (distance >= bestDistance) return;
            bestCell = candidate;
            bestDistance = distance;
        }
    }
}
