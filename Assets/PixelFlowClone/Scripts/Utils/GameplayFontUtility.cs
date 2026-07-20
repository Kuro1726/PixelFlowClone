using TMPro;
using UnityEngine;

namespace PixelFlowClone.Utils
{
    /// <summary>Applies the shared gameplay font to HUD and collector counters.</summary>
    public static class GameplayFontUtility
    {
        private const string FontResourcePath = "Fonts/LilitaOne_SDF";
        private const string MaterialResourcePath = "Fonts/LilitaOne_Gameplay";
        private static TMP_FontAsset _font;
        private static Material _material;
        private static bool _warnedMissingFont;

        public static void Apply(TMP_Text label)
        {
            if (label == null)
                return;

            if (_font == null)
                _font = Resources.Load<TMP_FontAsset>(FontResourcePath);
            if (_material == null)
                _material = Resources.Load<Material>(MaterialResourcePath);

            if (_font == null)
            {
                if (!_warnedMissingFont)
                {
                    Debug.LogWarning(
                        $"[GameplayFontUtility] Font not found at Resources/{FontResourcePath}.");
                    _warnedMissingFont = true;
                }

                return;
            }

            label.font = _font;
            if (_material != null)
                label.fontSharedMaterial = _material;
        }
    }
}
