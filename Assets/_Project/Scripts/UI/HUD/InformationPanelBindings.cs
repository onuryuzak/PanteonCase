using System;
using UnityEngine;
using UnityEngine.UI;

namespace Panteon.UI
{
    public sealed class InformationPanelBindings : MonoBehaviour
    {
        [Header("General")]
        [SerializeField] private Image _headerPlate;
        [SerializeField] private Text _header;
        [SerializeField] private Text _title;
        [SerializeField] private Text _subtitle;
        [SerializeField] private Image _preview;
        [SerializeField] private Text _hp;

        [Header("Unit production")]
        [SerializeField] private RectTransform _productionRoot;
        [SerializeField] private Image _productionTitlePlate;
        [SerializeField] private Text _productionTitle;
        [SerializeField] private RectTransform _productionContent;
        [SerializeField] private ScrollRect _productionScroll;
        [SerializeField] private HudButtonView[] _productionCards;

        [Header("Selected units")]
        [SerializeField] private RectTransform _unitListRoot;
        [SerializeField] private RectTransform _unitListViewport;
        [SerializeField] private RectTransform _unitListContent;
        [SerializeField] private ScrollRect _unitListScroll;
        [SerializeField] private SelectedUnitRowView[] _unitRows;

        [Header("Unit details")]
        [SerializeField] private RectTransform _unitDetailRoot;
        [SerializeField] private Image _unitPortraitFrame;
        [SerializeField] private RectTransform _unitPortraitMask;
        [SerializeField] private Image _unitPortraitIcon;
        [SerializeField] private Image _unitNamePlate;
        [SerializeField] private Text _unitName;
        [SerializeField] private Image _unitDescriptionPlate;
        [SerializeField] private Text _unitDescription;
        [SerializeField] private Text _unitHealthStat;
        [SerializeField] private Text _unitAttackStat;

        [Header("Building details")]
        [SerializeField] private RectTransform _buildingDetailRoot;
        [SerializeField] private Image _buildingPortraitFrame;
        [SerializeField] private Image _buildingPortrait;
        [SerializeField] private Image _buildingNamePlate;
        [SerializeField] private Text _buildingName;
        [SerializeField] private Image _buildingDescriptionPlate;
        [SerializeField] private Text _buildingDescription;
        [SerializeField] private Text _buildingHealthStat;
        [SerializeField] private Image _buildingSeparator;

        public RectTransform Root => (RectTransform)transform;
        public Image HeaderPlate => _headerPlate;
        public Text Header => _header;
        public Text Title => _title;
        public Text Subtitle => _subtitle;
        public Image Preview => _preview;
        public Text Hp => _hp;
        public RectTransform ProductionRoot => _productionRoot;
        public Image ProductionTitlePlate => _productionTitlePlate;
        public Text ProductionTitle => _productionTitle;
        public RectTransform ProductionContent => _productionContent;
        public ScrollRect ProductionScroll => _productionScroll;
        public HudButtonView[] ProductionCards => _productionCards;
        public RectTransform UnitListRoot => _unitListRoot;
        public RectTransform UnitListViewport => _unitListViewport;
        public RectTransform UnitListContent => _unitListContent;
        public ScrollRect UnitListScroll => _unitListScroll;
        public SelectedUnitRowView[] UnitRows => _unitRows;
        public RectTransform UnitDetailRoot => _unitDetailRoot;
        public Image UnitPortraitFrame => _unitPortraitFrame;
        public RectTransform UnitPortraitMask => _unitPortraitMask;
        public Image UnitPortraitIcon => _unitPortraitIcon;
        public Image UnitNamePlate => _unitNamePlate;
        public Text UnitName => _unitName;
        public Image UnitDescriptionPlate => _unitDescriptionPlate;
        public Text UnitDescription => _unitDescription;
        public Text UnitHealthStat => _unitHealthStat;
        public Text UnitAttackStat => _unitAttackStat;
        public RectTransform BuildingDetailRoot => _buildingDetailRoot;
        public Image BuildingPortraitFrame => _buildingPortraitFrame;
        public Image BuildingPortrait => _buildingPortrait;
        public Image BuildingNamePlate => _buildingNamePlate;
        public Text BuildingName => _buildingName;
        public Image BuildingDescriptionPlate => _buildingDescriptionPlate;
        public Text BuildingDescription => _buildingDescription;
        public Text BuildingHealthStat => _buildingHealthStat;
        public Image BuildingSeparator => _buildingSeparator;

        public void ValidateReferences()
        {
            if (_headerPlate == null || _header == null || _title == null || _subtitle == null ||
                _preview == null || _hp == null || _productionRoot == null || _productionTitlePlate == null ||
                _productionTitle == null || _productionContent == null || _productionScroll == null ||
                _productionCards == null || _productionCards.Length == 0 || _unitListRoot == null ||
                _unitListViewport == null || _unitListContent == null || _unitListScroll == null ||
                _unitRows == null || _unitRows.Length == 0 || _unitDetailRoot == null ||
                _unitPortraitFrame == null || _unitPortraitMask == null || _unitPortraitIcon == null ||
                _unitNamePlate == null || _unitName == null || _unitDescriptionPlate == null ||
                _unitDescription == null || _unitHealthStat == null || _unitAttackStat == null ||
                _buildingDetailRoot == null || _buildingPortraitFrame == null || _buildingPortrait == null ||
                _buildingNamePlate == null || _buildingName == null || _buildingDescriptionPlate == null ||
                _buildingDescription == null || _buildingHealthStat == null || _buildingSeparator == null)
                throw new InvalidOperationException("RuntimeHUD information panel bindings are incomplete.");

            foreach (var card in _productionCards)
            {
                if (card == null) throw new InvalidOperationException("RuntimeHUD unit production card reference is missing.");
                card.ValidateReferences();
            }
            foreach (var row in _unitRows)
            {
                if (row == null) throw new InvalidOperationException("RuntimeHUD selected-unit row reference is missing.");
                row.ValidateReferences();
            }
        }
    }
}
