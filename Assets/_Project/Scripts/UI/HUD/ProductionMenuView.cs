using System;
using System.Collections.Generic;
using Panteon.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Panteon.UI
{
    // Building cards and the downward looping scroll live here.
    internal sealed class ProductionMenuView
    {
        private const int ColumnCount = 2;
        private readonly HudViewFactory _factory;
        private readonly RectTransform _panel;
        private readonly Image _titlePlate;
        private readonly Text _title;
        private readonly Text _subtitle;
        private readonly RectTransform _scroll;
        private readonly RectTransform _viewport;
        private readonly RectTransform _content;
        private readonly ScrollRect _scrollRect;
        private readonly Text _status;
        private readonly List<HudButtonView> _buttons = new List<HudButtonView>();
        private readonly List<BuildingDefinitionSO> _buildings = new List<BuildingDefinitionSO>();
        private Vector2 _buttonSize;
        private float _spacing;
        private float _padding;
        private float _rowStride;
        private bool _suppressScrollCallback;
        private bool _infiniteScrollEnabled;

        public event Action<BuildingDefinitionSO> BuildingRequested;

        public ProductionMenuView(RuntimeHudView hud, HudViewFactory factory)
        {
            _factory = factory;
            var bindings = hud.ProductionBindings != null
                ? hud.ProductionBindings
                : throw new InvalidOperationException("RuntimeHUD production bindings are missing.");
            bindings.ValidateReferences();
            _panel = bindings.Root;
            _titlePlate = bindings.TitlePlate;
            _title = bindings.Title;
            _subtitle = bindings.Subtitle;
            _scroll = bindings.Scroll;
            _scrollRect = bindings.ScrollRect;
            _viewport = bindings.Viewport;
            _content = bindings.Content;
            _status = bindings.Status;
            _buttons.AddRange(bindings.Cards);
            _scrollRect.onValueChanged.AddListener(HandleScrollValueChanged);
        }

        public void Dispose() => _scrollRect.onValueChanged.RemoveListener(HandleScrollValueChanged);

        public void SetBuildings(IEnumerable<BuildingDefinitionSO> buildings)
        {
            _buildings.Clear();
            if (buildings != null)
                foreach (var building in buildings)
                    if (building != null) _buildings.Add(building);
            RebuildButtons();
        }

        public void SetStatus(string value) => _status.text = value;

        public void Layout(Rect rect)
        {
            HudViewFactory.SetRect(_panel, rect);
            var horizontalMargin = _factory.Scaled(12f);
            var titleHeight = _factory.Scaled(28f);
            var scrollY = _factory.Scaled(62f);
            var scrollWidth = rect.width - horizontalMargin * 2f;
            var scrollHeight = rect.height - scrollY - _factory.Scaled(12f);
            HudViewFactory.SetRect(_titlePlate.rectTransform, new Rect(horizontalMargin, _factory.Scaled(12f), scrollWidth, titleHeight));
            HudViewFactory.SetRect(_title.rectTransform, new Rect(0f, 0f, scrollWidth, titleHeight));
            HudViewFactory.SetRect(_scroll, new Rect(horizontalMargin, scrollY, scrollWidth, scrollHeight));
            HudViewFactory.SetRect(_viewport, new Rect(0f, 0f, scrollWidth, scrollHeight));

            _spacing = _factory.Scaled(8f);
            _padding = Mathf.Round(_factory.Scaled(6f));
            var cell = Mathf.Max(_factory.Scaled(42f), (scrollWidth - _padding * 2f - _spacing) * 0.5f);
            _buttonSize = new Vector2(cell, cell + _factory.Scaled(12f));
            ConfigureScrollContent(false);
        }

        public void RefreshContentLayout()
        {
            var scrollWidth = _viewport.rect.width;
            var scrollHeight = _viewport.rect.height;
            var template = _buttons.Count > 0 ? _buttons[0].Root : null;
            _buttonSize = template != null && template.rect.width > 1f && template.rect.height > 1f
                ? template.rect.size
                : new Vector2(140f, 140f);
            _spacing = 20f;
            _padding = Mathf.Max(0f,
                (scrollWidth - ColumnCount * _buttonSize.x - (ColumnCount - 1) * _spacing) * 0.5f);
            ConfigureScrollContent(false);
        }

        private void RebuildButtons()
        {
            EnsureButtonPool(_buildings.Count);
            for (var i = 0; i < _buttons.Count; i++)
            {
                var button = _buttons[i];
                button.Button.onClick.RemoveAllListeners();
                var active = i < _buildings.Count;
                button.gameObject.SetActive(active);
                if (!active) continue;
                var building = _buildings[i];
                var definition = building;
                button.name = definition.DisplayName.Replace(" ", string.Empty);
                var label = button.Label;
                label.text = definition.DisplayName;
                var icon = button.Icon;
                if (icon != null)
                {
                    icon.sprite = definition.Icon;
                    icon.color = definition.Icon != null ? Color.white : HudViewFactory.MutedTextColor;
                }
                button.Button.onClick.AddListener(() => BuildingRequested?.Invoke(definition));
            }
            _content.anchoredPosition = Vector2.zero;
            _scrollRect.verticalNormalizedPosition = 1f;
            RefreshContentLayout();
            ConfigureScrollContent(true);
        }

        private void EnsureButtonPool(int requiredCount)
        {
            if (requiredCount <= _buttons.Count) return;
            if (_buttons.Count == 0)
                throw new InvalidOperationException("RuntimeHUD requires at least one authored building card.");

            // Extra catalog entries use the first authored card as a template.
            var template = _buttons[0];
            while (_buttons.Count < requiredCount)
            {
                var card = UnityEngine.Object.Instantiate(template, _content);
                card.name = $"BuildingCard{_buttons.Count + 1}";
                card.Button.onClick.RemoveAllListeners();
                card.gameObject.SetActive(false);
                _buttons.Add(card);
            }
        }

        private void ConfigureScrollContent(bool resetToStart)
        {
            // If everything fits, leave scrolling off.
            _rowStride = Mathf.Max(1f, _buttonSize.y + _spacing);
            var sourceRows = SourceRowCount;
            var cycleHeight = sourceRows * _rowStride;
            var naturalHeight = _padding * 2f +
                                sourceRows * _buttonSize.y +
                                Mathf.Max(0, sourceRows - 1) * _spacing;
            _infiniteScrollEnabled = naturalHeight > _viewport.rect.height + 0.5f;
            var contentHeight = _infiniteScrollEnabled
                ? Mathf.Max(_viewport.rect.height + cycleHeight * 2f, naturalHeight)
                : naturalHeight;

            _suppressScrollCallback = true;
            if (resetToStart || !_infiniteScrollEnabled)
                _content.anchoredPosition = Vector2.zero;
            _content.sizeDelta = new Vector2(_content.sizeDelta.x, Mathf.Max(1f, contentHeight));
            _scrollRect.vertical = _infiniteScrollEnabled;
            if (!_infiniteScrollEnabled) _scrollRect.velocity = Vector2.zero;
            _suppressScrollCallback = false;

            PositionRecycledCards();
        }

        private void HandleScrollValueChanged(Vector2 _)
        {
            if (_suppressScrollCallback || !_infiniteScrollEnabled || _buildings.Count == 0) return;
            ExtendContentIfNeeded();
            PositionRecycledCards();
        }

        private void ExtendContentIfNeeded()
        {
            var sourceRows = SourceRowCount;
            var cycleHeight = sourceRows * _rowStride;
            var scrollY = Mathf.Max(0f, _content.anchoredPosition.y);
            var remaining = _content.rect.height - _viewport.rect.height - scrollY;
            if (remaining > cycleHeight * 1.5f) return;

            _suppressScrollCallback = true;
            _content.sizeDelta = new Vector2(
                _content.sizeDelta.x,
                _content.rect.height + cycleHeight * 4f);
            _suppressScrollCallback = false;
        }

        private void PositionRecycledCards()
        {
            if (_rowStride <= 0f) return;
            // Move the same cards to later rows; do not copy catalog entries.
            var sourceRows = SourceRowCount;
            var firstVisibleRow = 0;
            var cycleStartRow = 0;
            if (_infiniteScrollEnabled)
            {
                var scrollY = Mathf.Max(0f, _content.anchoredPosition.y - _padding);
                firstVisibleRow = Mathf.Max(0, Mathf.FloorToInt(scrollY / _rowStride));
                cycleStartRow = firstVisibleRow / sourceRows * sourceRows;
            }

            for (var i = 0; i < _buttons.Count; i++)
            {
                var active = i < _buildings.Count;
                var button = _buttons[i];
                button.gameObject.SetActive(active);
                if (!active) continue;

                var sourceRow = i / ColumnCount;
                var column = i % ColumnCount;
                var virtualRow = cycleStartRow + sourceRow;
                if (virtualRow < firstVisibleRow) virtualRow += sourceRows;

                HudViewFactory.SetRect(button.Root, new Rect(
                    _padding + column * (_buttonSize.x + _spacing),
                    _padding + virtualRow * _rowStride,
                    _buttonSize.x,
                    _buttonSize.y));
                _factory.LayoutButton(button);
                _factory.LayoutButtonIconByContentHeight(
                    button,
                    _buildings[i].Icon,
                    _buildings[i].VisualContentRect);
            }
        }

        private int SourceRowCount =>
            Mathf.Max(1, Mathf.CeilToInt(_buildings.Count / (float)ColumnCount));
    }
}
