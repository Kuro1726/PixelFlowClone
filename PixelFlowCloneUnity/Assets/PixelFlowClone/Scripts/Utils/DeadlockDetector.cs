using System.Collections.Generic;

namespace PixelFlowClone.Utils
{
    /// <summary>
    /// Pure deadlock rules with no dependency on MonoBehaviour or scene objects.
    /// </summary>
    public static class DeadlockDetector
    {
        public static bool IsDeadlocked(IDeadlockContext context)
        {
            if (context == null)
                return false;

            if (context.ConveyorActiveCount < context.ConveyorMaxCapacity)
                return false;

            if (context.QueueOccupiedSlots < context.QueueMaxSlots)
                return false;

            IReadOnlyList<BlockSnapshot> blocks = context.RemainingBlocks;
            if (blocks == null || blocks.Count == 0)
                return false;

            IReadOnlyList<CollectorSnapshot> collectors =
                context.ActiveConveyorCollectors;
            if (collectors == null)
                return true;

            for (int i = 0; i < collectors.Count; i++)
            {
                CollectorSnapshot collector = collectors[i];
                if (collector.Capacity <= 0)
                    continue;

                for (int j = 0; j < blocks.Count; j++)
                {
                    BlockSnapshot block = blocks[j];
                    if (block.Color != collector.Color)
                        continue;

                    if (context.CanCollectorReachBlock(collector, block))
                        return false;
                }
            }

            return true;
        }
    }
}
