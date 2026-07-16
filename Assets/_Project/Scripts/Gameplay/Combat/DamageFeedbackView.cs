using System;
using System.Collections;
using Panteon.Core;
using UnityEngine;

namespace Panteon.Gameplay.Combat
{
    public interface ICombatFeedbackReceiver
    {
        void PrepareImpact(Vector3 sourceWorldPosition);
    }

    public interface IEntityVisualProvider
    {
        SpriteRenderer VisualRenderer { get; }
    }

    internal static class CombatParticleMaterial
    {
        private static Material _shared;

        public static Material Shared
        {
            get
            {
                if (_shared != null) return _shared;
                var shader = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default");
                if (shader == null) return null;
                _shared = new Material(shader)
                {
                    name = "CombatParticlesShared",
                    hideFlags = HideFlags.HideAndDontSave,
                    mainTexture = CreateParticleTexture()
                };
                return _shared;
            }
        }

        private static Texture2D CreateParticleTexture()
        {
            const int size = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "CombatParticleCircle",
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
                var alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01((1f - distance) * 4f) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }

    public sealed class DamageFeedbackView : MonoBehaviour
    {
        private ParticleSystem _particles;
        private IDamageable _source;
        private int _lastHp = -1;
        private Coroutine _visualRoutine;
        private Transform _visual;
        private SpriteRenderer _visualRenderer;
        private Vector3 _restPosition;
        private Vector3 _restScale;
        private Color _restColor;
        private Vector3 _impactOrigin;
        private bool _hasImpactOrigin;

        public static DamageFeedbackView Ensure(GameObject owner)
        {
            var existing = owner.GetComponentInChildren<DamageFeedbackView>(true);
            if (existing != null)
            {
                existing.EnsureParticles();
                return existing;
            }

            var root = new GameObject("DamageFeedback");
            root.transform.SetParent(owner.transform, false);
            root.transform.localPosition = Vector3.zero;
            var view = root.AddComponent<DamageFeedbackView>();
            view.EnsureParticles();
            return view;
        }

        public void Bind(IDamageable source)
        {
            Unbind();
            ResetFeedback();
            _source = source;
            if (_source == null) return;
            _lastHp = _source.CurrentHP;
            _source.OnHealthChanged += HandleHealthChanged;
        }

        public void PlaySpawn()
        {
            StopVisualRoutine(true);
            _visualRoutine = StartCoroutine(SpawnRoutine());
        }

        public void PrepareImpact(Vector3 sourceWorldPosition)
        {
            _impactOrigin = sourceWorldPosition;
            _hasImpactOrigin = true;
        }

        public void PlayRevealSpawn()
        {
            StopVisualRoutine(true);
            _visualRoutine = StartCoroutine(RevealSpawnRoutine());
        }

        public void PlayDeath(Action completed)
        {
            StopVisualRoutine(true);
            if (!TryCaptureVisual())
            {
                completed?.Invoke();
                return;
            }

            _visualRoutine = StartCoroutine(DeathRoutine(completed));
        }

        public void ResetFeedback()
        {
            StopVisualRoutine(true);
            _hasImpactOrigin = false;
            if (_particles != null)
                _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void OnDisable()
        {
            ResetFeedback();
            Unbind();
        }

        private void HandleHealthChanged(int current, int max)
        {
            if (_lastHp >= 0 && current < _lastHp)
            {
                Play();
            }
            _lastHp = current;
        }

        private void Play()
        {
            EnsureParticles();
            StopVisualRoutine(true);
            var hasVisual = TryCaptureVisual();
            var center = hasVisual ? _visualRenderer.bounds.center : transform.position;
            var particleOrigin = hasVisual
                ? ResolveImpactExitPoint(center, _visualRenderer.bounds.extents)
                : center;
            EmitImpactParticles(particleOrigin);
            if (hasVisual) _visualRoutine = StartCoroutine(HitRoutine());
            _hasImpactOrigin = false;
        }

        private Vector3 ResolveImpactExitPoint(Vector3 center, Vector3 extents)
        {
            if (!_hasImpactOrigin) return center;
            var exitDirection = center - _impactOrigin;
            exitDirection.z = 0f;
            if (exitDirection.sqrMagnitude <= 0.0001f) return center;
            exitDirection.Normalize();

            var distanceX = Mathf.Abs(exitDirection.x) > 0.0001f
                ? extents.x / Mathf.Abs(exitDirection.x)
                : float.PositiveInfinity;
            var distanceY = Mathf.Abs(exitDirection.y) > 0.0001f
                ? extents.y / Mathf.Abs(exitDirection.y)
                : float.PositiveInfinity;
            var edgeDistance = Mathf.Min(distanceX, distanceY);
            if (float.IsInfinity(edgeDistance) || float.IsNaN(edgeDistance)) return center;

            // Emit from the far side of the target and continue travelling away
            // from the attacker, like an impact exiting through the back.
            return center + exitDirection * edgeDistance * 0.92f;
        }

        private IEnumerator SpawnRoutine()
        {
            // Visual children are configured during entity initialization, after Bind.
            yield return null;
            if (!TryCaptureVisual())
            {
                _visualRoutine = null;
                yield break;
            }

            const float duration = 0.22f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var scale = t < 0.7f
                    ? Mathf.Lerp(0.72f, 1.08f, EaseOutBack(t / 0.7f))
                    : Mathf.Lerp(1.08f, 1f, (t - 0.7f) / 0.3f);
                _visual.localScale = ScaleFromRest(scale);
                yield return null;
            }

            RestoreVisual();
            _visualRoutine = null;
        }

        private IEnumerator RevealSpawnRoutine()
        {
            // BuildingView finishes configuring its visual later in the creation frame.
            yield return null;
            if (!TryCaptureVisual())
            {
                _visualRoutine = null;
                yield break;
            }

            const float duration = 0.2f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = 1f - (1f - t) * (1f - t);
                var revealColor = new Color(0.62f, 1f, 0.78f, _restColor.a * 0.35f);
                _visualRenderer.color = Color.Lerp(revealColor, _restColor, eased);
                _visual.localPosition = _restPosition;
                _visual.localScale = _restScale;
                yield return null;
            }

            RestoreVisual();
            _visualRoutine = null;
        }

        private IEnumerator HitRoutine()
        {
            const float duration = 0.16f;
            var recoil = ResolveLocalRecoil(0.09f);
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var strength = Mathf.Sin(t * Mathf.PI) * (1f - 0.25f * t);
                _visual.localPosition = _restPosition + recoil * strength;
                _visual.localScale = ScaleFromRest(1f + 0.08f * Mathf.Sin(t * Mathf.PI));
                _visualRenderer.color = Color.Lerp(new Color(1f, 0.22f, 0.12f, _restColor.a), _restColor, t);
                yield return null;
            }

            RestoreVisual();
            _visualRoutine = null;
        }

        private IEnumerator DeathRoutine(Action completed)
        {
            const float duration = 0.24f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = t * t;
                _visual.localPosition = _restPosition + Vector3.up * (0.12f * t);
                _visual.localScale = ScaleFromRest(Mathf.Lerp(1f, 0.25f, eased));
                var color = _restColor;
                color.a = Mathf.Lerp(_restColor.a, 0f, eased);
                _visualRenderer.color = color;
                yield return null;
            }

            _visualRoutine = null;
            completed?.Invoke();
        }

        private bool TryCaptureVisual()
        {
            var owner = transform.parent;
            if (owner == null) return false;
            var provider = owner.GetComponent<IEntityVisualProvider>();
            var renderer = provider != null ? provider.VisualRenderer : owner.GetComponent<SpriteRenderer>();
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) return false;

            _visual = renderer.transform;
            _visualRenderer = renderer;
            _restPosition = _visual.localPosition;
            _restScale = _visual.localScale;
            _restColor = _visualRenderer.color;
            return true;
        }

        private Vector3 ScaleFromRest(float multiplier) => new Vector3(
            _restScale.x * multiplier,
            _restScale.y * multiplier,
            _restScale.z);

        private Vector3 ResolveLocalRecoil(float worldDistance)
        {
            var direction = _hasImpactOrigin ? transform.position - _impactOrigin : Vector3.right;
            direction.z = 0f;
            if (direction.sqrMagnitude <= 0.0001f) direction = Vector3.right;
            direction.Normalize();
            return _visual != null && _visual.parent != null
                ? _visual.parent.InverseTransformVector(direction * worldDistance)
                : direction * worldDistance;
        }

        private void EmitImpactParticles(Vector3 origin)
        {
            if (_particles == null) return;
            var direction = _hasImpactOrigin ? origin - _impactOrigin : Vector3.up;
            direction.z = 0f;
            if (direction.sqrMagnitude <= 0.0001f) direction = Vector3.up;
            direction.Normalize();

            for (var i = 0; i < 18; i++)
            {
                var angle = UnityEngine.Random.Range(-42f, 42f) * Mathf.Deg2Rad;
                var velocityDirection = new Vector3(
                    direction.x * Mathf.Cos(angle) - direction.y * Mathf.Sin(angle),
                    direction.x * Mathf.Sin(angle) + direction.y * Mathf.Cos(angle),
                    0f);
                var parameters = new ParticleSystem.EmitParams
                {
                    position = origin + (Vector3)(UnityEngine.Random.insideUnitCircle * 0.08f),
                    velocity = velocityDirection * UnityEngine.Random.Range(1.2f, 2.5f),
                    startLifetime = UnityEngine.Random.Range(0.22f, 0.38f),
                    startSize = UnityEngine.Random.Range(0.12f, 0.24f),
                    startColor = Color.Lerp(
                        new Color(1f, 0.82f, 0.18f, 1f),
                        new Color(0.95f, 0.18f, 0.04f, 1f),
                        UnityEngine.Random.value)
                };
                _particles.Emit(parameters, 1);
            }
        }

        private void StopVisualRoutine(bool restore)
        {
            if (_visualRoutine != null) StopCoroutine(_visualRoutine);
            _visualRoutine = null;
            if (restore) RestoreVisual();
        }

        private void RestoreVisual()
        {
            if (_visual != null)
            {
                _visual.localPosition = _restPosition;
                _visual.localScale = _restScale;
            }
            if (_visualRenderer != null) _visualRenderer.color = _restColor;
        }

        private static float EaseOutBack(float value)
        {
            const float overshoot = 1.70158f;
            var x = Mathf.Clamp01(value) - 1f;
            return 1f + (overshoot + 1f) * x * x * x + overshoot * x * x;
        }

        private void EnsureParticles()
        {
            if (_particles == null) _particles = GetComponent<ParticleSystem>();
            if (_particles == null) _particles = gameObject.AddComponent<ParticleSystem>();
            _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = _particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.3f;
            main.startLifetime = 0.32f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.24f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.45f, 0.05f, 0.05f, 1f), new Color(0.95f, 0.28f, 0.06f, 1f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = _particles.emission;
            emission.enabled = false;

            var shape = _particles.shape;
            shape.enabled = false;

            var colorOverLifetime = _particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(
                new Gradient
                {
                    colorKeys = new[]
                    {
                        new GradientColorKey(new Color(0.25f, 0.02f, 0.02f), 0f),
                        new GradientColorKey(new Color(0.95f, 0.32f, 0.08f), 0.55f),
                        new GradientColorKey(new Color(0.12f, 0.12f, 0.12f), 1f)
                    },
                    alphaKeys = new[]
                    {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(0.9f, 0.5f),
                        new GradientAlphaKey(0f, 1f)
                    }
                });

            var sizeOverLifetime = _particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.75f, 1f, 1.1f));

            var renderer = _particles.GetComponent<ParticleSystemRenderer>();
            renderer.enabled = true;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = CombatParticleMaterial.Shared;
            renderer.sortingOrder = 60;
        }

        private void Unbind()
        {
            if (_source != null) _source.OnHealthChanged -= HandleHealthChanged;
            _source = null;
            _lastHp = -1;
        }
    }
}
