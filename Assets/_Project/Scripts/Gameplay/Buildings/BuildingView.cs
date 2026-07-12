using Panteon.Data;
using UnityEngine;

namespace Panteon.Gameplay.Buildings
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class BuildingView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;

        private void Awake()
        {
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
        }

        public void Render(BuildingDefinitionSO definition)
        {
            if (_renderer != null && definition.Icon != null) _renderer.sprite = definition.Icon;
        }
    }
}

