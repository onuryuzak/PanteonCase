using Panteon.Data;
using UnityEngine;

namespace Panteon.Gameplay.Buildings
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class BuildingView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        private SpriteRenderer _visualRenderer;

        private void Awake()
        {
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
        }

        public void Render(BuildingDefinitionSO definition)
        {
            if (_renderer == null || definition == null || definition.Icon == null) return;
            EnsureVisualRenderer();
            _renderer.enabled = false;
            _visualRenderer.sprite = definition.Icon;
            _visualRenderer.gameObject.SetActive(true);

            float worldScale;
            Vector3 worldOffset;
            if (!BuildingVisualScaleUtility.TryGetWorldLayout(definition, out worldScale, out worldOffset)) return;
            var rootScale = transform.localScale;
            _visualRenderer.transform.localScale = new Vector3(
                worldScale / Mathf.Max(0.01f, Mathf.Abs(rootScale.x)),
                worldScale / Mathf.Max(0.01f, Mathf.Abs(rootScale.y)), 1f);
            _visualRenderer.transform.localPosition = new Vector3(
                worldOffset.x / Mathf.Max(0.01f, Mathf.Abs(rootScale.x)),
                worldOffset.y / Mathf.Max(0.01f, Mathf.Abs(rootScale.y)), 0f);
        }

        private void EnsureVisualRenderer()
        {
            if (_visualRenderer != null) return;
            var visual = transform.Find("BuildingVisual");
            if (visual == null)
            {
                visual = new GameObject("BuildingVisual").transform;
                visual.SetParent(transform, false);
            }

            _visualRenderer = visual.GetComponent<SpriteRenderer>();
            if (_visualRenderer == null) _visualRenderer = visual.gameObject.AddComponent<SpriteRenderer>();
            _visualRenderer.sharedMaterial = _renderer.sharedMaterial;
            _visualRenderer.sortingLayerID = _renderer.sortingLayerID;
            _visualRenderer.sortingOrder = _renderer.sortingOrder;
            _visualRenderer.color = _renderer.color;
            visual.localRotation = Quaternion.identity;
        }
    }
}

