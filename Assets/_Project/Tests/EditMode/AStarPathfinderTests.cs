using NUnit.Framework;
using Panteon.Gameplay.Buildings;
using Panteon.Gameplay.Combat;
using Panteon.Gameplay.Grid;
using Panteon.Gameplay.Pathfinding;
using UnityEngine;

namespace Panteon.Tests
{
    public sealed class AStarPathfinderTests
    {
        [Test]
        public void FindsPathAroundObstacle_AndRejectsUnreachableGoal()
        {
            var gridObject = new GameObject("Grid");
            var grid = gridObject.AddComponent<GridManager>();
            grid.Initialize(new Vector2Int(5, 5));
            var obstacleObject = new GameObject("Obstacle");
            obstacleObject.AddComponent<HealthComponent>();
            var obstacle = obstacleObject.AddComponent<Building>();
            grid.Occupy(new Vector2Int(2, 1), new Vector2Int(1, 3), obstacle);
            var pathfinder = new AStarPathfinder();
            var path = pathfinder.FindPath(new Vector2Int(0, 2), new Vector2Int(4, 2), grid);
            Assert.That(path, Is.Not.Null);
            Assert.That(path.Contains(new Vector2Int(2, 2)), Is.False);
            grid.Occupy(new Vector2Int(4, 2), Vector2Int.one, obstacle);
            Assert.That(pathfinder.FindPath(Vector2Int.zero, new Vector2Int(4, 2), grid), Is.Null);
            Object.DestroyImmediate(obstacleObject);
            Object.DestroyImmediate(gridObject);
        }

        [Test]
        public void FindsShortestDiagonalPath_WhenGridIsOpen()
        {
            var gridObject = new GameObject("Grid");
            var grid = gridObject.AddComponent<GridManager>();
            grid.Initialize(new Vector2Int(4, 4));

            var path = new AStarPathfinder().FindPath(Vector2Int.zero, new Vector2Int(3, 3), grid);

            Assert.That(path, Is.EqualTo(new[]
            {
                new Vector2Int(0, 0),
                new Vector2Int(1, 1),
                new Vector2Int(2, 2),
                new Vector2Int(3, 3)
            }));
            Object.DestroyImmediate(gridObject);
        }

        [Test]
        public void BlocksDiagonalCornerCutting()
        {
            var gridObject = new GameObject("Grid");
            var grid = gridObject.AddComponent<GridManager>();
            grid.Initialize(new Vector2Int(2, 2));
            var obstacleA = CreateObstacle("ObstacleA");
            var obstacleB = CreateObstacle("ObstacleB");
            grid.Occupy(new Vector2Int(1, 0), Vector2Int.one, obstacleA);
            grid.Occupy(new Vector2Int(0, 1), Vector2Int.one, obstacleB);

            var path = new AStarPathfinder().FindPath(Vector2Int.zero, new Vector2Int(1, 1), grid);

            Assert.That(path, Is.Null);
            Object.DestroyImmediate(obstacleA.gameObject);
            Object.DestroyImmediate(obstacleB.gameObject);
            Object.DestroyImmediate(gridObject);
        }

        [Test]
        public void RejectsOccupiedStartCell()
        {
            var gridObject = new GameObject("Grid");
            var grid = gridObject.AddComponent<GridManager>();
            grid.Initialize(new Vector2Int(3, 3));
            var obstacle = CreateObstacle("StartObstacle");
            grid.Occupy(Vector2Int.zero, Vector2Int.one, obstacle);

            var path = new AStarPathfinder().FindPath(Vector2Int.zero, new Vector2Int(2, 2), grid);

            Assert.That(path, Is.Null);
            Object.DestroyImmediate(obstacle.gameObject);
            Object.DestroyImmediate(gridObject);
        }

        [Test]
        public void AvoidsEveryCellInFourByFourBuildingFootprint()
        {
            var gridObject = new GameObject("Grid");
            var grid = gridObject.AddComponent<GridManager>();
            grid.Initialize(new Vector2Int(8, 8));
            var obstacle = CreateObstacle("BarracksObstacle");
            var origin = new Vector2Int(2, 2);
            var footprint = new Vector2Int(4, 4);
            grid.Occupy(origin, footprint, obstacle);

            var path = new AStarPathfinder().FindPath(new Vector2Int(0, 4), new Vector2Int(7, 4), grid);

            Assert.That(path, Is.Not.Null);
            foreach (var cell in path)
                Assert.That(cell.x < origin.x || cell.x >= origin.x + footprint.x ||
                            cell.y < origin.y || cell.y >= origin.y + footprint.y, Is.True);
            Object.DestroyImmediate(obstacle.gameObject);
            Object.DestroyImmediate(gridObject);
        }

        [Test]
        public void BinaryHeapRemovesLowestFCost_WithHCostTieBreak()
        {
            var heap = new BinaryMinHeap<PathNode>(3);
            var high = new PathNode { Cell = Vector2Int.zero, GCost = 10, HCost = 10 };
            var lowTie = new PathNode { Cell = Vector2Int.right, GCost = 15, HCost = 2 };
            var highTie = new PathNode { Cell = Vector2Int.up, GCost = 10, HCost = 7 };

            heap.Add(high);
            heap.Add(highTie);
            heap.Add(lowTie);

            Assert.That(heap.RemoveFirst(), Is.SameAs(lowTie));
            Assert.That(heap.RemoveFirst(), Is.SameAs(highTie));
            Assert.That(heap.RemoveFirst(), Is.SameAs(high));
        }

        private static Building CreateObstacle(string name)
        {
            var obstacleObject = new GameObject(name);
            obstacleObject.AddComponent<HealthComponent>();
            return obstacleObject.AddComponent<Building>();
        }
    }
}
