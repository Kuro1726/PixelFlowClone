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
                ColorId.Red => new Color(0.95f, 0.28f, 0.32f),
                ColorId.Blue => new Color(0.25f, 0.78f, 0.95f),
                ColorId.Green => new Color(0.30f, 0.86f, 0.42f),
                ColorId.Yellow => new Color(1.00f, 0.88f, 0.22f),
                ColorId.Purple => new Color(0.70f, 0.38f, 0.92f),
                ColorId.Orange => new Color(1.00f, 0.62f, 0.20f),
                ColorId.Pink => new Color(1.00f, 0.48f, 0.72f),
                ColorId.Black => new Color(0.14f, 0.14f, 0.16f),
                ColorId.White => new Color(0.96f, 0.96f, 0.97f),
                ColorId.Brown => new Color(0.62f, 0.38f, 0.18f),
                _ => Color.clear
            };
        }
    }
}
