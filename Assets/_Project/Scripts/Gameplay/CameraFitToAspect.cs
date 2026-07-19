using Panteon.Core;
using Panteon.Data;
using Panteon.Gameplay.Grid;
using UnityEngine;

namespace Panteon.Gameplay
{
    [RequireComponent(typeof(Camera))]
    // Keeps the game board framed while still allowing pan and hit shake.
    public sealed class CameraFitToAspect : MonoBehaviour
    {
        [SerializeField] private float _baseOrthoSize = 8f;
        [SerializeField] private float _targetAspect = 16f / 9f;
        [SerializeField] private float _panSpeed = 8f;
        private Camera _camera;
        private GridManager _grid;
        private EventBus _bus;
        private int _lastWidth;
        private int _lastHeight;
        private Vector3 _shakeOffset;
        private float _shakeAmplitude;
        private float _shakeDuration;
        private float _shakeRemaining;
        private float _shakePhase;

        public void Configure(GridManager grid, EventBus bus)
        {
            if (_bus != null) _bus.Unsubscribe<CameraShakeRequested>(HandleShakeRequested);
            _grid = grid;
            _bus = bus;
            _bus?.Subscribe<CameraShakeRequested>(HandleShakeRequested);
            Fit();
        }

        private void Awake() => _camera = GetComponent<Camera>();

        private void Update()
        {
            RemoveShakeOffset();
            if (_lastWidth != Screen.width || _lastHeight != Screen.height) Fit();
            var input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (input.sqrMagnitude > 1f) input.Normalize();
            transform.position += (Vector3)(input * (_panSpeed * Time.deltaTime));
            ClampToGrid();
        }

        private void LateUpdate()
        {
            ApplyShakeOffset();
        }

        private void Fit()
        {
            if (_camera == null) _camera = GetComponent<Camera>();
            // Recalculate only after a resolution change; this otherwise runs every frame.
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

        private void HandleShakeRequested(CameraShakeRequested request)
        {
            if (request.Amplitude <= 0f || request.Duration <= 0f) return;
            if (_shakeRemaining <= 0f) _shakePhase = 0f;
            _shakeAmplitude = Mathf.Max(_shakeAmplitude, request.Amplitude);
            _shakeDuration = Mathf.Max(_shakeDuration, request.Duration);
            _shakeRemaining = Mathf.Max(_shakeRemaining, request.Duration);
        }

        private void ApplyShakeOffset()
        {
            if (_shakeRemaining <= 0f || _shakeDuration <= 0f) return;
            // Remove last frame's shake before adding the new offset to avoid camera drift.
            _shakeRemaining = Mathf.Max(0f, _shakeRemaining - Time.unscaledDeltaTime);
            _shakePhase += Time.unscaledDeltaTime * 52f;
            var decay = _shakeRemaining / _shakeDuration;
            var amplitude = _shakeAmplitude * decay * decay;
            _shakeOffset = new Vector3(
                Mathf.Sin(_shakePhase) * amplitude,
                Mathf.Sin(_shakePhase * 1.37f + 0.8f) * amplitude * 0.65f,
                0f);
            transform.position += _shakeOffset;

            if (_shakeRemaining > 0f) return;
            _shakeAmplitude = 0f;
            _shakeDuration = 0f;
        }

        private void RemoveShakeOffset()
        {
            if (_shakeOffset == Vector3.zero) return;
            transform.position -= _shakeOffset;
            _shakeOffset = Vector3.zero;
        }

        private void OnDestroy()
        {
            RemoveShakeOffset();
            if (_bus != null) _bus.Unsubscribe<CameraShakeRequested>(HandleShakeRequested);
        }
    }
}
