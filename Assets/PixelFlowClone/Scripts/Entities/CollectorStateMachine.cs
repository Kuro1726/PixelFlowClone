namespace PixelFlowClone.Entities
{
    /// <summary>
    /// Finite state machine for collector lifecycle. Valid transitions follow planning §1.3.
    /// </summary>
    public class CollectorStateMachine
    {
        public CollectorState CurrentState { get; private set; } = CollectorState.Pooled;

        public bool TryTransition(CollectorState target)
        {
            if (!IsValidTransition(CurrentState, target))
                return false;

            CurrentState = target;
            return true;
        }

        /// <summary>
        /// Sets state without validation. Used when resetting to Pooled or spawning into WaitingStack.
        /// </summary>
        public void ResetTo(CollectorState state)
        {
            CurrentState = state;
        }

        public static bool IsValidTransition(CollectorState from, CollectorState to)
        {
            return (from, to) switch
            {
                (CollectorState.InWaitingStack, CollectorState.OnConveyor) => true,
                (CollectorState.OnConveyor, CollectorState.Exiting) => true,
                (CollectorState.OnConveyor, CollectorState.InQueueSlot) => true,
                (CollectorState.InQueueSlot, CollectorState.OnConveyor) => true,
                (CollectorState.Exiting, CollectorState.Pooled) => true,
                _ => false
            };
        }
    }
}
