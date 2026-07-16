using System.Collections.Generic;
using NUnit.Framework;
using Panteon.Data;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Panteon.Tests
{
    public sealed class UnitAnimationContractTests
    {
        [TestCase(0f, 1f, UnitAnimationDirection.Up)]
        [TestCase(1f, 1f, UnitAnimationDirection.UpRight)]
        [TestCase(1f, 0f, UnitAnimationDirection.Right)]
        [TestCase(-1f, 0f, UnitAnimationDirection.Right)]
        [TestCase(1f, -1f, UnitAnimationDirection.DownRight)]
        [TestCase(0f, -1f, UnitAnimationDirection.Down)]
        public void ResolveDirection_MapsWorldDirectionToSharedAnimationDirection(
            float x, float y, UnitAnimationDirection expected)
        {
            Assert.That(UnitAnimationContract.ResolveDirection(new Vector2(x, y)), Is.EqualTo(expected));
        }

        [Test]
        public void StateHash_UsesStableAndDistinctSemanticPaths()
        {
            var idle = UnitAnimationContract.StateHash("Idle");
            var move = UnitAnimationContract.StateHash("Move");
            Assert.That(idle, Is.Not.Zero);
            Assert.That(move, Is.Not.Zero);
            Assert.That(idle, Is.Not.EqualTo(move));
        }

        [TestCase("Assets/Tiny Swords/Units/Blue Units/Archer/Archer Blue Animations/Archer_Blue.controller")]
        [TestCase("Assets/Tiny Swords/Units/Blue Units/Lancer/Lancer Blue Animations/Lancer_Blue.controller")]
        [TestCase("Assets/Tiny Swords/Units/Blue Units/Monk/Monk Blue Animations/Monk_Blue.controller")]
        [TestCase("Assets/Tiny Swords/Units/Blue Units/Warrior/Warrior Blue Animations/Warrior_Blue.controller")]
        public void ControllerStateNames_MatchTheirAnimationClips(string assetPath)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(assetPath);
            Assert.That(controller, Is.Not.Null, $"Animator Controller is missing at {assetPath}.");

            foreach (var layer in controller.layers)
            foreach (var state in EnumerateStates(layer.stateMachine))
            {
                if (!(state.motion is AnimationClip clip)) continue;
                Assert.That(state.name, Is.EqualTo(clip.name),
                    $"State '{state.name}' must match clip '{clip.name}' in {assetPath}.");
            }
        }

        private static IEnumerable<AnimatorState> EnumerateStates(AnimatorStateMachine stateMachine)
        {
            foreach (var childState in stateMachine.states) yield return childState.state;
            foreach (var childStateMachine in stateMachine.stateMachines)
            foreach (var state in EnumerateStates(childStateMachine.stateMachine))
                yield return state;
        }
    }
}
