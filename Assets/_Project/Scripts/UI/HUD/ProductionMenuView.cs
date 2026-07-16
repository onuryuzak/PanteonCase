using System;
using System.Collections.Generic;
using Panteon.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Panteon.UI
{
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
        }

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
            var rows = Mathf.Max(1, Mathf.CeilToInt(_buildings.Count / (float)ColumnCount));
            var contentHeight = _padding * 2f + rows * (_buttonSize.y + _spacing) - _spacing;
            _content.sizeDelta = new Vector2(0f, Mathf.Max(1f, contentHeight));
            _scrollRect.vertical = contentHeight > _viewport.rect.height;

            for (var i = 0; i < _buttons.Count; i++)
            {
                var row = i / ColumnCount;
                var column = i % ColumnCount;
                HudViewFactory.SetRect(_buttons[i].Root, new Rect(
                    _padding + column * (_buttonSize.x + _spacing),
                    _padding + row * (_buttonSize.y + _spacing),
                    _buttonSize.x, _buttonSize.y));
                _factory.LayoutButton(_buttons[i]);
                if (i < _buildings.Count)
                    _factory.LayoutButtonIconByContentHeight(
                        _buttons[i], _buildings[i].Icon, _buildings[i].VisualContentRect);
            }
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
            var rows = Mathf.Max(1, Mathf.CeilToInt(_buildings.Count / (float)ColumnCount));
            var contentHeight = _padding * 2f + rows * (_buttonSize.y + _spacing) - _spacing;
            _content.sizeDelta = new Vector2(0f, Mathf.Max(1f, contentHeight));
            _scrollRect.vertical = contentHeight > scrollHeight;

            for (var i = 0; i < _buttons.Count; i++)
            {
                var active = i < _buildings.Count;
                _buttons[i].gameObject.SetActive(active);
                if (!active) continue;
                var row = i / ColumnCount;
                var column = i % ColumnCount;
                HudViewFactory.SetRect(_buttons[i].Root, new Rect(
                    _padding + column * (_buttonSize.x + _spacing),
                    _padding + row * (_buttonSize.y + _spacing),
                    _buttonSize.x, _buttonSize.y));
                _factory.LayoutButton(_buttons[i]);
                _factory.LayoutButtonIconByContentHeight(
                    _buttons[i], _buildings[i].Icon, _buildings[i].VisualContentRect);
            }
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
        }

        private void EnsureButtonPool(int requiredCount)
        {
            if (requiredCount <= _buttons.Count) return;
            if (_buttons.Count == 0)
                throw new InvalidOperationException("RuntimeHUD requires at least one authored building card.");

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
    }
}
