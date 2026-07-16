using UnityEngine;

namespace Panteon.Gameplay.Combat
{
    public sealed class AttackFireFeedbackView : MonoBehaviour
    {
        private ParticleSystem _particles;

        public static AttackFireFeedbackView Ensure(GameObject owner)
        {
            var existing = owner.GetComponentInChildren<AttackFireFeedbackView>(true);
            if (existing != null)
            {
                existing.EnsureParticles();
                return existing;
            }

            var root = new GameObject("AttackFireFeedback");
            root.transform.SetParent(owner.transform, false);
            root.transform.localPosition = Vector3.zero;
            var view = root.AddComponent<AttackFireFeedbackView>();
            view.EnsureParticles();
            return view;
        }

        public void PlayTowards(Vector3 targetWorldPosition)
        {
            EnsureParticles();
            var direction = (targetWorldPosition - transform.position).normalized;
            if (direction.sqrMagnitude <= 0.0001f) direction = Vector3.right;

            var shape = _particles.shape;
            shape.rotation = new Vector3(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            _particles.Emit(10);
        }

        private void EnsureParticles()
        {
            if (_particles == null) _particles = GetComponent<ParticleSystem>();
            if (_particles == null) _particles = gameObject.AddComponent<ParticleSystem>();
            _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = _particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.18f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.22f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.8f, 4.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.13f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.2f, 0.12f, 0.02f, 1f), new Color(1f, 0.45f, 0.08f, 1f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = _particles.emission;
            emission.enabled = false;

            var shape = _particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 10f;
            shape.radius = 0.03f;

            var colorOverLifetime = _particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(
                new Gradient
                {
                    colorKeys = new[]
                    {
                        new GradientColorKey(new Color(1f, 0.9f, 0.45f), 0f),
                        new GradientColorKey(new Color(1f, 0.42f, 0.08f), 0.45f),
                        new GradientColorKey(new Color(0.18f, 0.12f, 0.12f), 1f)
                    },
                    alphaKeys = new[]
                    {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(0.85f, 0.45f),
                        new GradientAlphaKey(0f, 1f)
                    }
                });

            var renderer = _particles.GetComponent<ParticleSystemRenderer>();
            renderer.enabled = true;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = CombatParticleMaterial.Shared;
            renderer.sortingOrder = 65;
        }
    }
}
