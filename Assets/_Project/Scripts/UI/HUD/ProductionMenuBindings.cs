using System;
using UnityEngine;
using UnityEngine.UI;

namespace Panteon.UI
{
    public sealed class ProductionMenuBindings : MonoBehaviour
    {
        [SerializeField] private Image _titlePlate;
        [SerializeField] private Text _title;
        [SerializeField] private Text _subtitle;
        [SerializeField] private RectTransform _scroll;
        [SerializeField] private RectTransform _viewport;
        [SerializeField] private RectTransform _content;
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private Text _status;
        [SerializeField] private HudButtonView[] _cards;

        public RectTransform Root => (RectTransform)transform;
        public Image TitlePlate => _titlePlate;
        public Text Title => _title;
        public Text Subtitle => _subtitle;
        public RectTransform Scroll => _scroll;
        public RectTransform Viewport => _viewport;
        public RectTransform Content => _content;
        public ScrollRect ScrollRect => _scrollRect;
        public Text Status => _status;
        public HudButtonView[] Cards => _cards;

        public void ValidateReferences()
        {
            if (_titlePlate == null || _title == null || _subtitle == null || _scroll == null ||
                _viewport == null || _content == null || _scrollRect == null || _status == null ||
                _cards == null || _cards.Length == 0)
                throw new InvalidOperationException("RuntimeHUD production panel bindings are incomplete.");

            foreach (var card in _cards)
            {
                if (card == null) throw new InvalidOperationException("RuntimeHUD production card reference is missing.");
                card.ValidateReferences();
            }
        }
    }
}
