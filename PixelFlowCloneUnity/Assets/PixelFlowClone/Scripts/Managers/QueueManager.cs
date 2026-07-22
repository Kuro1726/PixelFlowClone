using System.Collections.Generic;
using PixelFlowClone.Core;
using PixelFlowClone.Data;
using PixelFlowClone.Entities;
using PixelFlowClone.Queue;
using UnityEngine;

namespace PixelFlowClone.Managers
{
    /// <summary>
    /// Coordinates waiting stack and horizontal queue slots. Tap dispatch from waiting goes
    /// straight to the conveyor (never into queue slots). Enqueue / queue dispatch come in P2-08+.
    /// </summary>
    public class QueueManager : Singleton<QueueManager>, ICollectorQueueFlow
    {
        [SerializeField] private WaitingSlotController _waiting;
        [SerializeField] private QueueSlotController _queueSlots;

        public int OccupiedSlots => _queueSlots != null ? _queueSlots.OccupiedCount : 0;
        public int MaxSlots => _queueSlots != null ? _queueSlots.MaxSlots : 5;
        public int WaitingCount => _waiting != null ? _waiting.Count : 0;

        public IReadOnlyList<CollectorUnit> QueueSlotUnits =>
            _queueSlots != null ? _queueSlots.Units : System.Array.Empty<CollectorUnit>();

        public WaitingSlotController Waiting => _waiting;
        public QueueSlotController QueueSlots => _queueSlots;

        /// <summary>Primary front (column 0). Use <see cref="TryDispatchFromWaiting(CollectorUnit)"/> when tapping a specific column front.</summary>
        public CollectorUnit GetWaitingQueueFront() => _waiting != null ? _waiting.Front : null;

        public void SpawnWaitingFromLevel(LevelDataSO level)
        {
            if (_waiting == null)
            {
                Debug.LogWarning("[QueueManager] WaitingSlotController is not assigned.");
                return;
            }

            LayoutForLevel(level);
            _waiting.SpawnFromLevel(level);
            NotifyQueueCountChanged();
        }

        /// <summary>Clears previous queue state and spawns the waiting collectors for a level.</summary>
        public void LoadLevel(LevelDataSO level)
        {
            ClearAll();
            if (level != null)
                SpawnWaitingFromLevel(level);
        }

        /// <summary>Releases every waiting/queue collector back to the pool.</summary>
        public void ClearAll()
        {
            _queueSlots?.Clear();
            _waiting?.Clear();
            NotifyQueueCountChanged();
        }

        /// <summary>Applies per-level collector spacing; scene positions stay fixed.</summary>
        public void LayoutForLevel(LevelDataSO level)
        {
            if (level == null)
                return;

            _queueSlots?.AnchorToLevel(level);
            _waiting?.AnchorToLevel(level);
        }

        /// <summary>Dispatches the primary front waiting unit to the conveyor.</summary>
        public bool TryDispatchFromWaiting() => TryDispatchFromWaiting(GetWaitingQueueFront());

        /// <summary>
        /// Dispatches a column-front waiting unit to the conveyor only.
        /// Returns false when not that column's front, conveyor full, or dispatch fails.
        /// </summary>
        public bool TryDispatchFromWaiting(CollectorUnit unit)
        {
            return CollectorFlowCoordinator.HasInstance &&
                   CollectorFlowCoordinator.Instance.TryDispatchFromWaiting(unit);
        }

        /// <summary>
        /// Moves an on-conveyor collector with remaining capacity into the first empty queue slot.
        /// Returns false when the queue is unavailable/full or the collector is not eligible.
        /// </summary>
        public bool TryEnqueueFromLap(CollectorUnit unit)
        {
            return CollectorFlowCoordinator.HasInstance &&
                   CollectorFlowCoordinator.Instance.TryEnqueueFromLap(unit);
        }

        /// <summary>
        /// Manual re-dispatch from a queue slot onto the conveyor.
        /// Removes the unit from the slot first so a later lap can enqueue again.
        /// </summary>
        public bool TryDispatchFromQueue(int slotIndex)
        {
            return CollectorFlowCoordinator.HasInstance &&
                   CollectorFlowCoordinator.Instance.TryDispatchFromQueue(slotIndex);
        }

        /// <summary>
        /// Manual re-dispatch of a specific queue-slot collector onto the conveyor.
        /// </summary>
        public bool TryDispatchFromQueue(CollectorUnit unit)
        {
            return CollectorFlowCoordinator.HasInstance &&
                   CollectorFlowCoordinator.Instance.TryDispatchFromQueue(unit);
        }

        internal bool CanDispatchFromWaiting(CollectorUnit unit)
        {
            if (unit == null || _waiting == null)
                return false;

            return _waiting.Contains(unit) &&
                   _waiting.IsFront(unit) &&
                   unit.State == CollectorState.InWaitingStack;
        }

        internal CollectorUnit PopWaitingUnit(CollectorUnit unit)
        {
            return _waiting != null ? _waiting.TryPop(unit) : null;
        }

        internal void RestoreWaitingUnit(CollectorUnit unit)
        {
            _waiting?.RestoreFront(unit);
        }

        internal bool TryGetQueueUnit(int slotIndex, out CollectorUnit unit)
        {
            unit = null;
            if (_queueSlots == null || !_queueSlots.IsValidSlot(slotIndex))
                return false;

            unit = _queueSlots.GetUnit(slotIndex);
            return unit != null && unit.State == CollectorState.InQueueSlot;
        }

        internal bool CanDispatchFromQueue(CollectorUnit unit)
        {
            if (unit == null || _queueSlots == null)
                return false;

            return _queueSlots.Contains(unit) && unit.State == CollectorState.InQueueSlot;
        }

        internal bool CanEnqueueFromLap(CollectorUnit unit)
        {
            return unit != null &&
                   _queueSlots != null &&
                   unit.State == CollectorState.OnConveyor &&
                   unit.Capacity > 0;
        }
        internal int AssignFirstQueueSlot(CollectorUnit unit)
        {
            return _queueSlots != null ? _queueSlots.TryAssignFirstEmpty(unit) : -1;
        }

        internal int IndexOfQueueUnit(CollectorUnit unit)
        {
            return _queueSlots != null ? _queueSlots.IndexOf(unit) : -1;
        }

        internal CollectorUnit RemoveQueueUnit(CollectorUnit unit)
        {
            return _queueSlots != null ? _queueSlots.TryRemove(unit) : null;
        }

        internal bool RestoreQueueUnit(int slotIndex, CollectorUnit unit)
        {
            return _queueSlots != null && _queueSlots.TryAssign(slotIndex, unit);
        }

        internal void NotifyQueueStateChanged()
        {
            NotifyQueueCountChanged();
        }

        /// <summary>
        /// Reject feedback: SFX via GameEvents, shake on the collector (P3-20).
        /// </summary>
        internal void PlayConveyorFullFeedback(CollectorUnit unit)
        {
            Debug.Log(
                $"[QueueManager] Dispatch rejected — conveyor full. " +
                $"collector={unit.Color}, capacity={unit.Capacity}");
            GameEvents.RaiseConveyorDispatchRejected(unit);
            unit.PlayRejectShake();
            GameSettings.TryHaptic();
        }

        private void NotifyQueueCountChanged()
        {
            GameEvents.RaiseQueueCountChanged(OccupiedSlots, MaxSlots);
        }
    }
}
