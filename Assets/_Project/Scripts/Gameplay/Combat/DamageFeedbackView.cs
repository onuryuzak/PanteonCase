using System;
using System.Collections;
using Panteon.Core;
using UnityEngine;

namespace Panteon.Gameplay.Combat
{
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
            if (_lastHp >= 0 && current < _lastHp) Play();
            _lastHp = current;
        }

        private void Play()
        {
            EnsureParticles();
            _particles.Emit(14);
            StopVisualRoutine(true);
            if (TryCaptureVisual()) _visualRoutine = StartCoroutine(HitRoutine());
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
            const float duration = 0.13f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var strength = 1f - t;
                var direction = Mathf.Sin(t * Mathf.PI * 4f);
                _visual.localPosition = _restPosition + Vector3.right * (0.055f * direction * strength);
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
            var candidate = owner.Find("TinySwordsVisual") ?? owner.Find("BuildingVisual");
            var renderer = candidate != null
                ? candidate.GetComponent<SpriteRenderer>()
                : owner.GetComponent<SpriteRenderer>();
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
            main.startSpeed = 1.8f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.45f, 0.05f, 0.05f, 1f), new Color(0.95f, 0.28f, 0.06f, 1f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = _particles.emission;
            emission.enabled = false;

            var shape = _particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.24f;

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
