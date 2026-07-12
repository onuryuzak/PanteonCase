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
        private readonly Text _header;
        private readonly Text _title;
        private readonly Text _subtitle;
        private readonly Image _preview;
        private readonly Text _hp;
        private readonly RectTransform _productionRoot;
        private readonly Text _productionTitle;
        private readonly RectTransform _productionContent;
        private readonly List<Button> _buttons = new List<Button>();
        private int _visibleProductionButtonCount;
        private readonly RectTransform _unitListRoot;
        private readonly RectTransform _unitListViewport;
        private readonly RectTransform _unitListContent;
        private readonly ScrollRect _unitListScroll;
        private readonly List<UnitListRow> _unitRows = new List<UnitListRow>();
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
        private int _visibleUnitCount;
        private Rect _lastRect;

        public InformationPanelView(RectTransform root, HudViewFactory factory)
        {
            _factory = factory;
            _panel = factory.Panel(root, "InformationPanel", HudViewFactory.PanelColor);
            _header = factory.Text(_panel, "InfoHeader", "Information", 18, FontStyle.Bold, TextAnchor.MiddleLeft, HudViewFactory.TextColor);
            _title = factory.Text(_panel, "InfoTitle", string.Empty, 14, FontStyle.Bold, TextAnchor.MiddleLeft, HudViewFactory.TextColor);
            _subtitle = factory.Text(_panel, "InfoSubtitle", string.Empty, 11, FontStyle.Normal, TextAnchor.MiddleLeft, HudViewFactory.MutedTextColor);
            _preview = factory.Image(_panel, "InfoPreview", HudViewFactory.CardColor);
            _preview.preserveAspect = true;
            _hp = factory.Text(_panel, "InfoHp", string.Empty, 12, FontStyle.Bold, TextAnchor.MiddleLeft, HudViewFactory.TextColor);
            _productionRoot = HudViewFactory.CreateRect("UnitProduction", _panel);
            _productionTitle = factory.Text(_productionRoot, "UnitProductionTitle", "PRODUCTION", 12, FontStyle.Bold, TextAnchor.MiddleLeft, HudViewFactory.TextColor);
            _productionContent = HudViewFactory.CreateRect("UnitProductionContent", _productionRoot);

            _unitListRoot = HudViewFactory.CreateRect("SelectedUnits", _panel);
            var listBackground = _unitListRoot.gameObject.AddComponent<Image>();
            listBackground.sprite = factory.SolidSprite;
            listBackground.color = HudViewFactory.CardColor;
            _unitListScroll = _unitListRoot.gameObject.AddComponent<ScrollRect>();
            _unitListScroll.horizontal = false;
            _unitListScroll.movementType = ScrollRect.MovementType.Clamped;
            _unitListViewport = HudViewFactory.CreateRect("Viewport", _unitListRoot);
            _unitListViewport.gameObject.AddComponent<RectMask2D>();
            _unitListContent = HudViewFactory.CreateRect("Content", _unitListViewport);
            _unitListContent.anchorMin = new Vector2(0f, 1f);
            _unitListContent.anchorMax = new Vector2(1f, 1f);
            _unitListContent.pivot = new Vector2(0f, 1f);
            _unitListScroll.viewport = _unitListViewport;
            _unitListScroll.content = _unitListContent;

            _unitDetailRoot = HudViewFactory.CreateRect("UnitDetails", _panel);
            _unitPortraitFrame = factory.Image(_unitDetailRoot, "PortraitFrame", new Color(0.47f, 0.47f, 0.37f, 1f));
            _unitPortraitFrame.sprite = factory.CircleSprite;
            _unitPortraitMask = HudViewFactory.CreateRect("PortraitMask", _unitDetailRoot);
            var portraitMaskImage = _unitPortraitMask.gameObject.AddComponent<Image>();
            portraitMaskImage.sprite = factory.CircleSprite;
            portraitMaskImage.color = Color.white;
            var portraitMask = _unitPortraitMask.gameObject.AddComponent<Mask>();
            portraitMask.showMaskGraphic = false;
            _unitPortraitIcon = factory.Image(_unitPortraitMask, "Portrait", Color.white);
            _unitPortraitIcon.preserveAspect = true;
            _unitNamePlate = factory.Image(_unitDetailRoot, "NamePlate", new Color(0.46f, 0.44f, 0.34f, 1f));
            _unitName = factory.Text(_unitNamePlate.transform, "Name", string.Empty, 12, FontStyle.Bold, TextAnchor.MiddleCenter, HudViewFactory.TextColor);
            _unitDescriptionPlate = factory.Image(_unitDetailRoot, "DescriptionPlate", new Color(0.07f, 0.08f, 0.1f, 0.48f));
            _unitDescription = factory.Text(_unitDescriptionPlate.transform, "Description", string.Empty, 11, FontStyle.Normal, TextAnchor.UpperLeft, HudViewFactory.TextColor);
            _unitHealthStat = factory.Text(_unitDetailRoot, "HealthStat", string.Empty, 13, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.95f, 0.35f, 0.35f, 1f));
            _unitAttackStat = factory.Text(_unitDetailRoot, "AttackStat", string.Empty, 13, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.75f, 0.76f, 0.68f, 1f));
            ShowEmpty();
        }

        public void ShowEmpty()
        {
            HideProductionButtons();
            _title.text = "NOTHING SELECTED";
            _subtitle.text = "Select a soldier or building.";
            _preview.sprite = _factory.SolidSprite;
            _preview.color = HudViewFactory.CardColor;
            _preview.gameObject.SetActive(true);
            _hp.gameObject.SetActive(true);
            _hp.text = string.Empty;
            _productionRoot.gameObject.SetActive(false);
            HideUnitRows();
            _unitListRoot.gameObject.SetActive(false);
            _unitDetailRoot.gameObject.SetActive(false);
            _title.gameObject.SetActive(true);
            _subtitle.gameObject.SetActive(true);
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

            HideProductionButtons();
            HideUnitRows();
            _unitListRoot.gameObject.SetActive(false);
            _unitDetailRoot.gameObject.SetActive(false);
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
            Layout(_lastRect);
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
                _unitRows[i].Icon.sprite = unit.Icon != null ? unit.Icon : _factory.SolidSprite;
                _unitRows[i].Icon.color = unit.Icon != null ? Color.white : HudViewFactory.MutedTextColor;
            }

            _unitListContent.anchoredPosition = Vector2.zero;
            _unitListScroll.verticalNormalizedPosition = 1f;
            Layout(_lastRect);
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

            _unitPortraitIcon.sprite = unit.Icon != null ? unit.Icon : _factory.SolidSprite;
            _unitPortraitIcon.color = unit.Icon != null ? Color.white : HudViewFactory.MutedTextColor;
            _unitName.text = unit.DisplayName;
            _unitDescription.text = unit.Description;
            _unitHealthStat.text = $"♥  {unit.CurrentHP}";
            _unitAttackStat.text = $"ATK  {unit.AttackDamage}";
            Layout(_lastRect);
        }

        public void Layout(Rect rect)
        {
            _lastRect = rect;
            HudViewFactory.SetRect(_panel, rect);
            HudViewFactory.SetRect(_header.rectTransform, _factory.ScaledRect(16f, 18f, rect.width - _factory.Scaled(32f), 28f));
            HudViewFactory.SetRect(_title.rectTransform, _factory.ScaledRect(16f, 96f, rect.width - _factory.Scaled(32f), 26f));
            HudViewFactory.SetRect(_subtitle.rectTransform, _factory.ScaledRect(16f, 122f, rect.width - _factory.Scaled(32f), 20f));
            HudViewFactory.SetRect(_preview.rectTransform, _factory.ScaledRect(18f, 158f, rect.width - _factory.Scaled(36f), 72f));
            HudViewFactory.SetRect(_hp.rectTransform, _factory.ScaledRect(18f, 240f, rect.width - _factory.Scaled(36f), 24f));
            HudViewFactory.SetRect(_productionRoot, _factory.ScaledRect(16f, 292f, rect.width - _factory.Scaled(32f), rect.height - _factory.Scaled(312f)));
            HudViewFactory.SetRect(_productionTitle.rectTransform, new Rect(0f, 0f, rect.width - _factory.Scaled(32f), _factory.Scaled(24f)));
            HudViewFactory.SetRect(_productionContent, new Rect(0f, _factory.Scaled(34f), rect.width - _factory.Scaled(32f), rect.height - _factory.Scaled(346f)));

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

            for (var i = 0; i < _visibleProductionButtonCount; i++)
            {
                HudViewFactory.SetRect((RectTransform)_buttons[i].transform, new Rect(0f, i * _factory.Scaled(58f), rect.width - _factory.Scaled(32f), _factory.Scaled(50f)));
                _factory.LayoutButton(_buttons[i]);
            }
        }

        public void PrewarmProductionButtons(int capacity)
        {
            EnsureProductionButtonPool(Mathf.Max(0, capacity));
            HideProductionButtons();
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
                    button.onClick.RemoveAllListeners();
                    var capturedDefinition = definition;
                    var capturedBuilding = building;
                    button.onClick.AddListener(() => requestProduction?.Invoke(capturedBuilding, capturedDefinition));

                    var icon = button.transform.Find("Icon")?.GetComponent<Image>();
                    if (icon != null)
                    {
                        icon.sprite = definition.Icon != null ? definition.Icon : _factory.SolidSprite;
                        icon.color = definition.Icon != null ? Color.white : HudViewFactory.MutedTextColor;
                    }
                    var label = button.transform.Find("Label")?.GetComponent<Text>();
                    if (label != null) label.text = definition.DisplayName;
                }
            }

            for (var i = _visibleProductionButtonCount; i < _buttons.Count; i++)
                _buttons[i].gameObject.SetActive(false);
        }

        private void EnsureProductionButtonPool(int requiredCount)
        {
            while (_buttons.Count < requiredCount)
            {
                var button = _factory.Button(_productionContent, string.Empty, null);
                button.gameObject.SetActive(false);
                _buttons.Add(button);
            }
        }

        private void HideProductionButtons()
        {
            _visibleProductionButtonCount = 0;
            foreach (var button in _buttons)
            {
                if (button == null) continue;
                button.onClick.RemoveAllListeners();
                button.gameObject.SetActive(false);
            }
        }

        private void EnsureUnitRowPool(int requiredCount)
        {
            while (_unitRows.Count < requiredCount)
            {
                var root = _factory.Panel(_unitListContent, $"SelectedUnit_{_unitRows.Count + 1}", new Color(0.11f, 0.14f, 0.18f, 1f));
                var icon = _factory.Image(root, "Icon", HudViewFactory.CardColor);
                icon.preserveAspect = true;
                var name = _factory.Text(root, "Name", string.Empty, 11, FontStyle.Bold, TextAnchor.MiddleLeft, HudViewFactory.TextColor);
                _unitRows.Add(new UnitListRow(root, icon, name));
            }
        }

        private void HideUnitRows()
        {
            _visibleUnitCount = 0;
            foreach (var row in _unitRows)
                if (row.Root != null) row.Root.gameObject.SetActive(false);
        }

        private sealed class UnitListRow
        {
            public readonly RectTransform Root;
            public readonly Image Icon;
            public readonly Text Name;

            public UnitListRow(RectTransform root, Image icon, Text name)
            {
                Root = root;
                Icon = icon;
                Name = name;
            }
        }
    }
}
