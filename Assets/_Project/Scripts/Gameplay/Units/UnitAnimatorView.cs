using System;
using System.Collections;
using Panteon.Data;
using Panteon.Gameplay.Combat;
using UnityEngine;

namespace Panteon.Gameplay.Units
{
    [DisallowMultipleComponent]
    // Bridges UnitState to the Tiny Swords Animator.
    public sealed class UnitAnimatorView : MonoBehaviour, IEntityVisualProvider
    {
        private const string VisualName = "TinySwordsVisual";
        private const string MotionRootName = "UnitVisualMotion";
        private UnitVisualProfileSO _profile;
        private UnitAnimationMap _animations;
        private UnitStateMachine _stateMachine;
        private SpriteRenderer _legacyRenderer;
        private SpriteRenderer _renderer;
        private Animator _animator;
        private Coroutine _hitRoutine;
        private Coroutine _anticipationRoutine;
        private Coroutine _impactPauseRoutine;
        private Coroutine _deathRoutine;
        private Transform _motionRoot;
        private float _animatorResumeSpeed = 1f;
        private bool _animatorPaused;
        private Color _baseVisualColor = Color.white;
        private bool _useSecondaryAttack;
        private int _currentState;

        public float AttackImpactDelay => _profile != null ? _profile.AttackImpactDelay : 0f;
        public Sprite ProjectileSprite => _profile != null ? _profile.ProjectileSprite : null;
        public bool UsesAttackFire => _profile != null && _profile.UsesAttackFire;
        public SpriteRenderer VisualRenderer =>
            _renderer != null && _renderer.gameObject.activeSelf ? _renderer : _legacyRenderer;

        public static UnitAnimatorView Ensure(GameObject owner)
        {
            var view = owner.GetComponent<UnitAnimatorView>();
            return view != null ? view : owner.AddComponent<UnitAnimatorView>();
        }

        public void Initialize(UnitVisualProfileSO profile, UnitStateMachine stateMachine)
        {
            Release();
            _profile = profile;
            // Cache state hashes once; animation playback uses them often.
            _animations = profile != null ? UnitAnimationMap.Create(profile.Controller) : null;
            _stateMachine = stateMachine;
            EnsureVisual();
            var hasProfile = _profile != null && _profile.Controller != null && _animations != null && _animations.IsValid;
            if (_legacyRenderer != null) _legacyRenderer.enabled = !hasProfile;
            _renderer.gameObject.SetActive(hasProfile);
            if (!hasProfile) return;

            _animator.runtimeAnimatorController = _profile.Controller;
            _stateMachine.OnStateChanged += HandleStateChanged;
            Play(_animations.IdleHash);
            ApplyScale();
        }

        public void SetFacing(float horizontalDirection)
        {
            if (_renderer == null || Mathf.Abs(horizontalDirection) < 0.001f) return;
            _renderer.flipX = horizontalDirection < 0f;
        }

        public void PlayAttack(Vector3 targetWorldPosition)
        {
            if (_profile == null) return;
            var direction = targetWorldPosition - transform.position;
            SetFacing(direction.x);
            var state = _useSecondaryAttack && _animations.HasSecondaryAttack
                ? _animations.SecondaryAttackHash
                : _animations.ResolveAttackHash(direction);
            if (_animations.HasSecondaryAttack) _useSecondaryAttack = !_useSecondaryAttack;
            Play(state, true);
        }

        public void PlayAnticipation(Vector3 targetWorldPosition, float duration)
        {
            if (_motionRoot == null || duration <= 0f) return;
            SetFacing(targetWorldPosition.x - transform.position.x);
            StopAnticipation(true);
            _anticipationRoutine = StartCoroutine(AnticipationRoutine(targetWorldPosition, duration));
        }

        public void PlayImpactPause(float duration)
        {
            if (_animator == null || !_animator.isActiveAndEnabled || duration <= 0f) return;
            StopImpactPause();
            _impactPauseRoutine = StartCoroutine(ImpactPauseRoutine(duration));
        }

        public void CancelCombatFeedback()
        {
            StopAnticipation(true);
            StopImpactPause();
        }

        public bool PlayDeath(Action completed)
        {
            var visual = VisualRenderer;
            if (visual == null || _motionRoot == null) return false;
            CancelCombatFeedback();
            if (_hitRoutine != null) StopCoroutine(_hitRoutine);
            _hitRoutine = null;
            StopDeathRoutine(true);
            _deathRoutine = StartCoroutine(DeathRoutine(completed));
            return true;
        }

        public void PlayHit()
        {
            if (_profile == null) return;
            if (_animations == null || !_animations.HasHit) return;
            var state = _animations.ResolveHitHash(
                _renderer != null && _renderer.flipX ? Vector2.left : Vector2.right);
            if (_hitRoutine != null) StopCoroutine(_hitRoutine);
            _hitRoutine = StartCoroutine(PlayHitThenResume(state));
        }

        public void Release()
        {
            CancelCombatFeedback();
            StopDeathRoutine(true);
            if (_stateMachine != null) _stateMachine.OnStateChanged -= HandleStateChanged;
            _stateMachine = null;
            _animations = null;
            if (_hitRoutine != null) StopCoroutine(_hitRoutine);
            _hitRoutine = null;
            _useSecondaryAttack = false;
            _currentState = 0;
            if (_animator != null) _animator.runtimeAnimatorController = null;
            if (_renderer != null) _renderer.gameObject.SetActive(false);
        }

        private IEnumerator DeathRoutine(Action completed)
        {
            if (_animations != null && _animations.HasDeath)
            {
                var deathHash = _animations.DeathHash;
                var previousCullingMode = _animator.cullingMode;
                _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                _animator.speed = 1f;
                Play(deathHash, true);

                // Clip length is unreliable here; wait for the Animator's last frame.
                var timeout = Mathf.Max(0.5f, _animations.DeathDuration * 3f);
                var elapsed = 0f;
                while (elapsed < timeout)
                {
                    var state = _animator.GetCurrentAnimatorStateInfo(0);
                    if (state.fullPathHash == deathHash && state.normalizedTime >= 1f) break;
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                // Give Unity one frame to draw the final death pose.
                yield return null;
                _animator.cullingMode = previousCullingMode;
            }
            else
            {
                if (_animator != null) _animator.speed = 0f;
                const float duration = 0.58f;
                var elapsed = 0f;
                var fallDirection = _renderer != null && _renderer.flipX ? 1f : -1f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    var t = Mathf.Clamp01(elapsed / duration);
                    var lift = Mathf.Sin(Mathf.Min(1f, t * 1.8f) * Mathf.PI) * 0.08f;
                    var fall = Mathf.Max(0f, t - 0.28f) / 0.72f;
                    _motionRoot.localPosition = new Vector3(
                        fallDirection * 0.09f * t,
                        lift - 0.15f * fall * fall,
                        0f);
                    _motionRoot.localRotation = Quaternion.Euler(0f, 0f, fallDirection * 78f * fall);
                    _motionRoot.localScale = new Vector3(
                        Mathf.Lerp(1f, 1.12f, fall),
                        Mathf.Lerp(1f, 0.42f, fall),
                        1f);

                    yield return null;
                }

                // Pooling hides the object next, so keep the death pose opaque here.
                yield return null;
            }

            _deathRoutine = null;
            completed?.Invoke();
        }

        private IEnumerator AnticipationRoutine(Vector3 targetWorldPosition, float duration)
        {
            var direction = targetWorldPosition - transform.position;
            direction.z = 0f;
            if (direction.sqrMagnitude <= 0.0001f) direction = Vector3.right;
            direction = transform.InverseTransformDirection(direction.normalized);
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = t * t * (3f - 2f * t);
                _motionRoot.localPosition = -direction * (0.065f * eased);
                _motionRoot.localScale = new Vector3(
                    Mathf.Lerp(1f, 1.05f, eased),
                    Mathf.Lerp(1f, 0.92f, eased),
                    1f);
                yield return null;
            }

            ResetMotionRoot();
            _anticipationRoutine = null;
        }

        private IEnumerator ImpactPauseRoutine(float duration)
        {
            _animatorResumeSpeed = _animator.speed;
            _animator.speed = 0f;
            _animatorPaused = true;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            RestoreAnimatorSpeed();
            _impactPauseRoutine = null;
        }

        private IEnumerator PlayHitThenResume(int state)
        {
            Play(state, true);
            yield return new WaitForSeconds(_profile.HitAnimationDuration);
            _hitRoutine = null;
            PlayForUnitState(_stateMachine != null ? _stateMachine.Current : UnitState.Idle);
        }

        private void HandleStateChanged(UnitState previous, UnitState next)
        {
            if (next != UnitState.Attacking && next != UnitState.Dead) PlayForUnitState(next);
        }

        private void PlayForUnitState(UnitState state)
        {
            if (_profile == null) return;
            if (_animations == null) return;
            Play(state == UnitState.Moving ? _animations.MoveHash : _animations.IdleHash);
        }

        private void Play(int stateHash, bool restart = false)
        {
            if (_animator == null || stateHash == 0) return;
            // Optional clips are not guaranteed, so fall back to idle quietly.
            if (!_animator.HasState(0, stateHash))
            {
                var idleHash = _animations != null ? _animations.IdleHash : 0;
                if (idleHash == 0 || !_animator.HasState(0, idleHash)) return;
                stateHash = idleHash;
                restart = false;
            }

            if (!restart && _currentState == stateHash) return;
            _currentState = stateHash;
            _animator.Play(stateHash, 0, 0f);
            _animator.Update(0f);
        }

        private void EnsureVisual()
        {
            if (_renderer != null) return;
            _legacyRenderer = GetComponent<SpriteRenderer>();
            _motionRoot = transform.Find(MotionRootName);
            if (_motionRoot == null)
            {
                _motionRoot = new GameObject(MotionRootName).transform;
                _motionRoot.SetParent(transform, false);
            }

            var visual = _motionRoot.Find(VisualName);
            if (visual == null)
            {
                visual = transform.Find(VisualName);
                if (visual == null) visual = new GameObject(VisualName).transform;
                visual.SetParent(_motionRoot, false);
            }

            _renderer = visual.GetComponent<SpriteRenderer>();
            if (_renderer == null) _renderer = visual.gameObject.AddComponent<SpriteRenderer>();
            _animator = visual.GetComponent<Animator>();
            if (_animator == null) _animator = visual.gameObject.AddComponent<Animator>();
            _animator.applyRootMotion = false;
            _animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            if (_legacyRenderer == null) return;
            _renderer.sharedMaterial = _legacyRenderer.sharedMaterial;
            _renderer.sortingLayerID = _legacyRenderer.sortingLayerID;
            _renderer.sortingOrder = _legacyRenderer.sortingOrder;
            _renderer.color = _legacyRenderer.color;
            _baseVisualColor = _renderer.color;
            ResetMotionRoot();
        }

        private void ApplyScale()
        {
            var sprite = _profile.ReferenceSprite != null ? _profile.ReferenceSprite : _renderer.sprite;
            if (sprite == null || sprite.bounds.size.y <= 0f) return;
            var visibleHeight = sprite.bounds.size.y * Mathf.Max(0.01f, _profile.ContentHeightRatio);
            var scale = _profile.VisualHeightInCells / visibleHeight;
            _renderer.transform.localPosition = Vector3.zero;
            _renderer.transform.localRotation = Quaternion.identity;
            _renderer.transform.localScale = new Vector3(scale, scale, 1f);
        }

        private void StopAnticipation(bool restore)
        {
            if (_anticipationRoutine != null) StopCoroutine(_anticipationRoutine);
            _anticipationRoutine = null;
            if (restore) ResetMotionRoot();
        }

        private void StopImpactPause()
        {
            if (_impactPauseRoutine != null) StopCoroutine(_impactPauseRoutine);
            _impactPauseRoutine = null;
            RestoreAnimatorSpeed();
        }

        private void RestoreAnimatorSpeed()
        {
            if (!_animatorPaused) return;
            if (_animator != null) _animator.speed = _animatorResumeSpeed;
            _animatorPaused = false;
            _animatorResumeSpeed = 1f;
        }

        private void ResetMotionRoot()
        {
            if (_motionRoot == null) return;
            _motionRoot.localPosition = Vector3.zero;
            _motionRoot.localRotation = Quaternion.identity;
            _motionRoot.localScale = Vector3.one;
        }

        private void StopDeathRoutine(bool restore)
        {
            if (_deathRoutine != null) StopCoroutine(_deathRoutine);
            _deathRoutine = null;
            if (!restore) return;
            ResetMotionRoot();
            if (_renderer != null) _renderer.color = _baseVisualColor;
            if (_animator != null && !_animatorPaused) _animator.speed = 1f;
        }
    }
}
