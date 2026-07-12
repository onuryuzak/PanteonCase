using Panteon.Core;
using UnityEngine;

namespace Panteon.Gameplay.Combat
{
    public sealed class DamageFeedbackView : MonoBehaviour
    {
        private ParticleSystem _particles;
        private IDamageable _source;
        private int _lastHp = -1;

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
            _source = source;
            if (_source == null) return;
            _lastHp = _source.CurrentHP;
            _source.OnHealthChanged += HandleHealthChanged;
        }

        private void OnDisable() => Unbind();

        private void HandleHealthChanged(int current, int max)
        {
            if (_lastHp >= 0 && current < _lastHp) Play();
            _lastHp = current;
        }

        private void Play()
        {
            EnsureParticles();
            _particles.Emit(14);
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
