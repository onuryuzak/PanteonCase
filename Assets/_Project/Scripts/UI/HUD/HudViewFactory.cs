using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Panteon.UI
{
    internal sealed class HudViewFactory
    {
        public static readonly Color PanelColor = new Color(0.08f, 0.1f, 0.14f, 0.96f);
        public static readonly Color CardColor = new Color(0.16f, 0.19f, 0.24f, 0.98f);
        public static readonly Color AccentColor = new Color(0.25f, 0.67f, 0.95f, 1f);
        public static readonly Color TextColor = new Color(0.95f, 0.97f, 1f, 1f);
        public static readonly Color MutedTextColor = new Color(0.7f, 0.76f, 0.84f, 1f);

        private readonly Dictionary<Text, int> _baseTextSizes = new Dictionary<Text, int>();
        private readonly Font _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        private readonly Texture2D _solidTexture;
        private readonly Texture2D _circleTexture;

        public Sprite SolidSprite { get; }
        public Sprite CircleSprite { get; }
        public float Scale { get; private set; } = 1f;

        public HudViewFactory()
        {
            _solidTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            _solidTexture.SetPixel(0, 0, Color.white);
            _solidTexture.Apply(false, true);
            SolidSprite = Sprite.Create(_solidTexture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f, 1f);

            _circleTexture = CreateCircleTexture(64);
            CircleSprite = Sprite.Create(_circleTexture, new Rect(0f, 0f, 64f, 64f), Vector2.one * 0.5f, 64f);
        }

        public RectTransform CreateHudRoot()
        {
            EnsureEventSystem();
            var canvasObject = GameObject.Find("Canvas_UI") ?? new GameObject("Canvas_UI");
            var canvas = EnsureComponent<Canvas>(canvasObject);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            EnsureComponent<GraphicRaycaster>(canvasObject);
            var scaler = EnsureComponent<CanvasScaler>(canvasObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            Disable(canvasObject.transform, "ProductionMenu");
            Disable(canvasObject.transform, "InfoPanel");
            Disable(canvasObject.transform, "PlacementGhostLayer");
            var existing = canvasObject.transform.Find("RuntimeHUD");
            if (existing != null) Object.Destroy(existing.gameObject);

            var root = CreateRect("RuntimeHUD", canvasObject.transform);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            return root;
        }

        public RectTransform Panel(Transform parent, string name, Color color)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = SolidSprite;
            image.color = color;
            return rect;
        }

        public Image Image(Transform parent, string name, Color color)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = SolidSprite;
            image.color = color;
            return image;
        }

        public Text Text(Transform parent, string name, string value, int size, FontStyle style, TextAnchor anchor, Color color)
        {
            var label = CreateRect(name, parent).gameObject.AddComponent<Text>();
            label.font = _font;
            label.text = value;
            label.fontStyle = style;
            label.alignment = anchor;
            label.color = color;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            _baseTextSizes[label] = size;
            label.fontSize = Mathf.Max(8, Mathf.RoundToInt(size * Scale));
            return label;
        }

        public Button Button(Transform parent, string label, Sprite icon)
        {
            var rect = CreateRect(string.IsNullOrWhiteSpace(label) ? "Button" : label.Replace(" ", string.Empty), parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = SolidSprite;
            image.color = CardColor;
            var button = rect.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = CardColor;
            colors.highlightedColor = new Color(0.22f, 0.28f, 0.36f, 1f);
            colors.pressedColor = AccentColor;
            button.colors = colors;

            var iconImage = Image(rect, "Icon", new Color(1f, 1f, 1f, 0.08f));
            iconImage.sprite = icon != null ? icon : SolidSprite;
            iconImage.color = icon != null ? Color.white : new Color(1f, 1f, 1f, 0.08f);
            iconImage.preserveAspect = true;
            Text(rect, "Label", label, 8, FontStyle.Bold, TextAnchor.MiddleCenter, TextColor);
            LayoutButton(button);
            return button;
        }

        public Slider Slider(Transform parent, string name, float minimum, float maximum, float value)
        {
            var root = CreateRect(name, parent);
            var slider = root.gameObject.AddComponent<Slider>();
            slider.minValue = minimum;
            slider.maxValue = maximum;
            slider.value = value;
            var background = Image(root, "Background", CardColor);
            Stretch(background.rectTransform);
            var fillArea = CreateRect("Fill Area", root);
            Stretch(fillArea);
            var fill = Image(fillArea, "Fill", AccentColor);
            Stretch(fill.rectTransform);
            var handleArea = CreateRect("Handle Slide Area", root);
            Stretch(handleArea);
            var handle = Image(handleArea, "Handle", TextColor);
            handle.rectTransform.sizeDelta = new Vector2(18f, 28f);
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            return slider;
        }

        public void SetScale(float scale)
        {
            Scale = scale;
            foreach (var pair in _baseTextSizes)
                if (pair.Key != null) pair.Key.fontSize = Mathf.Max(8, Mathf.RoundToInt(pair.Value * scale));
        }

        public void LayoutButton(Button button)
        {
            if (button == null) return;
            var icon = button.transform.Find("Icon") as RectTransform;
            if (icon != null)
            {
                icon.anchorMin = icon.anchorMax = new Vector2(0.5f, 1f);
                icon.pivot = new Vector2(0.5f, 1f);
                icon.anchoredPosition = new Vector2(0f, -Scaled(8f));
                icon.sizeDelta = Vector2.one * Scaled(30f);
            }
            var label = button.transform.Find("Label") as RectTransform;
            if (label == null) return;
            label.anchorMin = new Vector2(0f, 0f);
            label.anchorMax = new Vector2(1f, 0f);
            label.pivot = new Vector2(0.5f, 0f);
            label.anchoredPosition = new Vector2(0f, Scaled(4f));
            label.sizeDelta = new Vector2(-Scaled(8f), Scaled(22f));
        }

        public float Scaled(float value) => value * Scale;
        public Rect ScaledRect(float x, float y, float width, float height) =>
            new Rect(Scaled(x), Scaled(y), width, Scaled(height));

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return (RectTransform)gameObject.transform;
        }

        public static void SetRect(RectTransform target, Rect rect)
        {
            if (target == null) return;
            target.anchorMin = target.anchorMax = new Vector2(0f, 1f);
            target.pivot = new Vector2(0f, 1f);
            target.anchoredPosition = new Vector2(rect.x, -rect.y);
            target.sizeDelta = rect.size;
        }

        public void Dispose()
        {
            if (SolidSprite != null) Object.Destroy(SolidSprite);
            if (_solidTexture != null) Object.Destroy(_solidTexture);
            if (CircleSprite != null) Object.Destroy(CircleSprite);
            if (_circleTexture != null) Object.Destroy(_circleTexture);
        }

        private static Texture2D CreateCircleTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[size * size];
            var center = (size - 1) * 0.5f;
            var radiusSquared = center * center;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var dx = x - center;
                var dy = y - center;
                pixels[y * size + x] = dx * dx + dy * dy <= radiusSquared ? Color.white : Color.clear;
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static void Stretch(RectTransform target)
        {
            target.anchorMin = Vector2.zero;
            target.anchorMax = Vector2.one;
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static void Disable(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child != null) child.gameObject.SetActive(false);
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var target = new GameObject("EventSystem");
            target.AddComponent<EventSystem>();
            target.AddComponent<StandaloneInputModule>();
        }
    }
}
