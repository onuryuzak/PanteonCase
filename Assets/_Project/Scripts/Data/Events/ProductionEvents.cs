namespace Panteon.Data
{
    public readonly struct ProductionRequested
    {
        public readonly IProductionBuilding Source;
        public readonly UnitDefinitionSO UnitDefinition;

        public ProductionRequested(IProductionBuilding source, UnitDefinitionSO unitDefinition)
        {
            Source = source;
            UnitDefinition = unitDefinition;
        }
    }

    public readonly struct UnitSpawned
    {
        public readonly IEntityPresentation Unit;

        public UnitSpawned(IEntityPresentation unit) => Unit = unit;
    }
}
