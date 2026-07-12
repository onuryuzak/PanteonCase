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
            ShowEmpty();
        }

        public void ShowEmpty()
        {
            ClearButtons();
            _title.text = "NOTHING SELECTED";
            _subtitle.text = "Select a soldier or building.";
            _preview.sprite = _factory.SolidSprite;
            _preview.color = HudViewFactory.CardColor;
            _hp.text = string.Empty;
            _productionRoot.gameObject.SetActive(false);
        }

        public void Show(IDamageable selected, IProductionBuilding building, Action<IProductionBuilding, UnitDefinitionSO> requestProduction)
        {
            if (selected == null)
            {
                ShowEmpty();
                return;
            }

            ClearButtons();
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
    }
}
