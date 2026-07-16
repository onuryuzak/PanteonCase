using Panteon.Core;
using System.Collections;
using UnityEngine;

namespace Panteon.Gameplay.Selection
{
    public sealed class SelectableComponent : MonoBehaviour, ISelectable
    {
        [SerializeField] private GameObject _selectionVisual;
        private Coroutine _pulseRoutine;
        private Vector3 _selectionRestScale = Vector3.one;
        public bool IsSelected { get; private set; }

        private void Awake()
        {
            if (_selectionVisual == null) return;
            _selectionRestScale = _selectionVisual.transform.localScale;
            _selectionVisual.SetActive(false);
        }

        public void Select()
        {
            IsSelected = true;
            if (_selectionVisual == null) return;
            _selectionVisual.SetActive(true);
            if (_pulseRoutine != null) StopCoroutine(_pulseRoutine);
            _pulseRoutine = StartCoroutine(PulseRoutine());
        }

        public void Deselect()
        {
            IsSelected = false;
            StopPulse();
            if (_selectionVisual != null) _selectionVisual.SetActive(false);
        }

        private IEnumerator PulseRoutine()
        {
            const float duration = 0.18f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var multiplier = t < 0.6f
                    ? Mathf.Lerp(0.65f, 1.14f, t / 0.6f)
                    : Mathf.Lerp(1.14f, 1f, (t - 0.6f) / 0.4f);
                _selectionVisual.transform.localScale = _selectionRestScale * multiplier;
                yield return null;
            }

            _selectionVisual.transform.localScale = _selectionRestScale;
            _pulseRoutine = null;
        }

        private void StopPulse()
        {
            if (_pulseRoutine != null) StopCoroutine(_pulseRoutine);
            _pulseRoutine = null;
            if (_selectionVisual != null) _selectionVisual.transform.localScale = _selectionRestScale;
        }

        private void OnDisable()
        {
            IsSelected = false;
            StopPulse();
        }
    }
}
