using System;
using Panteon.Core;
using Panteon.Data;
using UnityEngine;

namespace Panteon.Gameplay.Units
{
    // Gameplay-side event handler keeps unit spawning out of UI controllers.
    public sealed class ProductionService : IDisposable
    {
        private readonly UnitFactory _unitFactory;
        private readonly EventBus _bus;

        public ProductionService(UnitFactory unitFactory, EventBus bus)
        {
            _unitFactory = unitFactory ?? throw new ArgumentNullException(nameof(unitFactory));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _bus.Subscribe<ProductionRequested>(OnProductionRequested);
        }

        public void Dispose() => _bus.Unsubscribe<ProductionRequested>(OnProductionRequested);

        private void OnProductionRequested(ProductionRequested message)
        {
            if (message.Source == null || message.Source.IsDead || !message.Source.CanProduce || message.UnitDefinition == null) return;
            Vector2Int spawnCell;
            if (!message.Source.TryGetSpawnCell(out spawnCell)) return;
            _unitFactory.Create(message.UnitDefinition, spawnCell);
        }
    }
}
