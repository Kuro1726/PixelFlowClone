using UnityEngine;

namespace PixelFlowClone.Entities
{
    public readonly struct CollectorShotShakeFrame
    {
        public CollectorShotShakeFrame(Vector2 localOffset, bool isComplete)
        {
            LocalOffset = localOffset;
            IsComplete = isComplete;
        }

        public Vector2 LocalOffset { get; }
        public bool IsComplete { get; }
    }

    public static class CollectorShotShakeAnimation
    {
        public const float DefaultDurationSeconds = 0.12f;
        public const float DefaultAmplitude = 0.045f;
        public const float DefaultOscillations = 3f;

        private const float MinimumDurationSeconds = 0.01f;
        private const float MinimumOscillations = 1f;
        private const float BackwardWeight = 0.9f;
        private const float LateralWeight = 0.35f;

        /// <summary>
        /// Samples a damped local-space recoil. Local down is opposite the collector's firing direction.
        /// </summary>
        public static CollectorShotShakeFrame Evaluate(
            float elapsedSeconds,
            float durationSeconds,
            float amplitude,
            float oscillations)
        {
            float duration = Mathf.Max(MinimumDurationSeconds, durationSeconds);
            if (elapsedSeconds >= duration)
                return new CollectorShotShakeFrame(Vector2.zero, true);

            float normalizedTime = Mathf.Clamp01(elapsedSeconds / duration);
            float damping = 1f - normalizedTime;
            float safeAmplitude = Mathf.Max(0f, amplitude);
            float safeOscillations = Mathf.Max(MinimumOscillations, oscillations);
            float wave = Mathf.Sin(normalizedTime * safeOscillations * Mathf.PI * 2f);

            var localOffset = new Vector2(
                wave * safeAmplitude * LateralWeight * damping,
                -Mathf.Abs(wave) * safeAmplitude * BackwardWeight * damping);
            return new CollectorShotShakeFrame(localOffset, false);
        }
    }
}
