using UnityEngine;

namespace PixelFlowClone.UI.Screens
{
    public readonly struct TapPulseFrame
    {
        public TapPulseFrame(float press, float circleScale, float circleAlpha)
        {
            Press = press;
            CircleScale = circleScale;
            CircleAlpha = circleAlpha;
        }

        public float Press { get; }
        public float CircleScale { get; }
        public float CircleAlpha { get; }
    }

    public static class TapPulseAnimation
    {
        public const float DefaultDurationSeconds = 1.5f;

        private const float MinimumDurationSeconds = 0.1f;
        private const float MinimumCircleScale = 0.72f;
        private const float MaximumCircleScale = 1.35f;
        private const float MaximumCircleAlpha = 0.62f;

        /// <summary>
        /// Samples synchronized finger compression and circle feedback for a looping cycle.
        /// </summary>
        public static TapPulseFrame Evaluate(float elapsedSeconds, float durationSeconds)
        {
            float duration = Mathf.Max(MinimumDurationSeconds, durationSeconds);
            float normalizedPhase = Mathf.Repeat(elapsedSeconds, duration) / duration;
            float press = (1f - Mathf.Cos(normalizedPhase * Mathf.PI * 2f)) * 0.5f;

            float circleScale = Mathf.Lerp(MaximumCircleScale, MinimumCircleScale, press);
            float circleAlpha = MaximumCircleAlpha * press;

            return new TapPulseFrame(press, circleScale, circleAlpha);
        }
    }
}
