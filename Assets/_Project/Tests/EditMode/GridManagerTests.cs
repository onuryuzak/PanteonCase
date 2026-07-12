using NUnit.Framework;
using Panteon.Gameplay.Buildings;
using Panteon.Gameplay.Combat;
using Panteon.Gameplay.Grid;
using UnityEngine;

namespace Panteon.Tests
{
    public sealed class GridManagerTests
    {
        private GameObject _gridObject;
        private GridManager _grid;

        [SetUp]
        public void SetUp()
        {
            _gridObject = new GameObject("Grid");
            _grid = _gridObject.AddComponent<GridManager>();
            _grid.Initialize(new Vector2Int(4, 4));
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_gridObject);

        [Test]
        public void IsAreaValid_RejectsOutOfBoundsAndAcceptsAdjacentArea()
        {
            Assert.That(_grid.IsAreaValid(new Vector2Int(3, 3), new Vector2Int(2, 2)), Is.False);
            Assert.That(_grid.IsAreaValid(new Vector2Int(0, 0), new Vector2Int(2, 2)), Is.True);
        }

        [Test]
        public void IsAreaValid_RejectsOverlap()
        {
            var buildingObject = new GameObject("Building");
            buildingObject.AddComponent<HealthComponent>();
            var building = buildingObject.AddComponent<Building>();
            _grid.Occupy(Vector2Int.zero, new Vector2Int(2, 2), building);
            Assert.That(_grid.IsAreaValid(Vector2Int.one, Vector2Int.one), Is.False);
            Assert.That(_grid.IsAreaValid(new Vector2Int(2, 0), new Vector2Int(2, 2)), Is.True);
            Object.DestroyImmediate(buildingObject);
        }

        [Test]
        public void AreaToWorldCenter_ReturnsFootprintCenter()
        {
            Assert.That(_grid.AreaToWorldCenter(Vector2Int.zero, new Vector2Int(4, 4)), Is.EqualTo(new Vector3(2f, 2f, 0f)));
            Assert.That(_grid.AreaToWorldCenter(new Vector2Int(2, 1), new Vector2Int(2, 3)), Is.EqualTo(new Vector3(3f, 2.5f, 0f)));
        }
    }
}
