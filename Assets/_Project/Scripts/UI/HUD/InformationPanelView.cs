using System;
using System.Collections.Generic;
using Panteon.Core;
using Panteon.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Panteon.UI
{
    internal sealed class InformationPanelView
    {
        private readonly HudViewFactory _factory;
        private readonly RectTransform _panel;
        private readonly Image _headerPlate;
        private readonly Text _header;
        private readonly Text _title;
        private readonly Text _subtitle;
        private readonly Image _preview;
        private readonly Text _hp;
        private readonly RectTransform _productionRoot;
        private readonly Text _productionTitle;
        private readonly RectTransform _productionContent;
        private readonly ScrollRect _productionScroll;
        private readonly List<HudButtonView> _buttons = new List<HudButtonView>();
        private int _visibleProductionButtonCount;
        private readonly RectTransform _unitListRoot;
        private readonly RectTransform _unitListViewport;
        private readonly RectTransform _unitListContent;
        private readonly ScrollRect _unitListScroll;
        private readonly List<SelectedUnitRowView> _unitRows = new List<SelectedUnitRowView>();
        private readonly RectTransform _unitDetailRoot;
        private readonly Image _unitPortraitFrame;
        private readonly RectTransform _unitPortraitMask;
        private readonly Image _unitPortraitIcon;
        private readonly Image _unitNamePlate;
        private readonly Text _unitName;
        private readonly Image _unitDescriptionPlate;
        private readonly Text _unitDescription;
        private readonly Text _unitHealthStat;
        private readonly Text _unitAttackStat;
        private readonly RectTransform _buildingDetailRoot;
        private readonly Image _buildingPortraitFrame;
        private readonly Image _buildingPortrait;
        private readonly Image _buildingNamePlate;
        private readonly Text _buildingName;
        private readonly Image _buildingDescriptionPlate;
        private readonly Text _buildingDescription;
        private readonly Text _buildingHealthStat;
        private readonly Image _buildingSeparator;
        private readonly Image _productionTitlePlate;
        private int _visibleUnitCount;
        private Rect _lastRect;
        private Rect _buildingPortraitContentRect = new Rect(0f, 0f, 1f, 1f);

        public InformationPanelView(RuntimeHudView hud, HudViewFactory factory)
        {
            _factory = factory;
            var bindings = hud.InformationBindings != null
                ? hud.InformationBindings
                : throw new InvalidOperationException("RuntimeHUD information bindings are missing.");
            bindings.ValidateReferences();
            _panel = bindings.Root;
            _headerPlate = bindings.HeaderPlate;
            _header = bindings.Header;
            _title = bindings.Title;
            _subtitle = bindings.Subtitle;
            _preview = bindings.Preview;
            _hp = bindings.Hp;
            _productionRoot = bindings.ProductionRoot;
            _productionTitlePlate = bindings.ProductionTitlePlate;
            _productionTitle = bindings.ProductionTitle;
            _productionContent = bindings.ProductionContent;
            _productionScroll = bindings.ProductionScroll;
            _unitListRoot = bindings.UnitListRoot;
            _unitListScroll = bindings.UnitListScroll;
            _unitListViewport = bindings.UnitListViewport;
            _unitListContent = bindings.UnitListContent;
            _unitDetailRoot = bindings.UnitDetailRoot;
            _unitPortraitFrame = bindings.UnitPortraitFrame;
            _unitPortraitMask = bindings.UnitPortraitMask;
            _unitPortraitIcon = bindings.UnitPortraitIcon;
            _unitNamePlate = bindings.UnitNamePlate;
            _unitName = bindings.UnitName;
            _unitDescriptionPlate = bindings.UnitDescriptionPlate;
            _unitDescription = bindings.UnitDescription;
            _unitHealthStat = bindings.UnitHealthStat;
            _unitAttackStat = bindings.UnitAttackStat;
            _buildingDetailRoot = bindings.BuildingDetailRoot;
            _buildingPortraitFrame = bindings.BuildingPortraitFrame;
            _buildingPortrait = bindings.BuildingPortrait;
            _buildingNamePlate = bindings.BuildingNamePlate;
            _buildingName = bindings.BuildingName;
            _buildingDescriptionPlate = bindings.BuildingDescriptionPlate;
            _buildingDescription = bindings.BuildingDescription;
            _buildingHealthStat = bindings.BuildingHealthStat;
            _buildingSeparator = bindings.BuildingSeparator;
            _buttons.AddRange(bindings.ProductionCards);
            _unitRows.AddRange(bindings.UnitRows);
            ShowEmpty();
        }

        public void ShowEmpty()
        {
            HideProductionButtons();
            _title.text = string.Empty;
            _subtitle.text = string.Empty;
            _hp.text = string.Empty;
            _title.gameObject.SetActive(false);
            _subtitle.gameObject.SetActive(false);
            _preview.gameObject.SetActive(false);
            _hp.gameObject.SetActive(false);
            _productionRoot.gameObject.SetActive(false);
            HideUnitRows();
            _unitListRoot.gameObject.SetActive(false);
            _unitDetailRoot.gameObject.SetActive(false);
            _buildingDetailRoot.gameObject.SetActive(false);
        }

        public void Show(IDamageable selected, IProductionBuilding building, Action<IProductionBuilding, UnitDefinitionSO> requestProduction)
        {
            if (selected == null)
            {
                ShowEmpty();
                return;
            }

            if (building == null && selected is IUnitPresentation unitPresentation)
            {
                ShowUnit(unitPresentation);
                return;
            }

            if (building != null)
            {
                ShowBuilding(building, requestProduction);
                return;
            }

            HideProductionButtons();
            HideUnitRows();
            _unitListRoot.gameObject.SetActive(false);
            _unitDetailRoot.gameObject.SetActive(false);
            _buildingDetailRoot.gameObject.SetActive(false);
            _title.gameObject.SetActive(true);
            _subtitle.gameObject.SetActive(true);
            _preview.gameObject.SetActive(true);
            _hp.gameObject.SetActive(true);
            var presentation = selected as IEntityPresentation;
            var displayName = building != null ? building.DisplayName : presentation != null ? presentation.DisplayName : selected.GetType().Name;
            _title.text = displayName.ToUpperInvariant();
            _subtitle.text = building != null ? "BUILDING" : "UNIT";
            _preview.sprite = presentation != null && presentation.Icon != null ? presentation.Icon : _factory.SolidSprite;
            _preview.color = presentation != null && presentation.Icon != null ? Color.white : HudViewFactory.CardColor;
            _hp.text = $"HP  {selected.CurrentHP} / {selected.MaxHP}";

            var canProduce = building != null && building.CanProduce;
            _productionRoot.gameObject.SetActive(canProduce);
            if (canProduce)
                BindProductionButtons(building, requestProduction);
            RefreshDynamicLayout();
        }

        public void ShowUnits(IReadOnlyList<IEntityPresentation> units)
        {
            if (units == null || units.Count == 0)
            {
                ShowEmpty();
                return;
            }

            HideProductionButtons();
            _unitDetailRoot.gameObject.SetActive(false);
            _buildingDetailRoot.gameObject.SetActive(false);
            _title.gameObject.SetActive(true);
            _subtitle.gameObject.SetActive(true);
            _title.text = "SELECTED UNITS";
            _subtitle.text = $"{units.Count} SOLDIERS";
            _preview.gameObject.SetActive(false);
            _hp.gameObject.SetActive(false);
            _productionRoot.gameObject.SetActive(false);
            _unitListRoot.gameObject.SetActive(true);
            EnsureUnitRowPool(units.Count);
            _visibleUnitCount = units.Count;

            for (var i = 0; i < _unitRows.Count; i++)
            {
                var active = i < units.Count;
                _unitRows[i].Root.gameObject.SetActive(active);
                if (!active) continue;
                var unit = units[i];
                _unitRows[i].Name.text = unit.DisplayName;
                _factory.BindDisplaySprite(_unitRows[i].Icon, unit.Icon, unit.IconContentRect);
            }

            _unitListContent.anchoredPosition = Vector2.zero;
            _unitListScroll.verticalNormalizedPosition = 1f;
            RefreshDynamicLayout();
        }

        private void ShowUnit(IUnitPresentation unit)
        {
            HideProductionButtons();
            HideUnitRows();
            _unitListRoot.gameObject.SetActive(false);
            _productionRoot.gameObject.SetActive(false);
            _title.gameObject.SetActive(false);
            _subtitle.gameObject.SetActive(false);
            _preview.gameObject.SetActive(false);
            _hp.gameObject.SetActive(false);
            _unitDetailRoot.gameObject.SetActive(true);
            _buildingDetailRoot.gameObject.SetActive(false);

            _factory.BindDisplaySprite(_unitPortraitIcon, unit.Icon, unit.IconContentRect);
            _unitName.text = unit.DisplayName;
            _unitDescription.text = unit.Description;
            _unitHealthStat.text = $"♥  {unit.CurrentHP}";
            _unitAttackStat.text = $"ATK  {unit.AttackDamage}";
            RefreshDynamicLayout();
        }

        private void ShowBuilding(IProductionBuilding building,
            Action<IProductionBuilding, UnitDefinitionSO> requestProduction)
        {
            HideProductionButtons();
            HideUnitRows();
            _unitListRoot.gameObject.SetActive(false);
            _unitDetailRoot.gameObject.SetActive(false);
            _title.gameObject.SetActive(false);
            _subtitle.gameObject.SetActive(false);
            _preview.gameObject.SetActive(false);
            _hp.gameObject.SetActive(false);
            _buildingDetailRoot.gameObject.SetActive(true);

            _buildingPortrait.sprite = building.Icon != null ? building.Icon : _factory.SolidSprite;
            _buildingPortraitContentRect = building.Icon != null
                ? building.IconContentRect
                : new Rect(0f, 0f, 1f, 1f);
            _buildingPortrait.color = building.Icon != null ? Color.white : HudViewFactory.MutedTextColor;
            _buildingName.text = building.DisplayName;
            _buildingDescription.text = building.Description;
            _buildingHealthStat.text = $"\u2665  {building.CurrentHP}";

            _productionRoot.gameObject.SetActive(building.CanProduce);
            if (building.CanProduce) BindProductionButtons(building, requestProduction);
            RefreshDynamicLayout();
        }

        private void RefreshDynamicLayout()
        {
            if (_buildingDetailRoot.gameObject.activeSelf && _buildingPortrait.sprite != null)
            {
                var frameRect = _buildingPortraitFrame.rectTransform.rect;
                var inset = _factory.Scaled(6f);
                var bounds = new Rect(
                    inset,
                    inset,
                    Mathf.Max(1f, frameRect.width - inset * 2f),
                    Mathf.Max(1f, frameRect.height - inset * 2f));
                HudViewFactory.SetRect(
                    _buildingPortrait.rectTransform,
                    HudViewFactory.FitSpriteContentByHeight(
                        _buildingPortrait.sprite,
                        _buildingPortraitContentRect,
                        bounds));
            }

            var listWidth = _unitListViewport.rect.width;
            var listHeight = _unitListViewport.rect.height;
            var rowHeight = _factory.Scaled(54f);
            var spacing = _factory.Scaled(6f);
            var padding = _factory.Scaled(8f);
            var contentHeight = padding * 2f + Mathf.Max(0, _visibleUnitCount) * (rowHeight + spacing) -
                                (_visibleUnitCount > 0 ? spacing : 0f);
            _unitListContent.sizeDelta = new Vector2(0f, Mathf.Max(listHeight, contentHeight));
            _unitListScroll.vertical = contentHeight > listHeight;
            for (var i = 0; i < _visibleUnitCount; i++)
            {
                var row = _unitRows[i];
                HudViewFactory.SetRect(row.Root, new Rect(
                    padding,
                    padding + i * (rowHeight + spacing),
                    listWidth - padding * 2f,
                    rowHeight));
                HudViewFactory.SetRect(row.Icon.rectTransform, new Rect(
                    _factory.Scaled(7f), _factory.Scaled(7f), _factory.Scaled(40f), _factory.Scaled(40f)));
                HudViewFactory.SetRect(row.Name.rectTransform, new Rect(
                    _factory.Scaled(56f), 0f,
                    listWidth - padding * 2f - _factory.Scaled(64f), rowHeight));
            }

            var productionWidth = _productionRoot.rect.width;
            var productionHeight = _productionContent.rect.height;
            LayoutProductionButtons(productionWidth, productionHeight);
        }

        public void Layout(Rect rect)
        {
            _lastRect = rect;
            HudViewFactory.SetRect(_panel, rect);
            var headerRect = _factory.ScaledRect(12f, 12f, rect.width - _factory.Scaled(24f), 28f);
            HudViewFactory.SetRect(_headerPlate.rectTransform, headerRect);
            HudViewFactory.SetRect(_header.rectTransform, new Rect(0f, 0f, headerRect.width, headerRect.height));
            HudViewFactory.SetRect(_title.rectTransform, _factory.ScaledRect(16f, 96f, rect.width - _factory.Scaled(32f), 26f));
            HudViewFactory.SetRect(_subtitle.rectTransform, _factory.ScaledRect(16f, 122f, rect.width - _factory.Scaled(32f), 20f));
            HudViewFactory.SetRect(_preview.rectTransform, _factory.ScaledRect(18f, 158f, rect.width - _factory.Scaled(36f), 72f));
            HudViewFactory.SetRect(_hp.rectTransform, _factory.ScaledRect(18f, 240f, rect.width - _factory.Scaled(36f), 24f));
            var detailWidth = rect.width - _factory.Scaled(32f);
            HudViewFactory.SetRect(_unitDetailRoot, new Rect(_factory.Scaled(16f), _factory.Scaled(70f), detailWidth, rect.height - _factory.Scaled(88f)));
            var portraitSize = Mathf.Min(detailWidth - _factory.Scaled(28f), _factory.Scaled(116f));
            var portraitX = (detailWidth - portraitSize) * 0.5f;
            HudViewFactory.SetRect(_unitPortraitFrame.rectTransform, new Rect(portraitX, 0f, portraitSize, portraitSize));
            var portraitInset = _factory.Scaled(11f);
            HudViewFactory.SetRect(_unitPortraitMask, new Rect(portraitX + portraitInset, portraitInset, portraitSize - portraitInset * 2f, portraitSize - portraitInset * 2f));
            HudViewFactory.SetRect(_unitPortraitIcon.rectTransform, new Rect(0f, 0f, portraitSize - portraitInset * 2f, portraitSize - portraitInset * 2f));
            var nameY = portraitSize + _factory.Scaled(14f);
            HudViewFactory.SetRect(_unitNamePlate.rectTransform, new Rect(_factory.Scaled(4f), nameY, detailWidth - _factory.Scaled(8f), _factory.Scaled(28f)));
            HudViewFactory.SetRect(_unitName.rectTransform, new Rect(0f, 0f, detailWidth - _factory.Scaled(8f), _factory.Scaled(28f)));
            var descriptionY = nameY + _factory.Scaled(36f);
            HudViewFactory.SetRect(_unitDescriptionPlate.rectTransform, new Rect(0f, descriptionY, detailWidth, _factory.Scaled(54f)));
            HudViewFactory.SetRect(_unitDescription.rectTransform, new Rect(_factory.Scaled(8f), _factory.Scaled(7f), detailWidth - _factory.Scaled(16f), _factory.Scaled(40f)));
            var statsY = descriptionY + _factory.Scaled(68f);
            HudViewFactory.SetRect(_unitHealthStat.rectTransform, new Rect(_factory.Scaled(8f), statsY, detailWidth * 0.46f, _factory.Scaled(30f)));
            HudViewFactory.SetRect(_unitAttackStat.rectTransform, new Rect(detailWidth * 0.54f, statsY, detailWidth * 0.46f - _factory.Scaled(8f), _factory.Scaled(30f)));

            HudViewFactory.SetRect(_buildingDetailRoot, new Rect(_factory.Scaled(16f), _factory.Scaled(70f), detailWidth, rect.height - _factory.Scaled(88f)));
            var buildingPreviewHeight = Mathf.Min(detailWidth * 0.94f, _factory.Scaled(154f));
            HudViewFactory.SetRect(_buildingPortraitFrame.rectTransform, new Rect(_factory.Scaled(4f), 0f, detailWidth - _factory.Scaled(8f), buildingPreviewHeight));
            var buildingPortraitBounds = new Rect(_factory.Scaled(6f), _factory.Scaled(6f),
                detailWidth - _factory.Scaled(20f), buildingPreviewHeight - _factory.Scaled(12f));
            var fittedBuildingPortrait = HudViewFactory.FitSpriteContentByHeight(
                _buildingPortrait.sprite, _buildingPortraitContentRect, buildingPortraitBounds);
            HudViewFactory.SetRect(_buildingPortrait.rectTransform, fittedBuildingPortrait);
            var buildingNameY = buildingPreviewHeight + _factory.Scaled(10f);
            HudViewFactory.SetRect(_buildingNamePlate.rectTransform, new Rect(_factory.Scaled(4f), buildingNameY, detailWidth - _factory.Scaled(8f), _factory.Scaled(28f)));
            HudViewFactory.SetRect(_buildingName.rectTransform, new Rect(0f, 0f, detailWidth - _factory.Scaled(8f), _factory.Scaled(28f)));
            var buildingDescriptionY = buildingNameY + _factory.Scaled(36f);
            HudViewFactory.SetRect(_buildingDescriptionPlate.rectTransform, new Rect(0f, buildingDescriptionY, detailWidth, _factory.Scaled(58f)));
            HudViewFactory.SetRect(_buildingDescription.rectTransform, new Rect(_factory.Scaled(8f), _factory.Scaled(7f), detailWidth - _factory.Scaled(16f), _factory.Scaled(44f)));
            var buildingHealthY = buildingDescriptionY + _factory.Scaled(68f);
            HudViewFactory.SetRect(_buildingHealthStat.rectTransform, new Rect(0f, buildingHealthY, detailWidth, _factory.Scaled(30f)));
            var separatorY = buildingHealthY + _factory.Scaled(38f);
            HudViewFactory.SetRect(_buildingSeparator.rectTransform, new Rect(0f, separatorY, detailWidth, Mathf.Max(1f, _factory.Scaled(2f))));

            var productionY = _factory.Scaled(70f) + separatorY + _factory.Scaled(14f);
            HudViewFactory.SetRect(_productionRoot, new Rect(_factory.Scaled(16f), productionY, detailWidth, _factory.Scaled(116f)));
            HudViewFactory.SetRect(_productionTitlePlate.rectTransform, new Rect(0f, 0f, detailWidth, _factory.Scaled(26f)));
            HudViewFactory.SetRect(_productionTitle.rectTransform, new Rect(0f, 0f, detailWidth, _factory.Scaled(26f)));
            HudViewFactory.SetRect(_productionContent, new Rect(0f, _factory.Scaled(34f), detailWidth, _factory.Scaled(80f)));

            var listWidth = rect.width - _factory.Scaled(32f);
            var listHeight = Mathf.Max(_factory.Scaled(80f), rect.height - _factory.Scaled(176f));
            HudViewFactory.SetRect(_unitListRoot, new Rect(_factory.Scaled(16f), _factory.Scaled(158f), listWidth, listHeight));
            HudViewFactory.SetRect(_unitListViewport, new Rect(0f, 0f, listWidth, listHeight));
            var rowHeight = _factory.Scaled(54f);
            var spacing = _factory.Scaled(6f);
            var padding = _factory.Scaled(8f);
            var contentHeight = padding * 2f + Mathf.Max(0, _visibleUnitCount) * (rowHeight + spacing) -
                                (_visibleUnitCount > 0 ? spacing : 0f);
            _unitListContent.sizeDelta = new Vector2(0f, Mathf.Max(listHeight, contentHeight));
            _unitListScroll.vertical = contentHeight > listHeight;
            for (var i = 0; i < _visibleUnitCount; i++)
            {
                var row = _unitRows[i];
                HudViewFactory.SetRect(row.Root, new Rect(padding, padding + i * (rowHeight + spacing), listWidth - padding * 2f, rowHeight));
                HudViewFactory.SetRect(row.Icon.rectTransform, new Rect(_factory.Scaled(7f), _factory.Scaled(7f), _factory.Scaled(40f), _factory.Scaled(40f)));
                HudViewFactory.SetRect(row.Name.rectTransform, new Rect(_factory.Scaled(56f), 0f, listWidth - padding * 2f - _factory.Scaled(64f), rowHeight));
            }

            LayoutProductionButtons(detailWidth, _factory.Scaled(78f));
        }

        public void PrewarmProductionButtons(int capacity)
        {
            EnsureProductionButtonPool(Mathf.Max(0, capacity));
            HideProductionButtons();
        }

        public void PrewarmUnitRows(int capacity)
        {
            EnsureUnitRowPool(Mathf.Max(0, capacity));
            HideUnitRows();
        }

        private void BindProductionButtons(IProductionBuilding building,
            Action<IProductionBuilding, UnitDefinitionSO> requestProduction)
        {
            var definitions = building.Producibles;
            EnsureProductionButtonPool(definitions != null ? definitions.Count : 0);
            _visibleProductionButtonCount = 0;

            if (definitions != null)
            {
                foreach (var definition in definitions)
                {
                    if (definition == null) continue;
                    var button = _buttons[_visibleProductionButtonCount++];
                    button.gameObject.SetActive(true);
                    button.name = definition.DisplayName.Replace(" ", string.Empty);
                    button.Button.onClick.RemoveAllListeners();
                    var capturedDefinition = definition;
                    var capturedBuilding = building;
                    button.Button.onClick.AddListener(() => requestProduction?.Invoke(capturedBuilding, capturedDefinition));

                    var icon = button.Icon;
                    if (icon != null)
                    {
                        _factory.BindDisplaySprite(
                            icon,
                            definition.Icon,
                            definition.IconContentRect,
                            2f * definition.ProductionIconScale);
                    }
                    button.Label.text = definition.DisplayName;
                }
            }

            for (var i = _visibleProductionButtonCount; i < _buttons.Count; i++)
                _buttons[i].gameObject.SetActive(false);

            _productionContent.anchoredPosition = new Vector2(0f, _productionContent.anchoredPosition.y);
            _productionScroll.horizontalNormalizedPosition = 0f;
        }

        private void EnsureProductionButtonPool(int requiredCount)
        {
            if (requiredCount <= _buttons.Count) return;
            if (_buttons.Count == 0)
                throw new InvalidOperationException("RuntimeHUD requires at least one authored unit production card.");

            var template = _buttons[0];
            while (_buttons.Count < requiredCount)
            {
                var button = UnityEngine.Object.Instantiate(template, _productionContent);
                button.name = $"ProductionCard{_buttons.Count + 1}";
                button.Button.onClick.RemoveAllListeners();
                button.gameObject.SetActive(false);
                _buttons.Add(button);
            }
        }

        private void LayoutProductionButtons(float viewportWidth, float buttonHeight)
        {
            if (viewportWidth <= 0f) return;

            const int visibleCardCapacity = 4;
            var spacing = _factory.Scaled(2f);
            var buttonWidth = (viewportWidth - spacing * (visibleCardCapacity - 1)) / visibleCardCapacity;
            var contentWidth = Mathf.Max(
                viewportWidth,
                _visibleProductionButtonCount * buttonWidth +
                Mathf.Max(0, _visibleProductionButtonCount - 1) * spacing);

            _productionContent.sizeDelta = new Vector2(contentWidth, _productionContent.sizeDelta.y);
            _productionScroll.horizontal = contentWidth > viewportWidth + 0.5f;

            for (var i = 0; i < _visibleProductionButtonCount; i++)
            {
                HudViewFactory.SetRect(_buttons[i].Root,
                    new Rect(i * (buttonWidth + spacing), 0f, buttonWidth, buttonHeight));
                _factory.LayoutButton(_buttons[i]);
            }
        }

        private void HideProductionButtons()
        {
            _visibleProductionButtonCount = 0;
            foreach (var button in _buttons)
            {
                if (button == null) continue;
                button.Button.onClick.RemoveAllListeners();
                button.gameObject.SetActive(false);
            }
        }

        private void EnsureUnitRowPool(int requiredCount)
        {
            if (requiredCount <= _unitRows.Count) return;
            throw new InvalidOperationException(
                $"RuntimeHUD has {_unitRows.Count} selected-unit rows but {requiredCount} are required. " +
                "Add more selected-unit rows to RuntimeHUD.prefab.");
        }

        private void HideUnitRows()
        {
            _visibleUnitCount = 0;
            foreach (var row in _unitRows)
                if (row.Root != null) row.Root.gameObject.SetActive(false);
        }

    }
}
