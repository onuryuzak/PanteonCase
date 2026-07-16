using NUnit.Framework;
using Panteon.Data;
using UnityEngine;

namespace Panteon.Tests
{
    public sealed class UnitAnimationContractTests
    {
        [TestCase(0f, 1f, UnitAnimationDirection.Up)]
        [TestCase(1f, 1f, UnitAnimationDirection.UpRight)]
        [TestCase(1f, 0f, UnitAnimationDirection.Right)]
        [TestCase(-1f, 0f, UnitAnimationDirection.Right)]
        [TestCase(1f, -1f, UnitAnimationDirection.DownRight)]
        [TestCase(0f, -1f, UnitAnimationDirection.Down)]
        public void ResolveDirection_MapsWorldDirectionToSharedAnimationDirection(
            float x, float y, UnitAnimationDirection expected)
        {
            Assert.That(UnitAnimationContract.ResolveDirection(new Vector2(x, y)), Is.EqualTo(expected));
        }

        [Test]
        public void StateHash_UsesStableAndDistinctSemanticPaths()
        {
            var idle = UnitAnimationContract.StateHash("Idle");
            var move = UnitAnimationContract.StateHash("Move");
            Assert.That(idle, Is.Not.Zero);
            Assert.That(move, Is.Not.Zero);
            Assert.That(idle, Is.Not.EqualTo(move));
        }
    }
}
