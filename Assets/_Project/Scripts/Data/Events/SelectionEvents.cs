using System.Collections.Generic;

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
}
