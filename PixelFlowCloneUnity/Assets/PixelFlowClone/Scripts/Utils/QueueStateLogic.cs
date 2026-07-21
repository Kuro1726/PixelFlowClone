using PixelFlowClone.Entities;

namespace PixelFlowClone.Utils
{
    /// <summary>
    /// Pure queue / conveyor capacity guards used by QueueManager. No scene dependencies.
    /// </summary>
    public static class QueueStateLogic
    {
        /// <summary>
        /// Whether a lap-complete unit may enter the horizontal queue.
        /// </summary>
        public static bool CanEnqueueFromLap(
            CollectorState state,
            int capacity,
            int occupiedSlots,
            int maxSlots)
        {
            if (state != CollectorState.OnConveyor || capacity <= 0)
                return false;

            if (maxSlots <= 0)
                return false;

            return occupiedSlots < maxSlots;
        }

        /// <summary>
        /// Whether a queue-slot unit may re-enter the conveyor.
        /// </summary>
        public static bool CanDispatchFromQueue(
            CollectorState state,
            bool isInQueueSlot,
            int conveyorActive,
            int conveyorMax)
        {
            if (!isInQueueSlot || state != CollectorState.InQueueSlot)
                return false;

            if (conveyorMax <= 0)
                return false;

            return conveyorActive < conveyorMax;
        }

        /// <summary>
        /// Whether a waiting-stack front may enter the conveyor.
        /// </summary>
        public static bool CanDispatchFromWaiting(
            CollectorState state,
            bool isWaitingFront,
            int conveyorActive,
            int conveyorMax)
        {
            if (!isWaitingFront || state != CollectorState.InWaitingStack)
                return false;

            if (conveyorMax <= 0)
                return false;

            return conveyorActive < conveyorMax;
        }
    }
}
