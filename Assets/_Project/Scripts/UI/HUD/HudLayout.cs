using UnityEngine;

namespace Panteon.UI
{
    internal readonly struct HudLayout
    {
        public readonly Rect Notes;
        public readonly Rect Production;
        public readonly Rect BoardHeader;
        public readonly Rect Board;
        public readonly Rect Information;

        private HudLayout(Rect notes, Rect production, Rect boardHeader, Rect board, Rect information)
        {
            Notes = notes;
            Production = production;
            BoardHeader = boardHeader;
            Board = board;
            Information = information;
        }

        public static HudLayout Calculate(int width, int height, float scale)
        {
            var notesWidth = Mathf.Clamp(width * 0.16f, 120f * scale, 180f * scale);
            var productionWidth = Mathf.Clamp(width * 0.16f, 120f * scale, 170f * scale);
            var informationWidth = Mathf.Clamp(width * 0.2f, 170f * scale, 240f * scale);
            var headerHeight = Mathf.Clamp(height * 0.14f, 70f * scale, 92f * scale);
            var boardX = notesWidth + productionWidth;
            var boardWidth = Mathf.Max(180f, width - boardX - informationWidth);

            return new HudLayout(
                new Rect(0f, 0f, notesWidth, height),
                new Rect(notesWidth, 0f, productionWidth, height),
                new Rect(boardX, 0f, boardWidth, headerHeight),
                new Rect(boardX, headerHeight, boardWidth, height - headerHeight),
                new Rect(boardX + boardWidth, 0f, informationWidth, height));
        }
    }
}
