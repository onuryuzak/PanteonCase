using System.Collections;
using Panteon.Data;
using UnityEngine;

namespace Panteon.Gameplay.Units
{
    [DisallowMultipleComponent]
    public sealed class UnitAnimatorView : MonoBehaviour
    {
        private const string VisualName = "TinySwordsVisual";
        private UnitVisualProfileSO _profile;
        private UnitAnimationMap _animations;
        private UnitStateMachine _stateMachine;
        private SpriteRenderer _legacyRenderer;
        private SpriteRenderer _renderer;
        private Animator _animator;
        private Coroutine _hitRoutine;
        private bool _useSecondaryAttack;
        private int _currentState;

        public float AttackImpactDelay => _profile != null ? _profile.AttackImpactDelay : 0f;
        public Sprite ProjectileSprite => _profile != null ? _profile.ProjectileSprite : null;
        public bool UsesAttackFire => _profile != null && _profile.UsesAttackFire;

        public static UnitAnimatorView Ensure(GameObject owner)
        {
            var view = owner.GetComponent<UnitAnimatorView>();
            return view != null ? view : owner.AddComponent<UnitAnimatorView>();
        }

        public void Initialize(UnitVisualProfileSO profile, UnitStateMachine stateMachine)
        {
            Release();
            _profile = profile;
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

        private IEnumerator PlayHitThenResume(int state)
        {
            Play(state, true);
            yield return new WaitForSeconds(_profile.HitAnimationDuration);
            _hitRoutine = null;
            PlayForUnitState(_stateMachine != null ? _stateMachine.Current : UnitState.Idle);
        }

        private void HandleStateChanged(UnitState previous, UnitState next)
        {
            if (next != UnitState.Attacking) PlayForUnitState(next);
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
            var visual = transform.Find(VisualName);
            if (visual == null)
            {
                visual = new GameObject(VisualName).transform;
                visual.SetParent(transform, false);
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
    }
}
