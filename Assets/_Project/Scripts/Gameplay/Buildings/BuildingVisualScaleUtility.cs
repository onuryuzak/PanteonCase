using Panteon.Data;
using UnityEngine;

namespace Panteon.Gameplay.Buildings
{
    // Fits the visible artwork to the cells reserved by the building.
    internal static class BuildingVisualScaleUtility
    {
        public static bool TryGetWorldLayout(BuildingDefinitionSO definition,
            out float uniformScale, out Vector3 centerOffset)
        {
            uniformScale = 1f;
            centerOffset = Vector3.zero;
            if (definition == null || definition.Icon == null) return false;

            var spriteSize = (Vector2)definition.Icon.bounds.size;
            // Tiny Swords sprites include transparent padding, so bounds alone look too small.
            var contentRect = definition.VisualContentRect;
            var contentSize = Vector2.Scale(spriteSize, contentRect.size);
            if (contentSize.x <= 0f || contentSize.y <= 0f) return false;

            var footprint = definition.FootprintSize;
            uniformScale = Mathf.Min(footprint.x / contentSize.x, footprint.y / contentSize.y);

            var normalizedCenter = contentRect.position + contentRect.size * 0.5f;
            var centerFromPivot = Vector2.Scale(normalizedCenter - new Vector2(0.5f, 0.5f), spriteSize);
            centerOffset = -(Vector3)(centerFromPivot * uniformScale);
            return true;
        }
    }
}
