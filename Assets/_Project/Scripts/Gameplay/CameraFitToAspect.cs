using Panteon.Gameplay.Grid;
using UnityEngine;

namespace Panteon.Gameplay
{
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFitToAspect : MonoBehaviour
    {
        [SerializeField] private float _baseOrthoSize = 8f;
        [SerializeField] private float _targetAspect = 16f / 9f;
        [SerializeField] private float _panSpeed = 8f;
        private Camera _camera;
        private GridManager _grid;
        private int _lastWidth;
        private int _lastHeight;

        public void Configure(GridManager grid) { _grid = grid; Fit(); }
        private void Awake() => _camera = GetComponent<Camera>();

        private void Update()
        {
            if (_lastWidth != Screen.width || _lastHeight != Screen.height) Fit();
            var input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (input.sqrMagnitude > 1f) input.Normalize();
            transform.position += (Vector3)(input * (_panSpeed * Time.deltaTime));
            ClampToGrid();
        }

        private void Fit()
        {
            if (_camera == null) _camera = GetComponent<Camera>();
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;
            var aspect = _lastHeight > 0 ? (float)_lastWidth / _lastHeight : _targetAspect;
            _camera.orthographicSize = aspect < _targetAspect ? _baseOrthoSize * (_targetAspect / aspect) : _baseOrthoSize;
        }

        private void ClampToGrid()
        {
            if (_grid == null) return;
            var min = _grid.CellToWorld(Vector2Int.zero);
            var max = _grid.CellToWorld(_grid.Size - Vector2Int.one);
            var position = transform.position;
            position.x = Mathf.Clamp(position.x, min.x, max.x);
            position.y = Mathf.Clamp(position.y, min.y, max.y);
            transform.position = position;
        }
    }
}
