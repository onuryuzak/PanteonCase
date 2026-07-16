using System.Collections.Generic;
using Panteon.Data;
using UnityEngine;

namespace Panteon.Gameplay.Feedback
{
    internal sealed class CommandFeedbackPool
    {
        private const int PrewarmCount = 6;
        private const int MaxCount = 12;
        private readonly Transform _root;
        private readonly Queue<CommandRing> _available = new Queue<CommandRing>();
        private readonly List<CommandRing> _active = new List<CommandRing>();
        private readonly Sprite _ringSprite;
        private readonly Material _material;
        private readonly float _cellSize;

        public CommandFeedbackPool(Transform parent, float cellSize)
        {
            _cellSize = Mathf.Max(0.1f, cellSize);
            _root = new GameObject("CommandFeedbackPool").transform;
            _root.SetParent(parent, false);
            _ringSprite = CreateRingSprite();
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default");
            _material = shader != null ? new Material(shader) : null;
            if (_material != null)
            {
                _material.name = "CommandFeedbackShared";
                _material.mainTexture = _ringSprite.texture;
                _material.hideFlags = HideFlags.HideAndDontSave;
            }
            for (var i = 0; i < PrewarmCount; i++) _available.Enqueue(Create());
        }

        public void Show(Vector3 position, CommandFeedbackType type)
        {
            CommandRing ring;
            if (_available.Count > 0) ring = _available.Dequeue();
            else if (_active.Count < MaxCount) ring = Create();
            else
            {
                ring = _active[0];
                _active.RemoveAt(0);
            }
            ring.Show(position, type, _cellSize);
            _active.Add(ring);
        }

        public void Tick(float deltaTime)
        {
            for (var i = _active.Count - 1; i >= 0; i--)
            {
                var ring = _active[i];
                if (ring.Tick(deltaTime)) continue;
                _active.RemoveAt(i);
                ring.Hide();
                _available.Enqueue(ring);
            }
        }

        private CommandRing Create()
        {
            var owner = new GameObject("CommandRing");
            owner.transform.SetParent(_root, false);
            var renderer = owner.AddComponent<SpriteRenderer>();
            renderer.sprite = _ringSprite;
            renderer.sharedMaterial = _material;
            renderer.sortingOrder = 75;
            owner.SetActive(false);
            return new CommandRing(owner.transform, renderer);
        }

        private static Sprite CreateRingSprite()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "CommandRing",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[size * size];
            var center = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                var ring = 1f - Mathf.Clamp01(Mathf.Abs(distance - 0.78f) / 0.075f);
                var alpha = (byte)Mathf.RoundToInt(ring * ring * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = "CommandRing";
            return sprite;
        }

        private sealed class CommandRing
        {
            private const float Lifetime = 0.34f;
            private readonly Transform _transform;
            private readonly SpriteRenderer _renderer;
            private float _age;
            private float _size;
            private Color _color;

            public CommandRing(Transform transform, SpriteRenderer renderer)
            {
                _transform = transform;
                _renderer = renderer;
            }

            public void Show(Vector3 position, CommandFeedbackType type, float cellSize)
            {
                _age = 0f;
                _size = cellSize;
                _color = type == CommandFeedbackType.Attack
                    ? new Color(1f, 0.18f, 0.12f, 1f)
                    : new Color(0.36f, 0.72f, 0.28f, 1f);
                position.z = 0f;
                _transform.position = position;
                _transform.localScale = Vector3.one * (_size * 0.35f);
                _renderer.color = _color;
                _transform.gameObject.SetActive(true);
            }

            public bool Tick(float deltaTime)
            {
                _age += deltaTime;
                if (_age >= Lifetime) return false;
                var t = _age / Lifetime;
                var eased = 1f - (1f - t) * (1f - t);
                _transform.localScale = Vector3.one * (_size * Mathf.Lerp(0.35f, 1.05f, eased));
                var color = _color;
                color.a = 1f - t;
                _renderer.color = color;
                return true;
            }

            public void Hide() => _transform.gameObject.SetActive(false);
        }
    }
}
