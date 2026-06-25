namespace PixelFlowClone.Data
{
    /// <summary>
    /// Physics layer indices matching ProjectSettings/TagManager.asset.
    /// Layer 6 = PixelBlock, Layer 7 = Collector.
    /// </summary>
    public static class PhysicsLayers
    {
        public const string PixelBlockName = "PixelBlock";
        public const string CollectorName = "Collector";

        public const int PixelBlock = 6;
        public const int Collector = 7;

        public static int GetLayerMask(params int[] layers)
        {
            int mask = 0;
            foreach (int layer in layers)
                mask |= 1 << layer;
            return mask;
        }
    }
}
