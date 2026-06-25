using System;
using PixelFlowClone.Entities;

namespace PixelFlowClone.Core
{
    /// <summary>
    /// Global observer hub. UI and systems subscribe here; gameplay logic raises events without UI references.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<int, int> OnConveyorCountChanged;
        public static event Action<int, int> OnQueueCountChanged;
        public static event Action OnBlockConsumed;
        public static event Action<CollectorUnit> OnCollectorExited;
        public static event Action<CollectorUnit> OnCollectorLapComplete;
        public static event Action OnVictory;
        public static event Action OnDefeat;
        public static event Action OnDeadlockDetected;

        public static void RaiseConveyorCountChanged(int active, int max)
            => OnConveyorCountChanged?.Invoke(active, max);

        public static void RaiseQueueCountChanged(int occupied, int max)
            => OnQueueCountChanged?.Invoke(occupied, max);

        public static void RaiseBlockConsumed()
            => OnBlockConsumed?.Invoke();

        public static void RaiseCollectorExited(CollectorUnit unit)
            => OnCollectorExited?.Invoke(unit);

        public static void RaiseCollectorLapComplete(CollectorUnit unit)
            => OnCollectorLapComplete?.Invoke(unit);

        public static void RaiseVictory()
            => OnVictory?.Invoke();

        public static void RaiseDefeat()
            => OnDefeat?.Invoke();

        public static void RaiseDeadlockDetected()
            => OnDeadlockDetected?.Invoke();

        /// <summary>
        /// Clears all subscribers. Use when tearing down scenes or in Edit Mode tests.
        /// </summary>
        public static void ClearAllSubscribers()
        {
            OnConveyorCountChanged = null;
            OnQueueCountChanged = null;
            OnBlockConsumed = null;
            OnCollectorExited = null;
            OnCollectorLapComplete = null;
            OnVictory = null;
            OnDefeat = null;
            OnDeadlockDetected = null;
        }
    }
}
