using UnityEngine;

namespace PixelFlowClone.Utils
{
    /// <summary>
    /// Forces a phone-like portrait window on desktop builds so UI (1080x1920) and
    /// camera fit stay correct. Mobile / WebGL keep their platform defaults.
    /// </summary>
    public static class PortraitDisplay
    {
        public const int Width = 540;
        public const int Height = 960;

        /// <summary>
        /// Call once at bootstrap. Safe to call repeatedly.
        /// </summary>
        public static void ApplyForStandalone()
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            // Editor Game view is user-controlled; only force resolution in player builds.
#if !UNITY_EDITOR
            Screen.fullScreenMode = FullScreenMode.Windowed;
            if (Screen.width != Width || Screen.height != Height || Screen.fullScreen)
                Screen.SetResolution(Width, Height, FullScreenMode.Windowed);

            Debug.Log($"[PortraitDisplay] Standalone window {Width}x{Height} (portrait).");
#endif
#endif
        }
    }
}
