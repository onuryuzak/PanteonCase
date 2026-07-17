using System;
using System.Collections;
using Panteon.Core;
using Panteon.Gameplay.Buildings;
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
        private const float CriticalSmokeHealthRatio = 0.35f;
        private ParticleSystem _particles;
        private ParticleSystem _criticalSmokeParticles;
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
        private bool _isStructure;
        private bool _particlesConfigured;
        private float _criticalSmokeSeverity;

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
            var owner = transform.parent;
            _isStructure = owner != null && owner.GetComponent<Building>() != null;
            if (_isStructure) EnsureCriticalSmokeParticles();
            _lastHp = _source.CurrentHP;
            _source.OnHealthChanged += HandleHealthChanged;
            UpdateStructuralSmoke(_source.CurrentHP, _source.MaxHP);
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
            if (_criticalSmokeParticles != null)
                _criticalSmokeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _criticalSmokeSeverity = 0f;
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
            UpdateStructuralSmoke(current, max);
            _lastHp = current;
        }

        private void LateUpdate()
        {
            if (_criticalSmokeSeverity > 0f) UpdateCriticalSmokeOrigin();
        }

        private void Play()
        {
            EnsureParticles();
            StopVisualRoutine(true);
            var hasVisual = TryCaptureVisual();
            var center = hasVisual ? _visualRenderer.bounds.center : transform.position;
            var particleOrigin = hasVisual
                ? ResolveImpactPoint(center, _visualRenderer.bounds.extents)
                : center;
            EmitImpactParticles(particleOrigin);
            if (hasVisual) _visualRoutine = StartCoroutine(HitRoutine());
            _hasImpactOrigin = false;
        }

        private Vector3 ResolveImpactPoint(Vector3 center, Vector3 extents)
        {
            if (!_hasImpactOrigin) return center;
            // Organic targets spray through the far side. Structures emit dust and
            // smoke from the struck face, towards the attacker.
            var surfaceDirection = _isStructure
                ? _impactOrigin - center
                : center - _impactOrigin;
            surfaceDirection.z = 0f;
            if (surfaceDirection.sqrMagnitude <= 0.0001f) return center;
            surfaceDirection.Normalize();

            var distanceX = Mathf.Abs(surfaceDirection.x) > 0.0001f
                ? extents.x / Mathf.Abs(surfaceDirection.x)
                : float.PositiveInfinity;
            var distanceY = Mathf.Abs(surfaceDirection.y) > 0.0001f
                ? extents.y / Mathf.Abs(surfaceDirection.y)
                : float.PositiveInfinity;
            var edgeDistance = Mathf.Min(distanceX, distanceY);
            if (float.IsInfinity(edgeDistance) || float.IsNaN(edgeDistance)) return center;

            return center + surfaceDirection * edgeDistance * 0.92f;
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
            if (!TryCaptureVisual())
            {
                _visualRoutine = null;
                yield break;
            }

            EmitPlacementDust(_visualRenderer.bounds);

            const float duration = 0.28f;
            const float impactPoint = 0.62f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                float scaleX;
                float scaleY;
                if (t < impactPoint)
                {
                    var stamp = 1f - Mathf.Pow(1f - t / impactPoint, 3f);
                    scaleX = Mathf.Lerp(0.84f, 1.07f, stamp);
                    scaleY = Mathf.Lerp(0.62f, 1.1f, stamp);
                }
                else
                {
                    var settle = (t - impactPoint) / (1f - impactPoint);
                    var easedSettle = 1f - Mathf.Pow(1f - settle, 2f);
                    scaleX = Mathf.Lerp(1.07f, 1f, easedSettle);
                    scaleY = Mathf.Lerp(1.1f, 1f, easedSettle);
                }

                var flash = Mathf.Sin(t * Mathf.PI);
                _visualRenderer.color = Color.Lerp(
                    _restColor,
                    new Color(1f, 0.88f, 0.58f, _restColor.a),
                    flash * 0.62f);
                _visual.localPosition = _restPosition + Vector3.down * (0.08f * (1f - t));
                _visual.localScale = new Vector3(
                    _restScale.x * scaleX,
                    _restScale.y * scaleY,
                    _restScale.z);
                yield return null;
            }

            RestoreVisual();
            _visualRoutine = null;
        }

        private void EmitPlacementDust(Bounds bounds)
        {
            if (_particles == null) return;
            var baseY = bounds.min.y + bounds.extents.y * 0.06f;
            var horizontalExtent = Mathf.Max(0.2f, bounds.extents.x * 0.92f);
            for (var i = 0; i < 28; i++)
            {
                var offsetX = UnityEngine.Random.Range(-horizontalExtent, horizontalExtent);
                var outwardSign = Mathf.Abs(offsetX) > 0.03f ? Mathf.Sign(offsetX) : (UnityEngine.Random.value < 0.5f ? -1f : 1f);
                var velocity = new Vector3(
                    outwardSign * UnityEngine.Random.Range(0.45f, 1.25f),
                    UnityEngine.Random.Range(0.18f, 0.58f),
                    0f);
                var parameters = new ParticleSystem.EmitParams
                {
                    position = new Vector3(bounds.center.x + offsetX, baseY, bounds.center.z),
                    velocity = velocity,
                    startLifetime = UnityEngine.Random.Range(0.38f, 0.72f),
                    startSize = UnityEngine.Random.Range(0.18f, 0.38f),
                    startColor = Color.Lerp(
                        new Color(0.56f, 0.48f, 0.35f, 0.9f),
                        new Color(0.25f, 0.27f, 0.25f, 0.82f),
                        UnityEngine.Random.value)
                };
                _particles.Emit(parameters, 1);
            }
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
                var impactColor = _isStructure
                    ? new Color(1f, 0.72f, 0.32f, _restColor.a)
                    : new Color(1f, 0.22f, 0.12f, _restColor.a);
                _visualRenderer.color = Color.Lerp(impactColor, _restColor, t);
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
            if (_isStructure)
            {
                EmitStructureImpact(origin);
                return;
            }

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

        private void EmitStructureImpact(Vector3 origin, int particleCount = 18, bool useImpactDirection = true)
        {
            var awayFromImpact = useImpactDirection && _hasImpactOrigin
                ? origin - _impactOrigin
                : Vector3.up;
            awayFromImpact.z = 0f;
            if (awayFromImpact.sqrMagnitude <= 0.0001f) awayFromImpact = Vector3.up;
            awayFromImpact.Normalize();

            for (var i = 0; i < particleCount; i++)
            {
                var drift = Vector3.up * UnityEngine.Random.Range(0.45f, 1.1f) +
                            awayFromImpact * UnityEngine.Random.Range(0.12f, 0.42f) +
                            (Vector3)(UnityEngine.Random.insideUnitCircle * 0.34f);
                var parameters = new ParticleSystem.EmitParams
                {
                    position = origin + (Vector3)(UnityEngine.Random.insideUnitCircle * 0.12f),
                    velocity = drift,
                    startLifetime = UnityEngine.Random.Range(0.45f, 0.78f),
                    startSize = UnityEngine.Random.Range(0.24f, 0.46f),
                    startColor = Color.Lerp(
                        new Color(0.48f, 0.43f, 0.36f, 0.9f),
                        new Color(0.2f, 0.22f, 0.22f, 0.82f),
                        UnityEngine.Random.value)
                };
                _particles.Emit(parameters, 1);
            }
        }

        private void UpdateStructuralSmoke(int current, int max)
        {
            if (!_isStructure || _criticalSmokeParticles == null)
            {
                _criticalSmokeSeverity = 0f;
                return;
            }

            var normalized = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            var severity = current > 0
                ? Mathf.InverseLerp(CriticalSmokeHealthRatio, 0.05f, normalized)
                : 0f;
            if (severity <= 0f)
            {
                var emission = _criticalSmokeParticles.emission;
                emission.rateOverTime = 0f;
                if (_criticalSmokeParticles.isPlaying)
                    _criticalSmokeParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                _criticalSmokeSeverity = 0f;
                return;
            }

            UpdateCriticalSmokeOrigin();
            var criticalEmission = _criticalSmokeParticles.emission;
            criticalEmission.rateOverTime = Mathf.Lerp(8f, 28f, Mathf.Pow(severity, 1.25f));
            if (!_criticalSmokeParticles.isPlaying)
            {
                _criticalSmokeParticles.Play(true);
                _criticalSmokeParticles.Emit(Mathf.RoundToInt(Mathf.Lerp(5f, 10f, severity)));
            }
            _criticalSmokeSeverity = severity;
        }

        private void UpdateCriticalSmokeOrigin()
        {
            if (_criticalSmokeParticles == null) return;
            var owner = transform.parent;
            if (owner == null) return;
            var provider = owner.GetComponent<IEntityVisualProvider>();
            var renderer = provider != null ? provider.VisualRenderer : owner.GetComponent<SpriteRenderer>();
            if (renderer == null || !renderer.enabled) return;
            var bounds = renderer.bounds;
            _criticalSmokeParticles.transform.position =
                bounds.center + Vector3.up * (bounds.extents.y * 0.62f);
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
            if (_particlesConfigured && _particles != null) return;
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
            main.maxParticles = 180;

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
                        new GradientColorKey(Color.white, 0f),
                        new GradientColorKey(new Color(0.92f, 0.92f, 0.92f), 0.55f),
                        new GradientColorKey(new Color(0.55f, 0.55f, 0.55f), 1f)
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
            _particlesConfigured = true;
        }

        private void EnsureCriticalSmokeParticles()
        {
            if (_criticalSmokeParticles != null) return;
            var smokeRoot = transform.Find("CriticalSmoke");
            if (smokeRoot == null)
            {
                smokeRoot = new GameObject("CriticalSmoke").transform;
                smokeRoot.SetParent(transform, false);
            }

            _criticalSmokeParticles = smokeRoot.GetComponent<ParticleSystem>();
            if (_criticalSmokeParticles == null)
                _criticalSmokeParticles = smokeRoot.gameObject.AddComponent<ParticleSystem>();
            _criticalSmokeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = _criticalSmokeParticles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 1f;
            main.maxParticles = 80;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.5f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.28f, 0.55f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.44f, 0.41f, 0.36f, 0.92f),
                new Color(0.13f, 0.15f, 0.15f, 0.94f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = _criticalSmokeParticles.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;

            var shape = _criticalSmokeParticles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.24f;

            var velocity = _criticalSmokeParticles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            // Unity validates the three axes after each assignment, so keep all
            // curves in Constant mode. The noise module supplies lateral variance.
            velocity.x = 0f;
            velocity.y = 0.32f;
            velocity.z = 0f;

            var noise = _criticalSmokeParticles.noise;
            noise.enabled = true;
            noise.strength = 0.15f;
            noise.frequency = 0.65f;
            noise.scrollSpeed = 0.22f;
            noise.damping = true;

            var colorOverLifetime = _criticalSmokeParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(
                new Gradient
                {
                    colorKeys = new[]
                    {
                        new GradientColorKey(new Color(0.56f, 0.52f, 0.44f), 0f),
                        new GradientColorKey(new Color(0.24f, 0.26f, 0.25f), 0.5f),
                        new GradientColorKey(new Color(0.09f, 0.1f, 0.1f), 1f)
                    },
                    alphaKeys = new[]
                    {
                        new GradientAlphaKey(0.2f, 0f),
                        new GradientAlphaKey(0.92f, 0.12f),
                        new GradientAlphaKey(0.62f, 0.68f),
                        new GradientAlphaKey(0f, 1f)
                    }
                });

            var sizeOverLifetime = _criticalSmokeParticles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
                1f,
                new AnimationCurve(
                    new Keyframe(0f, 0.72f),
                    new Keyframe(0.38f, 1.18f),
                    new Keyframe(1f, 1.55f)));

            var particleRenderer = _criticalSmokeParticles.GetComponent<ParticleSystemRenderer>();
            particleRenderer.enabled = true;
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.sharedMaterial = CombatParticleMaterial.Shared;
            particleRenderer.sortingOrder = 59;
        }

        private void Unbind()
        {
            if (_source != null) _source.OnHealthChanged -= HandleHealthChanged;
            _source = null;
            _lastHp = -1;
            _isStructure = false;
        }
    }
}
