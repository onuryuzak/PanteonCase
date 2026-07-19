using UnityEngine;

namespace Panteon.Data
{
    public readonly struct CameraShakeRequested
    {
        public readonly float Amplitude;
        public readonly float Duration;

        public CameraShakeRequested(float amplitude, float duration)
        {
            Amplitude = Mathf.Max(0f, amplitude);
            Duration = Mathf.Max(0f, duration);
        }
    }
}
