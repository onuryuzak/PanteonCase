using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Panteon.UI
{
    internal sealed class HudViewFactory
    {
        public static readonly Color PanelColor = new Color32(43, 43, 43, 255);
        public static readonly Color CardColor = new Color32(61, 61, 58, 255);
        public static readonly Color HeaderColor = new Color32(74, 91, 73, 255);
        public static readonly Color PlateColor = new Color32(111, 108, 91, 255);
        public static readonly Color BorderColor = new Color32(118, 115, 96, 255);
        public static readonly Color AccentColor = new Color32(92, 111, 82, 255);
        public static readonly Color DangerColor = new Color32(190, 70, 70, 255);
        public static readonly Color TextColor = new Color32(246, 244, 238, 255);
        public static readonly Color MutedTextColor = new Color32(205, 202, 190, 255);

        private readonly Dictionary<Text, int> _baseTextSizes = new Dictionary<Text, int>();
        private readonly Dictionary<string, Sprite> _displaySprites = new Dictionary<string, Sprite>();
        private readonly Font _fallbackFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        private readonly Font _regularFont;
        private readonly Font _boldFont;
        private readonly Texture2D _solidTexture;
        private readonly Texture2D _circleTexture;
        private readonly Texture2D _roundedTexture;

        public Sprite SolidSprite { get; }
        public Sprite CircleSprite { get; }
        public Sprite RoundedSprite { get; }
        public float Scale { get; private set; } = 1f;

        public HudViewFactory(Font regularFont, Font boldFont)
        {
            _regularFont = regularFont != null ? regularFont : _fallbackFont;
            _boldFont = boldFont != null ? boldFont : _regularFont;
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
            _roundedTexture = CreateRoundedRectTexture(32, 8);
            RoundedSprite = Sprite.Create(_roundedTexture, new Rect(0f, 0f, 32f, 32f),
                Vector2.one * 0.5f, 32f, 0, SpriteMeshType.FullRect, new Vector4(8f, 8f, 8f, 8f));
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

            RemoveLegacyScaleControls(canvasObject.transform);
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

        public void StyleRounded(Image image)
        {
            if (image == null) return;
            image.sprite = RoundedSprite;
            image.type = UnityEngine.UI.Image.Type.Sliced;
        }

        public Text Text(Transform parent, string name, string value, int size, FontStyle style, TextAnchor anchor, Color color)
        {
            var label = CreateRect(name, parent).gameObject.AddComponent<Text>();
            var wantsBold = style == FontStyle.Bold || style == FontStyle.BoldAndItalic;
            var selectedFont = wantsBold ? _boldFont : _regularFont;
            label.font = selectedFont != null ? selectedFont : _fallbackFont;
            label.text = value;
            var wantsItalic = style == FontStyle.Italic || style == FontStyle.BoldAndItalic;
            label.fontStyle = label.font == _fallbackFont ? style : wantsItalic ? FontStyle.Italic : FontStyle.Normal;
            label.alignment = anchor;
            label.color = color;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            _baseTextSizes[label] = size;
            label.fontSize = Mathf.Max(8, Mathf.RoundToInt(size * Scale));
            return label;
        }

        public Button Button(Transform parent, string label, Sprite icon, Rect? contentRect = null)
        {
            var rect = CreateRect(string.IsNullOrWhiteSpace(label) ? "Button" : label.Replace(" ", string.Empty), parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = RoundedSprite;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.color = CardColor;
            rect.gameObject.AddComponent<RectMask2D>();
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = BorderColor;
            outline.effectDistance = new Vector2(Scaled(2f), -Scaled(2f));
            outline.useGraphicAlpha = true;
            var button = rect.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = CardColor;
            colors.highlightedColor = new Color32(79, 82, 72, 255);
            colors.pressedColor = AccentColor;
            button.colors = colors;

            var iconImage = Image(rect, "Icon", new Color(1f, 1f, 1f, 0.08f));
            iconImage.sprite = icon != null ? DisplaySprite(icon, contentRect ?? new Rect(0f, 0f, 1f, 1f)) : SolidSprite;
            iconImage.color = icon != null ? Color.white : new Color(1f, 1f, 1f, 0.08f);
            iconImage.preserveAspect = true;
            Text(rect, "Label", label, 8, FontStyle.Bold, TextAnchor.MiddleCenter, TextColor);
            LayoutButton(button);
            return button;
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
                var buttonRect = (RectTransform)button.transform;
                var topInset = Mathf.Max(2f, buttonRect.rect.height * 0.06f);
                var labelSpace = Mathf.Max(Scaled(20f), buttonRect.rect.height * 0.28f);
                var availableWidth = buttonRect.rect.width * 0.82f;
                var availableHeight = buttonRect.rect.height - topInset - labelSpace;
                var iconSize = Mathf.Max(1f, Mathf.Min(availableWidth, availableHeight));
                icon.anchoredPosition = new Vector2(0f, -topInset);
                icon.sizeDelta = Vector2.one * iconSize;
            }
            var label = button.transform.Find("Label") as RectTransform;
            if (label == null) return;
            label.anchorMin = new Vector2(0f, 0f);
            label.anchorMax = new Vector2(1f, 0f);
            label.pivot = new Vector2(0.5f, 0f);
            label.anchoredPosition = new Vector2(0f, Scaled(2f));
            label.sizeDelta = new Vector2(-Scaled(2f), Scaled(26f));
        }

        public static void SetButtonLabelSingleLine(Button button)
        {
            if (button == null) return;
            var label = button.transform.Find("Label")?.GetComponent<Text>();
            if (label == null) return;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;
        }

        public void LayoutButtonIconByContentHeight(Button button, Sprite sprite, Rect normalizedContentRect)
        {
            if (button == null || sprite == null) return;
            var icon = button.transform.Find("Icon")?.GetComponent<Image>();
            if (icon == null) return;

            var buttonRect = (RectTransform)button.transform;
            var topInset = Mathf.Max(2f, buttonRect.rect.height * 0.06f);
            var labelSpace = Mathf.Max(Scaled(20f), buttonRect.rect.height * 0.28f);
            var horizontalInset = buttonRect.rect.width * 0.09f;
            var bounds = new Rect(
                horizontalInset,
                topInset,
                buttonRect.rect.width - horizontalInset * 2f,
                Mathf.Max(1f, buttonRect.rect.height - topInset - labelSpace));

            icon.sprite = sprite;
            icon.preserveAspect = false;
            icon.rectTransform.localScale = Vector3.one;
            SetRect(icon.rectTransform, FitSpriteContentByHeight(sprite, normalizedContentRect, bounds));
        }

        public float Scaled(float value) => value * Scale;

        public Sprite DisplaySprite(Sprite source, Rect normalizedContentRect)
        {
            if (source == null) return SolidSprite;

            var content = ClampNormalizedRect(normalizedContentRect);
            if (content.xMin <= 0f && content.yMin <= 0f && content.xMax >= 1f && content.yMax >= 1f)
                return source;

            var key = $"{source.GetInstanceID()}:{content.x:F5}:{content.y:F5}:{content.width:F5}:{content.height:F5}";
            if (_displaySprites.TryGetValue(key, out var cached) && cached != null) return cached;

            // Sprite.texture points at the atlas texture after packing, so its crop
            // coordinates must come from textureRect rather than the source asset rect.
            var sourceRect = source.textureRect;
            var croppedRect = new Rect(
                sourceRect.x + sourceRect.width * content.x,
                sourceRect.y + sourceRect.height * content.y,
                sourceRect.width * content.width,
                sourceRect.height * content.height);
            var displaySprite = Sprite.Create(source.texture, croppedRect, Vector2.one * 0.5f, source.pixelsPerUnit);
            displaySprite.name = $"{source.name}_UI";
            _displaySprites[key] = displaySprite;
            return displaySprite;
        }

        public static void SetButtonIconScale(Button button, float scale)
        {
            if (button == null) return;
            var icon = button.transform.Find("Icon") as RectTransform;
            if (icon != null) icon.localScale = Vector3.one * Mathf.Max(0.1f, scale);
        }
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

        public static Rect FitSpriteContentByHeight(Sprite sprite, Rect normalizedContentRect, Rect bounds)
        {
            if (sprite == null || bounds.width <= 0f || bounds.height <= 0f || sprite.rect.height <= 0f)
                return bounds;

            var content = ClampNormalizedRect(normalizedContentRect);
            var height = bounds.height / content.height;
            var width = height * sprite.rect.width / sprite.rect.height;
            var contentCenter = content.center;
            var contentOffset = new Vector2(
                (contentCenter.x - 0.5f) * width,
                (0.5f - contentCenter.y) * height);
            var boundsCenter = bounds.center;
            var imageCenter = boundsCenter - contentOffset;

            return new Rect(
                imageCenter.x - width * 0.5f,
                imageCenter.y - height * 0.5f,
                width,
                height);
        }

        public void Dispose()
        {
            foreach (var sprite in _displaySprites.Values)
                if (sprite != null) Object.Destroy(sprite);
            _displaySprites.Clear();
            if (SolidSprite != null) Object.Destroy(SolidSprite);
            if (_solidTexture != null) Object.Destroy(_solidTexture);
            if (CircleSprite != null) Object.Destroy(CircleSprite);
            if (_circleTexture != null) Object.Destroy(_circleTexture);
            if (RoundedSprite != null) Object.Destroy(RoundedSprite);
            if (_roundedTexture != null) Object.Destroy(_roundedTexture);
        }

        private static Rect ClampNormalizedRect(Rect rect)
        {
            var xMin = Mathf.Clamp01(rect.xMin);
            var yMin = Mathf.Clamp01(rect.yMin);
            var xMax = Mathf.Clamp(rect.xMax, xMin + 0.0001f, 1f);
            var yMax = Mathf.Clamp(rect.yMax, yMin + 0.0001f, 1f);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
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

        private static Texture2D CreateRoundedRectTexture(int size, int radius)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[size * size];
            var corner = radius - 0.5f;
            var radiusSquared = corner * corner;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var dx = x < radius ? corner - x : x >= size - radius ? x - (size - radius) - corner : 0f;
                var dy = y < radius ? corner - y : y >= size - radius ? y - (size - radius) - corner : 0f;
                pixels[y * size + x] = dx * dx + dy * dy <= radiusSquared
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(255, 255, 255, 0);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
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

        private static void RemoveLegacyScaleControls(Transform root)
        {
            if (root == null) return;
            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (child.name == "UiScaleTitle" || child.name == "UiScaleSlider" || child.name == "UiScaleValue")
                {
                    Object.Destroy(child.gameObject);
                    continue;
                }

                RemoveLegacyScaleControls(child);
            }
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
