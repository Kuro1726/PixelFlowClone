using PixelFlowClone.Data;

namespace PixelFlowClone.Utils
{
    /// <summary>
    /// Pure capacity rules when a collector consumes a block. No Unity scene dependencies.
    /// </summary>
    public static class CapacityLogic
    {
        /// <summary>
        /// Decrements capacity when the collector color matches the block color.
        /// Returns false when capacity is already zero or colors differ.
        /// </summary>
        public static bool TryConsume(
            int capacity,
            ColorId collectorColor,
            ColorId blockColor,
            out int newCapacity)
        {
            newCapacity = capacity;

            if (capacity <= 0)
                return false;

            if (collectorColor != blockColor)
                return false;

            newCapacity = capacity - 1;
            return true;
        }
    }
}
