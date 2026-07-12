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
        private readonly RectTransform _unitListRoot;
        private readonly RectTransform _unitListViewport;
        private readonly RectTransform _unitListContent;
        private readonly ScrollRect _unitListScroll;
        private readonly List<UnitListRow> _unitRows = new List<UnitListRow>();
        private int _visibleUnitCount;
        private Rect _lastRect;

        public InformationPanelView(RectTransform root, HudViewFactory factory)
        {
            _factory = factory;
            _panel = factory.Panel(root, "InformationPanel", HudViewFactory.PanelColor);
            _header = factory.Text(_panel, "InfoHeader", "INFORMATION", 18, FontStyle.Bold, TextAnchor.MiddleLeft, HudViewFactory.TextColor);
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
            ShowEmpty();
        }

        public void ShowEmpty()
        {
            ClearButtons();
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
        }

        public void Show(IDamageable selected, IProductionBuilding building, Action<IProductionBuilding, UnitDefinitionSO> requestProduction)
        {
            if (selected == null)
            {
                ShowEmpty();
                return;
            }

            ClearButtons();
            HideUnitRows();
            _unitListRoot.gameObject.SetActive(false);
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
            {
                foreach (var unit in building.Producibles)
                {
                    if (unit == null) continue;
                    var definition = unit;
                    var owner = building;
                    var button = _factory.Button(_productionContent, definition.DisplayName, definition.Icon);
                    button.onClick.AddListener(() => requestProduction?.Invoke(owner, definition));
                    _buttons.Add(button);
                }
            }
            Layout(_lastRect);
        }

        public void ShowUnits(IReadOnlyList<IEntityPresentation> units)
        {
            if (units == null || units.Count == 0)
            {
                ShowEmpty();
                return;
            }

            ClearButtons();
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

            for (var i = 0; i < _buttons.Count; i++)
            {
                HudViewFactory.SetRect((RectTransform)_buttons[i].transform, new Rect(0f, i * _factory.Scaled(58f), rect.width - _factory.Scaled(32f), _factory.Scaled(50f)));
                _factory.LayoutButton(_buttons[i]);
            }
        }

        private void ClearButtons()
        {
            foreach (var button in _buttons)
                if (button != null) UnityEngine.Object.Destroy(button.gameObject);
            _buttons.Clear();
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
