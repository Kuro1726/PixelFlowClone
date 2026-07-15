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

        /// <summary>Primary front unit (depth 0). Use <see cref="TryDispatchFromWaiting(CollectorUnit)"/> when tapping a specific front-row unit.</summary>
        public CollectorUnit GetWaitingQueueFront() => _waiting != null ? _waiting.Front : null;

        public void SpawnWaitingFromLevel(LevelDataSO level)
        {
            if (_waiting == null)
            {
                Debug.LogWarning("[QueueManager] WaitingSlotController is not assigned.");
                return;
            }

            _waiting.SpawnFromLevel(level);
            NotifyQueueCountChanged();
        }

        /// <summary>Dispatches the primary front waiting unit to the conveyor.</summary>
        public bool TryDispatchFromWaiting() => TryDispatchFromWaiting(GetWaitingQueueFront());

        /// <summary>
        /// Dispatches a front-row waiting unit to the conveyor only.
        /// Returns false when not front, conveyor full, or dispatch fails.
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
                // P2-07: shake + reject SFX placeholder.
                Debug.Log("[QueueManager] Waiting dispatch rejected — conveyor full.");
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

        private void NotifyQueueCountChanged()
        {
            GameEvents.RaiseQueueCountChanged(OccupiedSlots, MaxSlots);
        }
    }
}
