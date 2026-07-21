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
        public static event Action<CollectorUnit> OnCollectorDispatchedFromWaiting;
        public static event Action<CollectorUnit> OnCollectorDispatchedFromQueue;
        /// <summary>Fired after a block is consumed. Args: world position, block color.</summary>
        public static event Action<Vector3, ColorId> OnBlockConsumed;
        /// <summary>Cosmetic shot from a collector to a consumed block. Args: origin, target.</summary>
        public static event Action<Vector3, Vector3> OnCollectorShot;
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

        public static void RaiseCollectorDispatchedFromWaiting(CollectorUnit unit)
            => OnCollectorDispatchedFromWaiting?.Invoke(unit);

        public static void RaiseCollectorDispatchedFromQueue(CollectorUnit unit)
            => OnCollectorDispatchedFromQueue?.Invoke(unit);

        public static void RaiseBlockConsumed(Vector3 worldPosition, ColorId color)
            => OnBlockConsumed?.Invoke(worldPosition, color);

        public static void RaiseCollectorShot(Vector3 origin, Vector3 target)
            => OnCollectorShot?.Invoke(origin, target);

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
            OnCollectorDispatchedFromWaiting = null;
            OnCollectorDispatchedFromQueue = null;
            OnBlockConsumed = null;
            OnCollectorShot = null;
            OnCollectorExited = null;
            OnCollectorLapComplete = null;
            OnVictory = null;
            OnDefeat = null;
            OnDeadlockDetected = null;
        }
    }
}
