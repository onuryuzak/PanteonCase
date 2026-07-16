using Panteon.Core;
using Panteon.Data;
using UnityEngine;

namespace Panteon.Gameplay.Feedback
{
    [DisallowMultipleComponent]
    public sealed class GameplayFeedbackController : MonoBehaviour
    {
        private EventBus _bus;
        private CommandFeedbackPool _commands;

        public static GameplayFeedbackController Ensure(GameObject owner)
        {
            var existing = owner.GetComponent<GameplayFeedbackController>();
            return existing != null ? existing : owner.AddComponent<GameplayFeedbackController>();
        }

        public void Configure(EventBus bus, float cellSize)
        {
            Unsubscribe();
            _bus = bus;
            _commands = _commands ?? new CommandFeedbackPool(transform, cellSize);
            if (_bus == null) return;
            _bus.Subscribe<CommandFeedbackRequested>(HandleCommand);
        }

        private void Update()
        {
            _commands?.Tick(Time.unscaledDeltaTime);
        }

        private void OnDestroy() => Unsubscribe();

        private void HandleCommand(CommandFeedbackRequested message)
        {
            _commands?.Show(message.WorldPosition, message.Type);
        }

        private void Unsubscribe()
        {
            if (_bus == null) return;
            _bus.Unsubscribe<CommandFeedbackRequested>(HandleCommand);
            _bus = null;
        }
    }
}
