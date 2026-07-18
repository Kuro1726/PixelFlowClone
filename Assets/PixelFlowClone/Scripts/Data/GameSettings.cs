using System;
using UnityEngine;

namespace PixelFlowClone.Data
{
    /// <summary>
    /// Persistent player preferences for audio and haptic feedback (PlayerPrefs).
    /// </summary>
    public static class GameSettings
    {
        public const string AudioPrefsKey = "PFC_AudioEnabled";
        public const string HapticPrefsKey = "PFC_HapticEnabled";

        private static bool _loaded;
        private static bool _audioEnabled = true;
        private static bool _hapticEnabled = true;

        public static event Action SettingsChanged;

        public static bool AudioEnabled
        {
            get
            {
                EnsureLoaded();
                return _audioEnabled;
            }
            set
            {
                EnsureLoaded();
                if (_audioEnabled == value)
                    return;

                _audioEnabled = value;
                PlayerPrefs.SetInt(AudioPrefsKey, value ? 1 : 0);
                PlayerPrefs.Save();
                ApplyAudio();
                SettingsChanged?.Invoke();
            }
        }

        public static bool HapticEnabled
        {
            get
            {
                EnsureLoaded();
                return _hapticEnabled;
            }
            set
            {
                EnsureLoaded();
                if (_hapticEnabled == value)
                    return;

                _hapticEnabled = value;
                PlayerPrefs.SetInt(HapticPrefsKey, value ? 1 : 0);
                PlayerPrefs.Save();
                SettingsChanged?.Invoke();
            }
        }

        /// <summary>
        /// Loads prefs (if needed) and applies audio mute state to <see cref="AudioListener"/>.
        /// Call once at bootstrap so settings survive scene loads.
        /// </summary>
        public static void Apply()
        {
            EnsureLoaded();
            ApplyAudio();
        }

        public static void ToggleAudio() => AudioEnabled = !AudioEnabled;

        public static void ToggleHaptic() => HapticEnabled = !HapticEnabled;

        /// <summary>
        /// Triggers a short device vibration when haptic is enabled (mobile).
        /// </summary>
        public static void TryHaptic()
        {
            if (!HapticEnabled)
                return;

#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }

        private static void EnsureLoaded()
        {
            if (_loaded)
                return;

            _audioEnabled = PlayerPrefs.GetInt(AudioPrefsKey, 1) != 0;
            _hapticEnabled = PlayerPrefs.GetInt(HapticPrefsKey, 1) != 0;
            _loaded = true;
        }

        private static void ApplyAudio()
        {
            AudioListener.volume = _audioEnabled ? 1f : 0f;
        }
    }
}
