using UnityEngine;

namespace Panteon.Data
{
    // Sent with the event so feedback can choose the right marker.
    public enum CommandFeedbackType
    {
        Move,
        Attack
    }

    public readonly struct CommandFeedbackRequested
    {
        public readonly Vector3 WorldPosition;
        public readonly CommandFeedbackType Type;

        public CommandFeedbackRequested(Vector3 worldPosition, CommandFeedbackType type)
        {
            WorldPosition = worldPosition;
            Type = type;
        }
    }
}
