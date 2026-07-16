using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Panteon.Data
{
    public enum UnitAnimationDirection
    {
        Up,
        UpRight,
        Right,
        DownRight,
        Down
    }

    public static class UnitAnimationContract
    {
        public static UnitAnimationDirection ResolveDirection(Vector2 direction)
        {
            var x = Mathf.Abs(direction.x);
            var y = direction.y;
            if (x < 0.25f) return y >= 0f ? UnitAnimationDirection.Up : UnitAnimationDirection.Down;
            if (y > x * 0.45f) return UnitAnimationDirection.UpRight;
            if (y < -x * 0.45f) return UnitAnimationDirection.DownRight;
            return UnitAnimationDirection.Right;
        }

        public static int StateHash(string stateName) => Animator.StringToHash($"Base Layer.{stateName}");
    }

    public sealed class UnitAnimationMap
    {
        private readonly int[] _attackHashes;
        private readonly int[] _hitHashes;

        private UnitAnimationMap(int idleHash, int moveHash, int secondaryAttackHash,
            int[] attackHashes, int[] hitHashes, bool hasHit)
        {
            IdleHash = idleHash;
            MoveHash = moveHash;
            SecondaryAttackHash = secondaryAttackHash;
            _attackHashes = attackHashes;
            _hitHashes = hitHashes;
            HasHit = hasHit;
        }

        public int IdleHash { get; }
        public int MoveHash { get; }
        public int SecondaryAttackHash { get; }
        public bool HasSecondaryAttack => SecondaryAttackHash != 0;
        public bool HasHit { get; }
        public bool IsValid => IdleHash != 0;

        public int ResolveAttackHash(Vector2 direction) => _attackHashes[(int)UnitAnimationContract.ResolveDirection(direction)];

        public int ResolveHitHash(Vector2 direction) => _hitHashes[(int)UnitAnimationContract.ResolveDirection(direction)];

        public static UnitAnimationMap Create(RuntimeAnimatorController controller)
        {
            var clips = controller != null
                ? controller.animationClips.Where(clip => clip != null).Distinct().OrderBy(clip => clip.name).ToList()
                : new List<AnimationClip>();
            var idle = clips.FirstOrDefault(clip => Normalize(clip.name).Contains("idle"));
            if (idle == null) return Empty();

            var move = clips.FirstOrDefault(clip => ContainsAny(Normalize(clip.name), "run", "walk", "move")) ?? idle;
            var secondary = clips.FirstOrDefault(clip => IsSecondaryAttack(Normalize(clip.name)));
            var attacks = clips.Where(clip => IsPrimaryAttack(Normalize(clip.name))).ToList();
            var hits = clips.Where(clip => IsHit(Normalize(clip.name))).ToList();
            var attackHashes = ResolveDirections(attacks, idle);
            var hitHashes = ResolveDirections(hits, idle);

            return new UnitAnimationMap(
                StateHash(idle),
                StateHash(move),
                secondary != null ? StateHash(secondary) : 0,
                attackHashes,
                hitHashes,
                hits.Count > 0);
        }

        private static UnitAnimationMap Empty() => new UnitAnimationMap(0, 0, 0, new int[5], new int[5], false);

        private static int[] ResolveDirections(IReadOnlyList<AnimationClip> clips, AnimationClip fallback)
        {
            var resolved = new AnimationClip[5];
            foreach (var clip in clips)
            {
                var direction = DetectDirection(Normalize(clip.name));
                if (direction.HasValue) resolved[(int)direction.Value] = clip;
            }

            var common = resolved[(int)UnitAnimationDirection.Right] ?? clips.FirstOrDefault() ?? fallback;
            var hashes = new int[resolved.Length];
            for (var i = 0; i < resolved.Length; i++) hashes[i] = StateHash(resolved[i] ?? common);
            return hashes;
        }

        private static int StateHash(AnimationClip clip) => clip != null ? UnitAnimationContract.StateHash(clip.name) : 0;

        private static UnitAnimationDirection? DetectDirection(string name)
        {
            if (name.Contains("upright")) return UnitAnimationDirection.UpRight;
            if (name.Contains("downright")) return UnitAnimationDirection.DownRight;
            if (name.Contains("right")) return UnitAnimationDirection.Right;
            if (name.Contains("down")) return UnitAnimationDirection.Down;
            if (name.Contains("up")) return UnitAnimationDirection.Up;
            return null;
        }

        private static bool IsPrimaryAttack(string name) =>
            ContainsAny(name, "attack", "shoot", "heal") && !IsSecondaryAttack(name);

        private static bool IsSecondaryAttack(string name) =>
            ContainsAny(name, "attack2", "attack02", "secondaryattack");

        private static bool IsHit(string name) =>
            ContainsAny(name, "hit", "hurt", "damage", "guard", "defence", "defense");

        private static string Normalize(string value) =>
            new string(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

        private static bool ContainsAny(string value, params string[] candidates) =>
            candidates.Any(value.Contains);
    }
}
