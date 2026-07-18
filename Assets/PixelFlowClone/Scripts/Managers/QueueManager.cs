using System.Collections.Generic;
using PixelFlowClone.Core;
using PixelFlowClone.Data;
using PixelFlowClone.Entities;
using PixelFlowClone.Queue;
using PixelFlowClone.Utils;
using UnityEngine;

namespace PixelFlowClone.Managers
{
    /// <summary>
    /// Coordinates waiting stack and horizontal queue slots. Tap dispatch from waiting goes
    /// straight to the conveyor (never into queue slots). Enqueue / queue dispatch come in P2-08+.
    /// </summary>
    public class QueueManager : Singleton<QueueManager>
    {
        [SerializeField] private WaitingSlotController _waiting;
        [SerializeField] private QueueSlotController _queueSlots;

        public int OccupiedSlots => _queueSlots != null ? _queueSlots.OccupiedCount : 0;
        public int MaxSlots => _queueSlots != null ? _queueSlots.MaxSlots : 5;

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
            _queueSlots?.Clear();
            SpawnWaitingFromLevel(level);
        }

        /// <summary>Repositions waiting stack + queue slots relative to the rebuilt conveyor path.</summary>
        public void LayoutForLevel(LevelDataSO level)
        {
            if (level == null)
                return;

            float pathMargin = ConveyorPathManager.HasInstance
                ? ConveyorPathManager.Instance.PathMargin
                : LevelLayout.DefaultPathMargin;

            _queueSlots?.AnchorToLevel(level, pathMargin);
            _waiting?.AnchorToLevel(level, pathMargin);
        }

        /// <summary>Dispatches the primary front waiting unit to the conveyor.</summary>
        public bool TryDispatchFromWaiting() => TryDispatchFromWaiting(GetWaitingQueueFront());

        /// <summary>
        /// Dispatches a column-front waiting unit to the conveyor only.
        /// Returns false when not that column's front, conveyor full, or dispatch fails.
        /// </summary>
        public bool TryDispatchFromWaiting(CollectorUnit unit)
        {
            if (unit == null || _waiting == null)
                return false;

            if (!_waiting.Contains(unit) || !_waiting.IsFront(unit))
                return false;

            if (unit.State != CollectorState.InWaitingStack)
                return false;

            if (!ConveyorPathManager.HasInstance)
            {
                Debug.LogWarning("[QueueManager] ConveyorPathManager missing.");
                return false;
            }

            ConveyorPathManager conveyor = ConveyorPathManager.Instance;
            if (!conveyor.HasCapacity)
            {
                PlayConveyorFullFeedback(unit);
                return false;
            }

            CollectorUnit popped = _waiting.TryPop(unit);
            if (popped == null)
                return false;

            if (!conveyor.DispatchToConveyor(popped))
            {
                _waiting.RestoreFront(popped);
                Debug.LogWarning("[QueueManager] Dispatch failed after pop — unit restored to waiting stack.");
                return false;
            }

            Debug.Log($"[QueueManager] Waiting → Conveyor OK: color={popped.Color} capacity={popped.Capacity} waitingLeft={_waiting.Count}");
            return true;
        }

        /// <summary>
        /// Moves an on-conveyor collector with remaining capacity into the first empty queue slot.
        /// Returns false when the queue is unavailable/full or the collector is not eligible.
        /// </summary>
        public bool TryEnqueueFromLap(CollectorUnit unit)
        {
            if (unit == null || _queueSlots == null)
                return false;

            if (unit.State != CollectorState.OnConveyor || unit.Capacity <= 0)
                return false;

            int slotIndex = _queueSlots.TryAssignFirstEmpty(unit);
            if (slotIndex < 0)
            {
                Debug.Log("[QueueManager] Lap enqueue failed — queue is full. Defeat.");
                if (GameManager.HasInstance)
                    GameManager.Instance.DeclareDefeat();
                else
                    Debug.LogError("[QueueManager] Cannot declare defeat — GameManager missing.");

                return false;
            }

            if (ConveyorPathManager.HasInstance)
                ConveyorPathManager.Instance.UnregisterUnit(unit);

            NotifyQueueCountChanged();
            Debug.Log(
                $"[QueueManager] Conveyor → Queue OK: slot={slotIndex}, " +
                $"color={unit.Color}, capacity={unit.Capacity}");
            return true;
        }

        /// <summary>
        /// Manual re-dispatch from a queue slot onto the conveyor.
        /// Removes the unit from the slot first so a later lap can enqueue again.
        /// </summary>
        public bool TryDispatchFromQueue(int slotIndex)
        {
            if (_queueSlots == null || !_queueSlots.IsValidSlot(slotIndex))
                return false;

            CollectorUnit unit = _queueSlots.GetUnit(slotIndex);
            if (unit == null || unit.State != CollectorState.InQueueSlot)
                return false;

            return TryDispatchFromQueue(unit);
        }

        /// <summary>
        /// Manual re-dispatch of a specific queue-slot collector onto the conveyor.
        /// </summary>
        public bool TryDispatchFromQueue(CollectorUnit unit)
        {
            if (unit == null || _queueSlots == null)
                return false;

            if (!_queueSlots.Contains(unit) || unit.State != CollectorState.InQueueSlot)
                return false;

            if (!ConveyorPathManager.HasInstance)
            {
                Debug.LogWarning("[QueueManager] ConveyorPathManager missing.");
                return false;
            }

            ConveyorPathManager conveyor = ConveyorPathManager.Instance;
            if (!conveyor.HasCapacity)
            {
                PlayConveyorFullFeedback(unit);
                return false;
            }

            int slotIndex = _queueSlots.IndexOf(unit);
            CollectorUnit removed = _queueSlots.TryRemove(unit);
            if (removed == null)
                return false;

            if (!conveyor.DispatchToConveyor(removed))
            {
                _queueSlots.TryAssign(slotIndex, removed);
                NotifyQueueCountChanged();
                Debug.LogWarning("[QueueManager] Queue dispatch failed — unit restored to slot.");
                return false;
            }

            NotifyQueueCountChanged();
            Debug.Log(
                $"[QueueManager] Queue → Conveyor OK: slot={slotIndex}, " +
                $"color={removed.Color}, capacity={removed.Capacity}");
            return true;
        }

        /// <summary>
        /// P2-07 feedback stub. Audio/UI can subscribe to the event; visual shake is added in P3-20.
        /// </summary>
        private static void PlayConveyorFullFeedback(CollectorUnit unit)
        {
            Debug.Log(
                $"[QueueManager] Dispatch rejected — conveyor full. " +
                $"collector={unit.Color}, capacity={unit.Capacity}");
            GameEvents.RaiseConveyorDispatchRejected(unit);
        }

        private void NotifyQueueCountChanged()
        {
            GameEvents.RaiseQueueCountChanged(OccupiedSlots, MaxSlots);
        }
    }
}
