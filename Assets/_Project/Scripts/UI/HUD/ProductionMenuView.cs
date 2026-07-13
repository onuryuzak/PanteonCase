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
        private readonly Text _title;
        private readonly Text _subtitle;
        private readonly RectTransform _scroll;
        private readonly RectTransform _viewport;
        private readonly RectTransform _content;
        private readonly ScrollRect _scrollRect;
        private readonly Text _status;
        private readonly List<Button> _buttons = new List<Button>();
        private readonly List<BuildingDefinitionSO> _buildings = new List<BuildingDefinitionSO>();
        private Vector2 _buttonSize;
        private float _spacing;
        private float _padding;

        public event Action<BuildingDefinitionSO> BuildingRequested;

        public ProductionMenuView(RectTransform root, HudViewFactory factory)
        {
            _factory = factory;
            _panel = factory.Panel(root, "ProductionPanel", HudViewFactory.PanelColor);
            _title = factory.Text(_panel, "ProductionTitle", "PRODUCTION MENU", 16, FontStyle.Bold, TextAnchor.MiddleLeft, HudViewFactory.TextColor);
            _subtitle = factory.Text(_panel, "ProductionSubtitle", "Infinite Scrollview", 11, FontStyle.Normal, TextAnchor.MiddleLeft, HudViewFactory.MutedTextColor);
            _scroll = HudViewFactory.CreateRect("ProductionScroll", _panel);
            var background = _scroll.gameObject.AddComponent<Image>();
            background.sprite = factory.SolidSprite;
            background.color = HudViewFactory.CardColor;
            _scrollRect = _scroll.gameObject.AddComponent<ScrollRect>();
            _scrollRect.horizontal = false;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;
            _viewport = HudViewFactory.CreateRect("Viewport", _scroll);
            var viewportImage = _viewport.gameObject.AddComponent<Image>();
            viewportImage.sprite = factory.SolidSprite;
            viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
            _viewport.gameObject.AddComponent<RectMask2D>();
            _content = HudViewFactory.CreateRect("Content", _viewport);
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0f, 1f);
            _scrollRect.viewport = _viewport;
            _scrollRect.content = _content;
            _status = factory.Text(_panel, "Status", string.Empty, 11, FontStyle.Normal, TextAnchor.MiddleLeft, HudViewFactory.MutedTextColor);
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
            HudViewFactory.SetRect(_title.rectTransform, _factory.ScaledRect(14f, 16f, rect.width - _factory.Scaled(28f), 28f));
            HudViewFactory.SetRect(_subtitle.rectTransform, _factory.ScaledRect(14f, 46f, rect.width - _factory.Scaled(28f), 22f));
            HudViewFactory.SetRect(_scroll, _factory.ScaledRect(10f, 84f, rect.width - _factory.Scaled(20f), rect.height - _factory.Scaled(138f)));
            HudViewFactory.SetRect(_viewport, new Rect(0f, 0f, rect.width - _factory.Scaled(20f), rect.height - _factory.Scaled(138f)));
            HudViewFactory.SetRect(_status.rectTransform, new Rect(_factory.Scaled(12f), rect.height - _factory.Scaled(44f), rect.width - _factory.Scaled(24f), _factory.Scaled(28f)));

            var cell = Mathf.Max(_factory.Scaled(42f), Mathf.Min(_factory.Scaled(64f), (rect.width - _factory.Scaled(48f)) * 0.5f));
            _buttonSize = new Vector2(cell, cell + _factory.Scaled(18f));
            _spacing = _factory.Scaled(8f);
            _padding = Mathf.Round(_factory.Scaled(8f));
            var rows = Mathf.Max(1, Mathf.CeilToInt(_buildings.Count / (float)ColumnCount));
            var contentHeight = _padding * 2f + rows * (_buttonSize.y + _spacing) - _spacing;
            _content.sizeDelta = new Vector2(0f, Mathf.Max(1f, contentHeight));
            _scrollRect.vertical = contentHeight > _viewport.rect.height;

            for (var i = 0; i < _buttons.Count; i++)
            {
                var row = i / ColumnCount;
                var column = i % ColumnCount;
                HudViewFactory.SetRect((RectTransform)_buttons[i].transform, new Rect(
                    _padding + column * (_buttonSize.x + _spacing),
                    _padding + row * (_buttonSize.y + _spacing),
                    _buttonSize.x, _buttonSize.y));
                _factory.LayoutButton(_buttons[i]);
            }
        }

        private void RebuildButtons()
        {
            foreach (var button in _buttons)
                if (button != null) UnityEngine.Object.Destroy(button.gameObject);
            _buttons.Clear();

            foreach (var building in _buildings)
            {
                var definition = building;
                var button = _factory.Button(_content, definition.DisplayName, definition.Icon, definition.VisualContentRect);
                button.onClick.AddListener(() => BuildingRequested?.Invoke(definition));
                _buttons.Add(button);
            }
            _content.anchoredPosition = Vector2.zero;
            _scrollRect.verticalNormalizedPosition = 1f;
        }
    }
}
