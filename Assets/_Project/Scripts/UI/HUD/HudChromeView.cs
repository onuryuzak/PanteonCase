using UnityEngine;
using UnityEngine.UI;

namespace Panteon.UI
{
    internal sealed class HudChromeView
    {
        private readonly HudViewFactory _factory;
        private readonly RectTransform _notes;
        private readonly Text _notesTitle;
        private readonly Text _notesBody;
        private readonly RectTransform _boardHeader;
        private readonly Text _boardTitle;

        public HudChromeView(RectTransform root, HudViewFactory factory)
        {
            _factory = factory;
            _notes = factory.Panel(root, "NotesPanel", HudViewFactory.PanelColor);
            _notesTitle = factory.Text(_notes, "NotesTitle", "SETUP NOTES", 16, FontStyle.Bold, TextAnchor.MiddleLeft, HudViewFactory.TextColor);
            _notesBody = factory.Text(_notes, "NotesBody",
                "1 Cell = 32 x 32 px\n1 Soldier = 1 x 1 cell\nBarracks = 4 x 4 cell\nPower Plant = 2 x 3 cell\n\nControls\nLMB Select\nRMB Move / Attack",
                12, FontStyle.Normal, TextAnchor.UpperLeft, HudViewFactory.MutedTextColor);
            _boardHeader = factory.Panel(root, "BoardHeader", HudViewFactory.PanelColor);
            _boardTitle = factory.Text(_boardHeader, "BoardTitle", "GAME BOARD", 22, FontStyle.Bold, TextAnchor.MiddleCenter, HudViewFactory.TextColor);
        }

        public void Layout(Rect notes, Rect boardHeader)
        {
            HudViewFactory.SetRect(_notes, notes);
            HudViewFactory.SetRect(_notesTitle.rectTransform, _factory.ScaledRect(16f, 18f, notes.width - _factory.Scaled(32f), 28f));
            HudViewFactory.SetRect(_notesBody.rectTransform, _factory.ScaledRect(16f, 76f, notes.width - _factory.Scaled(32f), 220f));
            HudViewFactory.SetRect(_boardHeader, boardHeader);
            HudViewFactory.SetRect(_boardTitle.rectTransform, new Rect(0f, 0f, boardHeader.width, boardHeader.height));
        }
    }
}
