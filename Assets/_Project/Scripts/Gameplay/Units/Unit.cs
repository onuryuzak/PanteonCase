using System;
using System.Collections;
using System.Collections.Generic;
using Panteon.Core;
using Panteon.Data;
using Panteon.Gameplay.Buildings;
using Panteon.Gameplay.Combat;
using Panteon.Gameplay.Grid;
using Panteon.Gameplay.Pathfinding;
using UnityEngine;

namespace Panteon.Gameplay.Units
{
    public sealed class Unit : Entity, IEntityPresentation, IPoolable
    {
        private GridManager _grid;
        private IPathfinder _pathfinder;
        private EventBus _bus;
        private Action<Unit> _returnToPool;
        private Coroutine _currentCommand;
        private AttackFireFeedbackView _attackFireFeedback;
        private UnitPathPreview _pathPreview;
        private int _pathGridRevision;
        private const float SnapDuration = 0.12f;
        public UnitDefinitionSO Definition { get; private set; }
        public string DisplayName => Definition.DisplayName;
        public Sprite Icon => Definition.Icon;
        public UnitStateMachine StateMachine { get; } = new UnitStateMachine();
        public Faction Faction => Health.Faction;
        public int CurrentHP => Health.CurrentHP;
        public int MaxHP => Health.MaxHP;
        public bool IsDead => Health.IsDead;
        public event Action<int, int> OnHealthChanged
        { add => Health.OnHealthChanged += value; remove => Health.OnHealthChanged -= value; }
        public event Action OnDied
        { add => Health.OnDied += value; remove => Health.OnDied -= value; }

        public void Initialize(UnitDefinitionSO definition, Vector2Int cell, GridManager grid,
            IPathfinder pathfinder, EventBus bus, Action<Unit> returnToPool, Faction faction = Faction.Player)
        {
            Definition = definition != null ? definition : throw new ArgumentNullException(nameof(definition));
            GridPosition = cell;
            _grid = grid;
            _pathfinder = pathfinder;
            _bus = bus;
            _returnToPool = returnToPool;
            CancelCurrentCommand();
            Health.OnHealthChanged -= HandleHealthChanged;
            Health.OnDied -= HandleDied;
            Health.OnHealthChanged += HandleHealthChanged;
            Health.OnDied += HandleDied;
            Health.Initialize(definition.MaxHP, faction);
            BindHealthBar(this);
            BindDamageFeedback(this);
            _attackFireFeedback = AttackFireFeedbackView.Ensure(gameObject);
            _pathPreview = UnitPathPreview.Ensure(gameObject);
            transform.localScale = Vector3.one;
            StateMachine.ChangeState(UnitState.Idle);
            name = $"UNT_{definition.DisplayName.Replace(" ", string.Empty)}_{GetInstanceID():000}";
        }

        public bool MoveTo(Vector2Int destination)
        {
            CancelCurrentCommand();
            var path = _pathfinder.FindPath(GridPosition, destination, _grid);
            if (path == null || path.Count == 0) return false;
            _pathPreview?.Show(path, _grid);
            _pathGridRevision = _grid.Revision;
            _currentCommand = StartCoroutine(FollowPath(path, destination));
            return true;
        }

        public bool AttackTarget(IDamageable target)
        {
            if (target == null || target.IsDead || ReferenceEquals(target, this)) return false;
            CancelCurrentCommand();
            _currentCommand = StartCoroutine(AttackRoutine(target));
            return true;
        }

        public void TakeDamage(int amount) => Health.TakeDamage(amount);
        public void OnTakenFromPool() { }

        public void OnReturnedToPool()
        {
            CancelCurrentCommand();
            Deselect();
            Health.OnHealthChanged -= HandleHealthChanged;
            Health.OnDied -= HandleDied;
        }

        private IEnumerator FollowPath(List<Vector2Int> path, Vector2Int destination)
        {
            StateMachine.ChangeState(UnitState.Moving);
            var index = 1;
            while (index < path.Count)
            {
                var next = path[index];
                if (_grid.Revision != _pathGridRevision && !_grid.IsWalkable(next))
                {
                    var replanned = _pathfinder.FindPath(GridPosition, destination, _grid);
                    if (replanned == null || replanned.Count <= 1)
                    {
                        FinishCommand(UnitState.Idle);
                        yield break;
                    }

                    path = replanned;
                    _pathPreview?.Show(path, _grid);
                    _pathGridRevision = _grid.Revision;
                    index = 1;
                    continue;
                }

                var target = _grid.CellToWorld(next);
                while ((transform.position - target).sqrMagnitude > 0.0025f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, target, Definition.MoveSpeed * Time.deltaTime);
                    yield return null;
                }
                transform.position = target;
                GridPosition = next;
                index++;
                _pathPreview?.SetFirstVisibleIndex(index);
            }
            FinishCommand(UnitState.Idle);
        }

        private IEnumerator AttackRoutine(IDamageable target)
        {
            var targetComponent = target as Component;
            if (targetComponent == null)
            {
                FinishCommand(UnitState.Idle);
                yield break;
            }

            while (target != null && !target.IsDead)
            {
                var targetArea = GetTargetArea(targetComponent);
                while (!IsWithinAttackRange(targetArea))
                {
                    var path = FindApproachPath(targetArea);
                    if (path == null || path.Count <= 1)
                    {
                        FinishCommand(UnitState.Idle);
                        yield break;
                    }

                    StateMachine.ChangeState(UnitState.Moving);
                    for (var i = 1; i < path.Count; i++)
                    {
                        var point = _grid.CellToWorld(path[i]);
                        while ((transform.position - point).sqrMagnitude > 0.0025f)
                        {
                            if (target == null || target.IsDead)
                            {
                                FinishCommand(UnitState.Idle);
                                yield break;
                            }

                            transform.position = Vector3.MoveTowards(transform.position, point, Definition.MoveSpeed * Time.deltaTime);
                            yield return null;
                        }
                        transform.position = point;
                        GridPosition = path[i];
                    }
                }
                StateMachine.ChangeState(UnitState.Attacking);
                _attackFireFeedback?.PlayTowards(targetComponent.transform.position);
                target.TakeDamage(Definition.AttackDamage);
                yield return new WaitForSeconds(Definition.AttackCooldown);
            }
            FinishCommand(UnitState.Idle);
        }

        private List<Vector2Int> FindApproachPath(TargetArea targetArea)
        {
            List<Vector2Int> bestPath = null;
            var bestScore = float.PositiveInfinity;
            var radius = Mathf.Max(1, Mathf.CeilToInt(Definition.AttackRange / Mathf.Max(0.01f, _grid.CellSize)));
            for (var x = targetArea.Origin.x - radius; x <= targetArea.Origin.x + targetArea.Footprint.x + radius; x++)
            for (var y = targetArea.Origin.y - radius; y <= targetArea.Origin.y + targetArea.Footprint.y + radius; y++)
            {
                var candidate = new Vector2Int(x, y);
                if (!_grid.IsWalkable(candidate) || !IsCellInAttackRange(candidate, targetArea)) continue;

                var path = _pathfinder.FindPath(GridPosition, candidate, _grid);
                if (path == null || path.Count == 0) continue;

                var score = CalculateApproachScore(path, candidate, targetArea);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestPath = path;
                }
            }

            return bestPath;
        }

        private float CalculateApproachScore(IReadOnlyList<Vector2Int> path, Vector2Int candidate, TargetArea targetArea)
        {
            var movementCost = CalculatePathWorldDistance(path);
            var candidatePosition = _grid.CellToWorld(candidate);
            var targetClosestPoint = targetArea.WorldBounds.ClosestPoint(candidatePosition);
            var distanceToTarget = Vector3.Distance(candidatePosition, targetClosestPoint);
            var directDistanceFromUnit = Vector3.Distance(transform.position, candidatePosition);

            return movementCost * 1000f +
                   distanceToTarget * 10f +
                   directDistanceFromUnit;
        }

        private float CalculatePathWorldDistance(IReadOnlyList<Vector2Int> path)
        {
            var distance = 0f;
            for (var i = 1; i < path.Count; i++)
                distance += Vector2Int.Distance(path[i - 1], path[i]) * _grid.CellSize;
            return distance;
        }

        private bool IsWithinAttackRange(TargetArea targetArea)
        {
            var closestPoint = targetArea.WorldBounds.ClosestPoint(transform.position);
            return (transform.position - closestPoint).sqrMagnitude <= Definition.AttackRange * Definition.AttackRange;
        }

        private bool IsCellInAttackRange(Vector2Int cell, TargetArea targetArea)
        {
            var position = _grid.CellToWorld(cell);
            var closestPoint = targetArea.WorldBounds.ClosestPoint(position);
            return (position - closestPoint).sqrMagnitude <= Definition.AttackRange * Definition.AttackRange;
        }

        private TargetArea GetTargetArea(Component targetComponent)
        {
            var building = targetComponent.GetComponentInParent<Building>();
            if (building != null)
                return CreateTargetArea(building.GridPosition, building.Definition.FootprintSize);

            return CreateTargetArea(_grid.WorldToCell(targetComponent.transform.position), Vector2Int.one);
        }

        private TargetArea CreateTargetArea(Vector2Int origin, Vector2Int footprint)
        {
            var center = _grid.AreaToWorldCenter(origin, footprint);
            var size = new Vector3(footprint.x * _grid.CellSize, footprint.y * _grid.CellSize, 1f);
            return new TargetArea(origin, footprint, new Bounds(center, size));
        }

        private readonly struct TargetArea
        {
            public TargetArea(Vector2Int origin, Vector2Int footprint, Bounds worldBounds)
            {
                Origin = origin;
                Footprint = footprint;
                WorldBounds = worldBounds;
            }

            public Vector2Int Origin { get; }
            public Vector2Int Footprint { get; }
            public Bounds WorldBounds { get; }
        }

        private void CancelCurrentCommand()
        {
            if (_currentCommand != null) StopCoroutine(_currentCommand);
            _currentCommand = null;
            _pathPreview?.Clear();
            if (IsDead) return;
            UpdateGridPositionFromCurrentWorldPosition();
            StateMachine.ChangeState(UnitState.Idle);
        }

        private void FinishCommand(UnitState state)
        {
            _pathPreview?.Clear();
            if (!IsDead && SmoothSnapToNearestWalkableCell(state)) return;
            StateMachine.ChangeState(state);
            _currentCommand = null;
        }

        private bool SmoothSnapToNearestWalkableCell(UnitState finalState)
        {
            if (_grid == null) return false;

            var nearest = ResolveNearestWalkableCell();
            var destination = _grid.CellToWorld(nearest);

            GridPosition = nearest;
            if ((transform.position - destination).sqrMagnitude <= 0.0004f)
            {
                transform.position = destination;
                return false;
            }

            _currentCommand = StartCoroutine(SmoothSnapRoutine(destination, finalState));
            return true;
        }

        private IEnumerator SmoothSnapRoutine(Vector3 destination, UnitState finalState)
        {
            StateMachine.ChangeState(UnitState.Moving);
            var start = transform.position;
            var elapsed = 0f;

            while (elapsed < SnapDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / SnapDuration);
                t = t * t * (3f - 2f * t);
                transform.position = Vector3.Lerp(start, destination, t);
                yield return null;
            }

            transform.position = destination;
            StateMachine.ChangeState(finalState);
            _currentCommand = null;
        }

        private void UpdateGridPositionFromCurrentWorldPosition()
        {
            if (_grid == null) return;
            GridPosition = ResolveNearestWalkableCell();
        }

        private Vector2Int ResolveNearestWalkableCell()
        {
            var nearest = _grid.WorldToCell(transform.position);
            if (_grid.Contains(nearest) && _grid.IsWalkable(nearest)) return nearest;
            return FindNearestWalkableCell(GridPosition);
        }

        private Vector2Int FindNearestWalkableCell(Vector2Int fallback)
        {
            if (_grid.IsWalkable(fallback)) return fallback;

            var center = _grid.WorldToCell(transform.position);
            for (var radius = 1; radius <= 4; radius++)
            for (var x = -radius; x <= radius; x++)
            for (var y = -radius; y <= radius; y++)
            {
                if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius) continue;
                var candidate = center + new Vector2Int(x, y);
                if (_grid.IsWalkable(candidate)) return candidate;
            }

            return fallback;
        }

        private void HandleHealthChanged(int current, int max) => _bus?.Publish(new EntityHealthChanged(this, current, max));

        private void HandleDied()
        {
            CancelCurrentCommand();
            StateMachine.ChangeState(UnitState.Dead);
            _bus?.Publish(new EntityDied(this));
            if (IsSelected) _bus?.Publish(new SelectionCleared());
            _returnToPool?.Invoke(this);
        }
    }
}
