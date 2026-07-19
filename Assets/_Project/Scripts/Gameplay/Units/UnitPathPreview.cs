using System.Collections.Generic;
using Panteon.Gameplay.Grid;
using UnityEngine;

namespace Panteon.Gameplay.Units
{
    /// <summary>
    /// Builds all visible path rings into one mesh.
    /// </summary>
    internal sealed class UnitPathPreview : MonoBehaviour
    {
        private const int CircleSegments = 20;
        private static readonly Color PathColor = new Color(0.32f, 0.48f, 0.16f, 0.95f);
        private static Material _sharedMaterial;

        private readonly List<Vector2Int> _path = new List<Vector2Int>();
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<int> _triangles = new List<int>();
        private readonly List<Color> _colors = new List<Color>();
        private GridManager _grid;
        private GameObject _renderObject;
        private Mesh _mesh;
        private MeshRenderer _renderer;
        private int _firstVisibleIndex;

        public static UnitPathPreview Ensure(GameObject owner)
        {
            var preview = owner.GetComponent<UnitPathPreview>();
            if (preview == null) preview = owner.AddComponent<UnitPathPreview>();
            preview.EnsureRenderer();
            return preview;
        }

        public void Show(IReadOnlyList<Vector2Int> path, GridManager grid)
        {
            _path.Clear();
            if (path != null)
                for (var i = 0; i < path.Count; i++) _path.Add(path[i]);

            _grid = grid;
            _firstVisibleIndex = 1;
            RebuildMesh();
        }

        public void SetFirstVisibleIndex(int index)
        {
            _firstVisibleIndex = Mathf.Clamp(index, 0, _path.Count);
            RebuildMesh();
        }

        public void Clear()
        {
            _path.Clear();
            _grid = null;
            _firstVisibleIndex = 0;
            if (_mesh != null) _mesh.Clear();
            if (_renderObject != null) _renderObject.SetActive(false);
        }

        private void EnsureRenderer()
        {
            if (_renderObject != null) return;

            _renderObject = new GameObject($"PathPreview_{GetInstanceID()}");
            _renderObject.transform.position = Vector3.zero;
            _renderObject.transform.rotation = Quaternion.identity;
            _renderObject.transform.localScale = Vector3.one;
            var filter = _renderObject.AddComponent<MeshFilter>();
            _renderer = _renderObject.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = GetSharedMaterial();
            _renderer.sortingOrder = -5;

            _mesh = new Mesh { name = $"UnitPathMesh_{GetInstanceID()}" };
            _mesh.MarkDynamic();
            filter.sharedMesh = _mesh;
            _renderObject.SetActive(false);
        }

        private void RebuildMesh()
        {
            EnsureRenderer();
            _vertices.Clear();
            _triangles.Clear();
            _colors.Clear();

            if (_grid == null || _firstVisibleIndex >= _path.Count)
            {
                _mesh.Clear();
                _renderObject.SetActive(false);
                return;
            }

            var outerRadius = _grid.CellSize * 0.18f;
            var innerRadius = outerRadius * 0.58f;
            for (var pathIndex = _firstVisibleIndex; pathIndex < _path.Count; pathIndex++)
                AppendRing(_grid.CellToWorld(_path[pathIndex]), outerRadius, innerRadius);

            _mesh.Clear();
            _mesh.SetVertices(_vertices);
            _mesh.SetTriangles(_triangles, 0, true);
            _mesh.SetColors(_colors);
            _mesh.RecalculateBounds();
            _renderObject.SetActive(_vertices.Count > 0);
        }

        private void AppendRing(Vector3 center, float outerRadius, float innerRadius)
        {
            center.z = 0f;
            for (var segment = 0; segment < CircleSegments; segment++)
            {
                var angleA = segment * Mathf.PI * 2f / CircleSegments;
                var angleB = (segment + 1) * Mathf.PI * 2f / CircleSegments;
                var directionA = new Vector3(Mathf.Cos(angleA), Mathf.Sin(angleA));
                var directionB = new Vector3(Mathf.Cos(angleB), Mathf.Sin(angleB));
                var vertexStart = _vertices.Count;

                _vertices.Add(center + directionA * outerRadius);
                _vertices.Add(center + directionB * outerRadius);
                _vertices.Add(center + directionB * innerRadius);
                _vertices.Add(center + directionA * innerRadius);
                for (var i = 0; i < 4; i++) _colors.Add(PathColor);

                _triangles.Add(vertexStart);
                _triangles.Add(vertexStart + 1);
                _triangles.Add(vertexStart + 2);
                _triangles.Add(vertexStart);
                _triangles.Add(vertexStart + 2);
                _triangles.Add(vertexStart + 3);
            }
        }

        private static Material GetSharedMaterial()
        {
            if (_sharedMaterial != null) return _sharedMaterial;
            var shader = Shader.Find("Sprites/Default");
            _sharedMaterial = new Material(shader)
            {
                name = "M_UnitPathPreview",
                hideFlags = HideFlags.HideAndDontSave
            };
            return _sharedMaterial;
        }

        private void OnDisable() => Clear();

        private void OnDestroy()
        {
            if (_renderObject != null) Destroy(_renderObject);
            if (_mesh != null) Destroy(_mesh);
        }
    }
}
