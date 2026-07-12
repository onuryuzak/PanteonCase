using UnityEngine;

namespace Panteon.UI
{
    internal sealed class BoardViewportController
    {
        private readonly Camera _camera;
        private readonly Vector2Int _gridSize;
        private readonly Vector2 _gridOrigin;
        private readonly float _cellSize;
        private readonly float _paddingPercent;
        private Texture2D _texture;
        private Sprite _sprite;
        private SpriteRenderer _renderer;
        private int _pixelsPerCell = -1;

        public BoardViewportController(Camera camera, Vector2Int gridSize, Vector2 gridOrigin, float cellSize, float paddingPercent)
        {
            _camera = camera;
            _gridSize = gridSize;
            _gridOrigin = gridOrigin;
            _cellSize = cellSize;
            _paddingPercent = paddingPercent;
            EnsureGridRenderer();
        }

        public void Apply(Rect board)
        {
            if (_camera == null || Screen.width <= 0 || Screen.height <= 0 || board.width <= 0f || board.height <= 0f) return;
            board = PixelAlign(board);
            var padding = 1f + Mathf.Max(0f, _paddingPercent);
            var pixelsPerCell = Mathf.Max(4, Mathf.FloorToInt(Mathf.Min(
                board.width / Mathf.Max(1, _gridSize.x),
                board.height / Mathf.Max(1, _gridSize.y)) / padding));
            UpdateGridSprite(pixelsPerCell);

            _camera.rect = new Rect(board.x / Screen.width, (Screen.height - board.yMax) / Screen.height,
                board.width / Screen.width, board.height / Screen.height);
            _camera.aspect = board.width / Mathf.Max(1f, board.height);
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.white;
            _camera.orthographicSize = Mathf.Max(0.1f, board.height * _cellSize / (2f * pixelsPerCell));

            var position = GetPixelAlignedCameraPosition(board, pixelsPerCell);
            _camera.transform.position = new Vector3(position.x, position.y, _camera.transform.position.z);
        }

        public void Dispose()
        {
            if (_sprite != null) Object.Destroy(_sprite);
            if (_texture != null) Object.Destroy(_texture);
        }

        private void EnsureGridRenderer()
        {
            var target = GameObject.Find("PlaytestGround") ?? new GameObject("PlaytestGround");
            target.transform.position = new Vector3(
                _gridOrigin.x + _gridSize.x * _cellSize * 0.5f,
                _gridOrigin.y + _gridSize.y * _cellSize * 0.5f, 0f);
            target.transform.localScale = Vector3.one;
            _renderer = target.GetComponent<SpriteRenderer>() ?? target.AddComponent<SpriteRenderer>();
            _renderer.sortingOrder = -20;
            _renderer.color = Color.white;
            UpdateGridSprite(32);
        }

        private void UpdateGridSprite(int pixelsPerCell)
        {
            pixelsPerCell = Mathf.Clamp(pixelsPerCell, 4, 256);
            if (_renderer == null || _pixelsPerCell == pixelsPerCell && _sprite != null) return;
            if (_sprite != null) Object.Destroy(_sprite);
            if (_texture != null) Object.Destroy(_texture);
            _pixelsPerCell = pixelsPerCell;
            _sprite = CreateGridSprite(pixelsPerCell);
            _renderer.sprite = _sprite;
        }

        private Sprite CreateGridSprite(int pixelsPerCell)
        {
            var width = Mathf.Max(1, _gridSize.x * pixelsPerCell);
            var height = Mathf.Max(1, _gridSize.y * pixelsPerCell);
            _texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var background = new Color(0.96f, 0.97f, 0.98f, 1f);
            var line = new Color(0.68f, 0.72f, 0.78f, 1f);
            var pixels = new Color[width * height];
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var isLine = x == 0 || y == 0 || x == width - 1 || y == height - 1 ||
                             x % pixelsPerCell == 0 || y % pixelsPerCell == 0;
                pixels[y * width + x] = isLine ? line : background;
            }
            _texture.SetPixels(pixels);
            _texture.Apply(false, true);
            return Sprite.Create(_texture, new Rect(0f, 0f, width, height), Vector2.one * 0.5f, pixelsPerCell / _cellSize);
        }

        private Vector2 GetPixelAlignedCameraPosition(Rect board, int pixelsPerCell)
        {
            var pixelsPerWorldUnit = pixelsPerCell / Mathf.Max(0.0001f, _cellSize);
            var gridPixelWidth = _gridSize.x * pixelsPerCell;
            var gridPixelHeight = _gridSize.y * pixelsPerCell;
            var screenGridLeft = board.x + (board.width - gridPixelWidth) * 0.5f;
            var screenGridBottom = Screen.height - board.yMax + (board.height - gridPixelHeight) * 0.5f;
            var snapX = Mathf.Round(screenGridLeft) - screenGridLeft;
            var snapY = Mathf.Round(screenGridBottom) - screenGridBottom;
            return new Vector2(
                _gridOrigin.x + _gridSize.x * _cellSize * 0.5f,
                _gridOrigin.y + _gridSize.y * _cellSize * 0.5f) -
                new Vector2(snapX / pixelsPerWorldUnit, snapY / pixelsPerWorldUnit);
        }

        private static Rect PixelAlign(Rect rect)
        {
            var xMin = Mathf.Round(rect.xMin);
            var yMin = Mathf.Round(rect.yMin);
            var xMax = Mathf.Round(rect.xMax);
            var yMax = Mathf.Round(rect.yMax);
            return Rect.MinMaxRect(xMin, yMin, Mathf.Max(xMin + 1f, xMax), Mathf.Max(yMin + 1f, yMax));
        }
    }
}
