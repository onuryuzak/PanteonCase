using UnityEngine;

namespace Panteon.UI
{
    internal readonly struct HudLayout
    {
        public readonly Rect Production;
        public readonly Rect Board;
        public readonly Rect Information;

        private HudLayout(Rect production, Rect board, Rect information)
        {
            Production = production;
            Board = board;
            Information = information;
        }

        public static HudLayout Calculate(int width, int height, float scale)
        {
            var minimumBoardWidth = Mathf.Min(width * 0.6f, 320f * scale);
            var maximumSideWidth = Mathf.Max(0f, (width - minimumBoardWidth) * 0.5f);
            var desiredSideWidth = Mathf.Clamp(width * 0.2f, 150f * scale, 190f * scale);
            var sideWidth = Mathf.Min(desiredSideWidth, maximumSideWidth);
            var boardWidth = Mathf.Max(1f, width - sideWidth * 2f);

            return new HudLayout(
                new Rect(0f, 0f, sideWidth, height),
                new Rect(sideWidth, 0f, boardWidth, height),
                new Rect(sideWidth + boardWidth, 0f, sideWidth, height));
        }
    }
}
