using NUnit.Framework;
using Panteon.Gameplay.Combat;
using UnityEngine;

namespace Panteon.Tests
{
    public sealed class HealthComponentTests
    {
        [Test]
        public void Damage_ClampsAndFiresDeathOnce()
        {
            var gameObject = new GameObject("Health");
            var health = gameObject.AddComponent<HealthComponent>();
            var deaths = 0;
            health.OnDied += () => deaths++;
            health.Initialize(10);
            health.TakeDamage(20);
            health.TakeDamage(5);
            Assert.That(health.CurrentHP, Is.Zero);
            Assert.That(deaths, Is.EqualTo(1));
            Object.DestroyImmediate(gameObject);
        }
    }
}

