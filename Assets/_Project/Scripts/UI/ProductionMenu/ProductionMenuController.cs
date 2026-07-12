using Panteon.Core;
using Panteon.Data;
using UnityEngine;

namespace Panteon.UI.ProductionMenu
{
    public sealed class ProductionMenuController : MonoBehaviour
    {
        [SerializeField] private BuildingCatalogSO _catalog;
        [SerializeField] private InfiniteScrollView _view;
        private IBuildingPlacementService _placement;

        private void Start()
        {
            IBuildingPlacementService placement;
            if (ServiceLocator.Instance.TryGet(out placement)) Configure(placement);
        }

        public void Configure(IBuildingPlacementService placement)
        {
            _placement = placement;
            if (_view != null && _catalog != null) _view.SetData(_catalog.Buildings, OnBuildingChosen);
        }

        private void OnBuildingChosen(BuildingDefinitionSO definition) => _placement?.EnterPlacementMode(definition);
    }
}
