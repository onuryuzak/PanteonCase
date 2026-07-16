using UnityEngine;

namespace Panteon.UI
{
    /// <summary>
    /// Scene-facing references owned by the authored RuntimeHUD prefab.
    /// Layout and styling belong to the prefab; controllers only bind data and events.
    /// </summary>
    public sealed class RuntimeHudView : MonoBehaviour
    {
        [SerializeField] private RectTransform _boardViewport;
        [SerializeField] private RectTransform _productionPanel;
        [SerializeField] private RectTransform _informationPanel;
        [SerializeField] private ProductionMenuBindings _productionBindings;
        [SerializeField] private InformationPanelBindings _informationBindings;

        public RectTransform ProductionPanel => _productionPanel;
        public RectTransform InformationPanel => _informationPanel;
        public ProductionMenuBindings ProductionBindings => _productionBindings;
        public InformationPanelBindings InformationBindings => _informationBindings;

        public Rect GetBoardScreenRect()
        {
            if (_boardViewport == null) return default;

            var corners = new Vector3[4];
            _boardViewport.GetWorldCorners(corners);
            var xMin = Mathf.Round(corners[0].x);
            var xMax = Mathf.Round(corners[2].x);
            var yMin = Mathf.Round(corners[0].y);
            var yMax = Mathf.Round(corners[2].y);
            return Rect.MinMaxRect(xMin, Screen.height - yMax, xMax, Screen.height - yMin);
        }

        public void Configure(
            RectTransform boardViewport,
            RectTransform productionPanel,
            RectTransform informationPanel)
        {
            _boardViewport = boardViewport;
            _productionPanel = productionPanel;
            _informationPanel = informationPanel;
        }

        private void OnValidate()
        {
            if (_boardViewport == null || _productionPanel == null || _informationPanel == null ||
                _productionBindings == null || _informationBindings == null)
                Debug.LogWarning("RuntimeHUD prefab has missing view references.", this);
        }
    }
}
