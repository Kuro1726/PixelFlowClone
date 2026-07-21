using UnityEngine;

namespace PixelFlowClone.Utils
{
    /// <summary>
    /// Simple anti-spam gate for taps (P3-16). Default cooldown is 0.15s.
    /// Uses unscaled time so pause / timeScale does not stretch the gate window unexpectedly.
    /// </summary>
    public sealed class TapCooldownGate
    {
        public const float DefaultCooldownSeconds = 0.15f;

        private float _cooldownSeconds;
        private float _nextAllowedUnscaledTime;

        public float CooldownSeconds => _cooldownSeconds;

        public bool IsReady => Time.unscaledTime >= _nextAllowedUnscaledTime;

        public TapCooldownGate(float cooldownSeconds = DefaultCooldownSeconds)
        {
            SetCooldown(cooldownSeconds);
            _nextAllowedUnscaledTime = 0f;
        }

        public void SetCooldown(float cooldownSeconds)
        {
            _cooldownSeconds = Mathf.Max(0f, cooldownSeconds);
        }

        /// <summary>
        /// Returns true and consumes the gate when enough time has elapsed since the last accept.
        /// </summary>
        public bool TryAccept()
        {
            if (!IsReady)
                return false;

            Consume();
            return true;
        }

        /// <summary>Marks the gate as used from now (caller already verified <see cref="IsReady"/>).</summary>
        public void Consume()
        {
            _nextAllowedUnscaledTime = Time.unscaledTime + _cooldownSeconds;
        }

        public void Reset()
        {
            _nextAllowedUnscaledTime = 0f;
        }
    }
}
