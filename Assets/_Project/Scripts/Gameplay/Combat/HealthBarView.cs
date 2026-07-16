using Panteon.Core;
using UnityEngine;

namespace Panteon.Gameplay.Combat
{
    public sealed class HealthBarView : MonoBehaviour
    {
        private const float DefaultWidth = 0.62f;
        private const float DefaultHeight = 0.045f;
        private static Sprite _pixelSprite;

        [SerializeField] private Transform _fill;
        [SerializeField] private Transform _chipFill;
        [SerializeField] private GameObject _visualRoot;
        private SpriteRenderer _fillRenderer;
        private IDamageable _source;
        private bool _alwaysVisible;
        private float _healthNormalized = 1f;
        private float _chipNormalized = 1f;
        private float _chipReleaseTime;
        private bool _hasHealthSnapshot;

        private const float ChipDelay = 0.18f;
        private const float ChipDrainSpeed = 1.25f;
        private const float CriticalThreshold = 0.25f;
        private static readonly Color HealthyColor = new Color(0.2f, 1f, 0.35f, 0.95f);
        private static readonly Color CriticalColor = new Color(1f, 0.18f, 0.12f, 1f);
        private static readonly Color ChipColor = new Color(1f, 0.62f, 0.12f, 0.95f);

        public static HealthBarView Ensure(GameObject owner, float yOffset = 0.58f, int sortingOrder = 20)
        {
            var existing = owner.GetComponentInChildren<HealthBarView>(true);
            if (existing != null)
            {
                existing.EnsureVisual(yOffset, sortingOrder);
                return existing;
            }

            var root = new GameObject("HealthBar");
            root.transform.SetParent(owner.transform, false);
            root.transform.localPosition = new Vector3(0f, yOffset, 0f);

            var view = root.AddComponent<HealthBarView>();
            view.EnsureVisual(yOffset, sortingOrder);
            return view;
        }

        private void EnsureVisual(float yOffset, int sortingOrder)
        {
            transform.localPosition = new Vector3(transform.localPosition.x, yOffset, transform.localPosition.z);
            if (_fill != null && _chipFill != null && _visualRoot != null) return;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(transform, false);
            _visualRoot = visual;

            var background = CreateBarPart("Background", visual.transform, sortingOrder, new Color(0f, 0f, 0f, 0.65f));
            background.transform.localScale = new Vector3(DefaultWidth, DefaultHeight, 1f);

            var chip = CreateBarPart("ChipFill", visual.transform, sortingOrder + 1, ChipColor);
            chip.transform.localScale = new Vector3(DefaultWidth, DefaultHeight, 1f);
            chip.transform.localPosition = new Vector3(-DefaultWidth * 0.5f, 0f, 0f);
            _chipFill = chip.transform;

            var fill = CreateBarPart("Fill", visual.transform, sortingOrder + 2, HealthyColor);
            fill.transform.localScale = new Vector3(DefaultWidth, DefaultHeight, 1f);
            fill.transform.localPosition = new Vector3(-DefaultWidth * 0.5f, 0f, 0f);
            _fill = fill.transform;
            _fillRenderer = fill;
            visual.SetActive(false);
        }

        public void Bind(IDamageable source, bool alwaysVisible = false)
        {
            Unbind();
            _source = source;
            _alwaysVisible = alwaysVisible;
            if (_source == null) return;
            _hasHealthSnapshot = false;
            _source.OnHealthChanged += Refresh;
            Refresh(_source.CurrentHP, _source.MaxHP);
        }

        private void OnDisable() => Unbind();

        private void Update()
        {
            if (!_hasHealthSnapshot || _visualRoot == null || !_visualRoot.activeSelf) return;

            if (Time.time >= _chipReleaseTime && _chipNormalized > _healthNormalized)
            {
                _chipNormalized = Mathf.MoveTowards(
                    _chipNormalized,
                    _healthNormalized,
                    ChipDrainSpeed * Time.deltaTime);
                ApplyWidth(_chipFill, _chipNormalized);
            }

            ApplyCriticalPulse();
        }

        private void Refresh(int current, int max)
        {
            var normalized = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            if (!_hasHealthSnapshot)
            {
                _healthNormalized = normalized;
                _chipNormalized = normalized;
                _hasHealthSnapshot = true;
            }
            else if (normalized < _healthNormalized)
            {
                _chipNormalized = Mathf.Max(_chipNormalized, _healthNormalized);
                _chipReleaseTime = Time.time + ChipDelay;
                _healthNormalized = normalized;
            }
            else
            {
                _healthNormalized = normalized;
                _chipNormalized = normalized;
            }

            ApplyWidth(_fill, _healthNormalized);
            ApplyWidth(_chipFill, _chipNormalized);
            if (_visualRoot != null)
                _visualRoot.SetActive(current > 0 && (_alwaysVisible || current < max));
        }

        private void ApplyCriticalPulse()
        {
            if (_fill == null) return;
            if (_fillRenderer == null) _fillRenderer = _fill.GetComponent<SpriteRenderer>();
            if (_fillRenderer == null) return;

            var isCritical = _healthNormalized > 0f && _healthNormalized <= CriticalThreshold;
            if (!isCritical)
            {
                _fillRenderer.color = HealthyColor;
                _visualRoot.transform.localScale = Vector3.one;
                return;
            }

            var pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 9f);
            _fillRenderer.color = Color.Lerp(CriticalColor, Color.white, pulse * 0.22f);
            _visualRoot.transform.localScale = new Vector3(1f + pulse * 0.035f, 1f + pulse * 0.12f, 1f);
        }

        private static void ApplyWidth(Transform part, float normalized)
        {
            if (part == null) return;
            var width = DefaultWidth * Mathf.Clamp01(normalized);
            part.localScale = new Vector3(width, DefaultHeight, 1f);
            part.localPosition = new Vector3(-DefaultWidth * 0.5f + width * 0.5f, 0f, 0f);
        }

        private void Unbind()
        {
            if (_source != null) _source.OnHealthChanged -= Refresh;
            _source = null;
            _alwaysVisible = false;
            _hasHealthSnapshot = false;
            _healthNormalized = 1f;
            _chipNormalized = 1f;
            if (_visualRoot != null) _visualRoot.transform.localScale = Vector3.one;
        }

        private static SpriteRenderer CreateBarPart(string name, Transform parent, int sortingOrder, Color color)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = PixelSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private static Sprite PixelSprite
        {
            get
            {
                if (_pixelSprite != null) return _pixelSprite;
                var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                _pixelSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
                return _pixelSprite;
            }
        }
    }
}
