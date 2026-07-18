using System;
using PixelFlowClone.Data;
using PixelFlowClone.Entities;
using UnityEngine;

namespace PixelFlowClone.Core
{
    /// <summary>
    /// Global observer hub. UI and systems subscribe here; gameplay logic raises events without UI references.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<int, int> OnConveyorCountChanged;
        public static event Action<int, int> OnQueueCountChanged;
        public static event Action<CollectorUnit> OnConveyorDispatchRejected;
        /// <summary>Fired after a block is consumed. Args: world position, block color.</summary>
        public static event Action<Vector3, ColorId> OnBlockConsumed;
        public static event Action<CollectorUnit> OnCollectorExited;
        public static event Action<CollectorUnit> OnCollectorLapComplete;
        public static event Action OnVictory;
        public static event Action OnDefeat;
        public static event Action OnDeadlockDetected;

        public static void RaiseConveyorCountChanged(int active, int max)
            => OnConveyorCountChanged?.Invoke(active, max);

        public static void RaiseQueueCountChanged(int occupied, int max)
            => OnQueueCountChanged?.Invoke(occupied, max);

        public static void RaiseConveyorDispatchRejected(CollectorUnit unit)
            => OnConveyorDispatchRejected?.Invoke(unit);

        public static void RaiseBlockConsumed(Vector3 worldPosition, ColorId color)
            => OnBlockConsumed?.Invoke(worldPosition, color);

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
            OnConveyorDispatchRejected = null;
            OnBlockConsumed = null;
            OnCollectorExited = null;
            OnCollectorLapComplete = null;
            OnVictory = null;
            OnDefeat = null;
            OnDeadlockDetected = null;
        }
    }
}
