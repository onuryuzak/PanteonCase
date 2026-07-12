using UnityEngine;

namespace Panteon.Gameplay.Selection
{
    internal sealed class DragSelectionView : MonoBehaviour
    {
        private static readonly Color FillColor = new Color(0.01f, 0.12f, 0.21f, 0.48f);
        private static readonly Color BorderColor = new Color(0f, 0.62f, 1f, 0.95f);
        private const float BorderThickness = 2f;

        private Rect _screenRect;
        private bool _visible;

        public static DragSelectionView Ensure(GameObject owner)
        {
            var view = owner.GetComponent<DragSelectionView>();
            return view != null ? view : owner.AddComponent<DragSelectionView>();
        }

        public void Show(Rect screenRect)
        {
            _screenRect = screenRect;
            _visible = screenRect.width > 0f && screenRect.height > 0f;
        }

        public void Hide() => _visible = false;

        private void OnGUI()
        {
            if (!_visible) return;

            var previousDepth = GUI.depth;
            GUI.depth = -1000;
            var guiRect = new Rect(
                _screenRect.xMin,
                Screen.height - _screenRect.yMax,
                _screenRect.width,
                _screenRect.height);
            var previousColor = GUI.color;
            GUI.color = FillColor;
            GUI.DrawTexture(guiRect, Texture2D.whiteTexture);
            GUI.color = BorderColor;
            GUI.DrawTexture(new Rect(guiRect.xMin, guiRect.yMin, guiRect.width, BorderThickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(guiRect.xMin, guiRect.yMax - BorderThickness, guiRect.width, BorderThickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(guiRect.xMin, guiRect.yMin, BorderThickness, guiRect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(guiRect.xMax - BorderThickness, guiRect.yMin, BorderThickness, guiRect.height), Texture2D.whiteTexture);
            GUI.color = previousColor;
            GUI.depth = previousDepth;
        }

        private void OnDisable() => Hide();
    }
}
