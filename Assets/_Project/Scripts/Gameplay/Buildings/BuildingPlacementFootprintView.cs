using System.Collections;
using System.Collections.Generic;
using Panteon.Gameplay.Grid;
using UnityEngine;

namespace Panteon.Gameplay.Buildings
{
    /// <summary>
    /// Renders an entire placement footprint as one mesh. Shared cell edges are
    /// generated once, keeping line thickness uniform and draw calls constant.
    /// </summary>
    public sealed class BuildingPlacementFootprintView : MonoBehaviour
    {
        private static readonly Color ValidColor = new Color(0.18f, 1f, 0.36f, 0.92f);
        private static readonly Color InvalidColor = new Color(1f, 0.16f, 0.12f, 0.92f);
        private const float LineWidthRatio = 0.045f;
        private const float PlacedFadeDuration = 0.32f;

        private GridManager _grid;
        private FootprintMesh _preview;
        private FootprintMesh _placedFade;
        private Material _sharedMaterial;
        private Coroutine _fadeRoutine;

        public static BuildingPlacementFootprintView Ensure(GameObject owner)
        {
            var existing = owner.GetComponentInChildren<BuildingPlacementFootprintView>(true);
            if (existing != null) return existing;
            var root = new GameObject("BuildingPlacementFootprint");
            root.transform.SetParent(owner.transform, false);
            return root.AddComponent<BuildingPlacementFootprintView>();
        }

        public void Configure(GridManager grid)
        {
            _grid = grid;
            EnsureVisuals();
            HidePreview();
        }

        public void ShowPreview(Vector2Int origin, Vector2Int footprint, bool isValid)
        {
            if (_grid == null || footprint.x <= 0 || footprint.y <= 0) return;
            EnsureVisuals();
            _preview.Show(origin, footprint, _grid, isValid ? ValidColor : InvalidColor);
        }

        public void HidePreview() => _preview?.Hide();

        public void PlayPlacedFade(Vector2Int origin, Vector2Int footprint)
        {
            if (_grid == null || footprint.x <= 0 || footprint.y <= 0) return;
            EnsureVisuals();
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _placedFade.Show(origin, footprint, _grid, ValidColor);
            _fadeRoutine = StartCoroutine(FadePlacedRoutine());
        }

        private void OnDisable()
        {
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
            _preview?.Hide();
            _placedFade?.Hide();
        }

        private IEnumerator FadePlacedRoutine()
        {
            var elapsed = 0f;
            while (elapsed < PlacedFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / PlacedFadeDuration);
                var color = ValidColor;
                color.a *= 1f - t * t;
                _placedFade.SetColor(color);
                yield return null;
            }
            _placedFade.Hide();
            _fadeRoutine = null;
        }

        private void EnsureVisuals()
        {
            if (_sharedMaterial == null) _sharedMaterial = CreateSharedMaterial();
            if (_preview == null) _preview = FootprintMesh.Create("Preview", transform, _sharedMaterial, 56);
            if (_placedFade == null) _placedFade = FootprintMesh.Create("PlacedFade", transform, _sharedMaterial, 57);
        }

        private static Material CreateSharedMaterial()
        {
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default");
            if (shader == null) return null;
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "PlacementFootprintPixel",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply(false, true);
            var material = new Material(shader)
            {
                name = "PlacementFootprintShared",
                mainTexture = texture,
                hideFlags = HideFlags.HideAndDontSave
            };
            return material;
        }

        private sealed class FootprintMesh
        {
            private readonly Transform _transform;
            private readonly MeshRenderer _renderer;
            private readonly Mesh _mesh;
            private Vector2Int _builtFootprint;
            private float _builtCellSize;
            private Color32[] _colors;

            private FootprintMesh(Transform transform, MeshRenderer renderer, Mesh mesh)
            {
                _transform = transform;
                _renderer = renderer;
                _mesh = mesh;
            }

            public static FootprintMesh Create(string name, Transform parent, Material material, int sortingOrder)
            {
                var owner = new GameObject(name);
                owner.transform.SetParent(parent, false);
                var filter = owner.AddComponent<MeshFilter>();
                var renderer = owner.AddComponent<MeshRenderer>();
                var mesh = new Mesh { name = $"{name}Mesh" };
                mesh.MarkDynamic();
                filter.sharedMesh = mesh;
                renderer.sharedMaterial = material;
                renderer.sortingOrder = sortingOrder;
                owner.SetActive(false);
                return new FootprintMesh(owner.transform, renderer, mesh);
            }

            public void Show(Vector2Int origin, Vector2Int footprint, GridManager grid, Color color)
            {
                if (_builtFootprint != footprint || !Mathf.Approximately(_builtCellSize, grid.CellSize))
                    Build(footprint, grid.CellSize);
                var lowerLeft = grid.CellToWorld(origin) - new Vector3(grid.CellSize * 0.5f, grid.CellSize * 0.5f, 0f);
                _transform.position = lowerLeft;
                _transform.localScale = Vector3.one;
                SetColor(color);
                _transform.gameObject.SetActive(true);
            }

            public void SetColor(Color color)
            {
                if (_colors == null) return;
                var value = (Color32)color;
                for (var i = 0; i < _colors.Length; i++) _colors[i] = value;
                _mesh.colors32 = _colors;
            }

            public void Hide() => _transform.gameObject.SetActive(false);

            private void Build(Vector2Int footprint, float cellSize)
            {
                _builtFootprint = footprint;
                _builtCellSize = cellSize;
                var vertices = new List<Vector3>();
                var triangles = new List<int>();
                var halfWidth = cellSize * LineWidthRatio * 0.5f;
                var width = footprint.x * cellSize;
                var height = footprint.y * cellSize;

                // Vertical lines own every intersection. Horizontal lines are split
                // between them so no edge or crossing is drawn twice.
                for (var x = 0; x <= footprint.x; x++)
                {
                    var centerX = x * cellSize;
                    AddQuad(vertices, triangles,
                        centerX - halfWidth, 0f,
                        centerX + halfWidth, height);
                }

                for (var y = 0; y <= footprint.y; y++)
                {
                    var centerY = y * cellSize;
                    for (var x = 0; x < footprint.x; x++)
                    {
                        var minX = x * cellSize + halfWidth;
                        var maxX = (x + 1) * cellSize - halfWidth;
                        AddQuad(vertices, triangles,
                            minX, centerY - halfWidth,
                            maxX, centerY + halfWidth);
                    }
                }

                _mesh.Clear();
                _mesh.SetVertices(vertices);
                _mesh.SetTriangles(triangles, 0, true);
                _colors = new Color32[vertices.Count];
                _mesh.colors32 = _colors;
                _mesh.RecalculateBounds();
            }

            private static void AddQuad(
                List<Vector3> vertices,
                List<int> triangles,
                float minX,
                float minY,
                float maxX,
                float maxY)
            {
                var start = vertices.Count;
                vertices.Add(new Vector3(minX, minY, 0f));
                vertices.Add(new Vector3(maxX, minY, 0f));
                vertices.Add(new Vector3(maxX, maxY, 0f));
                vertices.Add(new Vector3(minX, maxY, 0f));
                triangles.Add(start);
                triangles.Add(start + 2);
                triangles.Add(start + 1);
                triangles.Add(start);
                triangles.Add(start + 3);
                triangles.Add(start + 2);
            }
        }
    }
}
