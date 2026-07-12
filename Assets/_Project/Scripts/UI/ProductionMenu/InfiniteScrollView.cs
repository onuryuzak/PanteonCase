using System;
using System.Collections.Generic;
using Panteon.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Panteon.UI.ProductionMenu
{
    public sealed class InfiniteScrollView : MonoBehaviour
    {
        [SerializeField] private RectTransform _viewport;
        [SerializeField] private RectTransform _content;
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private ProductionListItem _itemPrefab;
        [SerializeField, Min(1f)] private float _itemHeight = 96f;
        private readonly List<ProductionListItem> _items = new List<ProductionListItem>();
        private IReadOnlyList<BuildingDefinitionSO> _data;
        private Action<BuildingDefinitionSO> _selected;
        private int _firstVisible = -1;

        public int PooledItemCount => _items.Count;

        private void Awake() => ResolveScrollReferences();

        private void Reset()
        {
            ResolveScrollReferences();
        }

        private void OnEnable()
        {
            ResolveScrollReferences();
            if (_scrollRect != null)
                _scrollRect.onValueChanged.AddListener(OnScroll);
        }

        private void OnDisable()
        {
            if (_scrollRect != null)
                _scrollRect.onValueChanged.RemoveListener(OnScroll);
        }

        private void ResolveScrollReferences()
        {
            if (_scrollRect == null) _scrollRect = GetComponentInParent<ScrollRect>();
            if (_scrollRect == null) return;
            if (_viewport == null) _viewport = _scrollRect.viewport;
            if (_content == null) _content = _scrollRect.content;
        }

        public void SetData(IReadOnlyList<BuildingDefinitionSO> data, Action<BuildingDefinitionSO> selected)
        {
            _data = data ?? Array.Empty<BuildingDefinitionSO>();
            _selected = selected;
            if (_viewport == null || _content == null || _itemPrefab == null) return;
            _content.sizeDelta = new Vector2(_content.sizeDelta.x, _data.Count * _itemHeight);
            var needed = Mathf.Min(_data.Count, Mathf.CeilToInt(_viewport.rect.height / _itemHeight) + 2);
            while (_items.Count < needed) _items.Add(Instantiate(_itemPrefab, _content));
            for (var i = needed; i < _items.Count; i++) _items[i].gameObject.SetActive(false);
            _firstVisible = -1;
            Refresh();
        }

        public void OnScroll(Vector2 _) => Refresh();

        private void Refresh()
        {
            if (_data == null || _content == null) return;
            var first = Mathf.Clamp(Mathf.FloorToInt(_content.anchoredPosition.y / _itemHeight), 0, Mathf.Max(0, _data.Count - 1));
            if (first == _firstVisible) return;
            _firstVisible = first;
            for (var i = 0; i < _items.Count; i++)
            {
                var index = first + i;
                var item = _items[i];
                var visible = index < _data.Count;
                item.gameObject.SetActive(visible);
                if (!visible)
                {
                    item.Clear();
                    continue;
                }
                item.Bind(_data[index], _selected);
                item.RectTransform.anchoredPosition = new Vector2(0f, -index * _itemHeight);
            }
        }
    }
}
