using System;
using UnityEngine;
using UnityEngine.UI;

namespace Panteon.UI
{
    /// <summary>
    /// Runtime-only HUD binding and layout utilities. Visual hierarchy creation belongs to the editor generator.
    /// </summary>
    internal sealed class HudViewFactory
    {
        public static readonly Color CardColor = new Color32(64, 64, 64, 255);
        public static readonly Color MutedTextColor = new Color32(205, 202, 190, 255);

        public Sprite SolidSprite => null;
        public float Scale { get; private set; } = 1f;

        public HudViewFactory() { }

        public void SetScale(float scale) => Scale = Mathf.Max(0.1f, scale);

        public float Scaled(float value) => value * Scale;

        public Rect ScaledRect(float x, float y, float width, float height) =>
            new Rect(Scaled(x), Scaled(y), width, Scaled(height));

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

        public void BindDisplaySprite(Image image, Sprite source, Rect normalizedContentRect, float scale = 1f)
        {
            if (image == null) return;
            image.sprite = source != null ? source : SolidSprite;
            image.color = source != null ? Color.white : MutedTextColor;
            var content = ClampNormalizedRect(normalizedContentRect);
            var contentScale = 1f / Mathf.Max(content.width, content.height);
            image.rectTransform.localScale = Vector3.one * Mathf.Max(0.1f, scale * contentScale);
        }

        public static void SetButtonLabelSingleLine(Button button)
        {
            if (button == null) return;
            var label = button.transform.Find("Label")?.GetComponent<Text>();
            if (label == null) return;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;
        }

        public static void SetRect(RectTransform target, Rect rect)
        {
            if (target == null) return;
            target.anchorMin = target.anchorMax = new Vector2(0f, 1f);
            target.pivot = new Vector2(0f, 1f);
            target.anchoredPosition = new Vector2(rect.x, -rect.y);
            target.sizeDelta = rect.size;
        }

        public static T Require<T>(Transform root, string path) where T : Component
        {
            var component = Find<T>(root, path);
            if (component != null) return component;
            throw new InvalidOperationException($"RuntimeHUD prefab is missing {typeof(T).Name} at '{path}'.");
        }

        public static T Find<T>(Transform root, string path) where T : Component
        {
            var target = root != null ? root.Find(path) : null;
            return target != null ? target.GetComponent<T>() : null;
        }

        public static Rect FitSpriteContentByHeight(Sprite sprite, Rect normalizedContentRect, Rect bounds)
        {
            if (sprite == null || bounds.width <= 0f || bounds.height <= 0f || sprite.rect.height <= 0f)
                return bounds;

            var content = ClampNormalizedRect(normalizedContentRect);
            var height = bounds.height / content.height;
            var width = height * sprite.rect.width / sprite.rect.height;
            var contentOffset = new Vector2(
                (content.center.x - 0.5f) * width,
                (0.5f - content.center.y) * height);
            var imageCenter = bounds.center - contentOffset;
            return new Rect(
                imageCenter.x - width * 0.5f,
                imageCenter.y - height * 0.5f,
                width,
                height);
        }

        public void Dispose()
        {
            // The prefab and editor-generated assets own all HUD graphics.
        }

        private static Rect ClampNormalizedRect(Rect rect)
        {
            var xMin = Mathf.Clamp01(rect.xMin);
            var yMin = Mathf.Clamp01(rect.yMin);
            var xMax = Mathf.Clamp(rect.xMax, xMin + 0.0001f, 1f);
            var yMax = Mathf.Clamp(rect.yMax, yMin + 0.0001f, 1f);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }
    }
}
