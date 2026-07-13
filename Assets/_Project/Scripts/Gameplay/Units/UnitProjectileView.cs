using System.Collections;
using UnityEngine;

namespace Panteon.Gameplay.Units
{
    [DisallowMultipleComponent]
    public sealed class UnitProjectileView : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Coroutine _flight;

        public static UnitProjectileView Ensure(GameObject owner)
        {
            var view = owner.GetComponent<UnitProjectileView>();
            return view != null ? view : owner.AddComponent<UnitProjectileView>();
        }

        public void Play(Sprite sprite, Vector3 destination, float duration)
        {
            Stop();
            if (sprite == null) return;
            EnsureRenderer();
            var start = transform.position;
            _renderer.sprite = sprite;
            var scale = sprite.bounds.size.y > 0f ? 0.18f / sprite.bounds.size.y : 1f;
            _renderer.transform.localScale = new Vector3(scale, scale, 1f);
            _renderer.transform.position = start;
            _renderer.transform.right = destination - start;
            _renderer.gameObject.SetActive(true);
            _flight = StartCoroutine(Fly(start, destination, Mathf.Max(0.05f, duration)));
        }

        public void Stop()
        {
            if (_flight != null) StopCoroutine(_flight);
            _flight = null;
            if (_renderer != null) _renderer.gameObject.SetActive(false);
        }

        private IEnumerator Fly(Vector3 start, Vector3 destination, float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _renderer.transform.position = Vector3.Lerp(start, destination, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            _flight = null;
            _renderer.gameObject.SetActive(false);
        }

        private void EnsureRenderer()
        {
            if (_renderer != null) return;
            var visual = new GameObject("TinySwordsProjectile");
            visual.transform.SetParent(transform, false);
            _renderer = visual.AddComponent<SpriteRenderer>();
            var source = GetComponent<SpriteRenderer>();
            if (source != null) _renderer.sharedMaterial = source.sharedMaterial;
            _renderer.sortingOrder = 66;
            visual.SetActive(false);
        }
    }
}
