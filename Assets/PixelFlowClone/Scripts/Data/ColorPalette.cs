using UnityEngine;

namespace PixelFlowClone.Data
{
    /// <summary>
    /// Maps logical ColorId values to rendered Unity colors.
    /// Shared by PixelBlock and CollectorUnit so matching is visually consistent.
    /// </summary>
    public static class ColorPalette
    {
        public static Color ToColor(ColorId id)
        {
            return id switch
            {
                ColorId.Red => new Color(0.90f, 0.22f, 0.22f),
                ColorId.Blue => new Color(0.20f, 0.45f, 0.90f),
                ColorId.Green => new Color(0.24f, 0.74f, 0.34f),
                ColorId.Yellow => new Color(0.96f, 0.82f, 0.18f),
                ColorId.Purple => new Color(0.60f, 0.30f, 0.80f),
                ColorId.Orange => new Color(0.96f, 0.55f, 0.15f),
                ColorId.Pink => new Color(0.95f, 0.45f, 0.70f),
                ColorId.Black => new Color(0.12f, 0.12f, 0.14f),
                ColorId.White => new Color(0.95f, 0.95f, 0.95f),
                ColorId.Brown => new Color(0.55f, 0.32f, 0.14f),
                _ => Color.clear
            };
        }
    }
}
