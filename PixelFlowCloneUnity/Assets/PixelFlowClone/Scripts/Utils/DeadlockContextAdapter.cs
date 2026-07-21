using System.Collections.Generic;
using PixelFlowClone.Core;
using PixelFlowClone.Entities;
using PixelFlowClone.Managers;

namespace PixelFlowClone.Utils
{
    /// <summary>
    /// Adapts live runtime managers to the read-only data required by deadlock detection.
    /// Snapshot buffers are reused to avoid allocating a new list on every check.
    /// </summary>
    public sealed class DeadlockContextAdapter : IDeadlockContext
    {
        private readonly GridManager _grid;
        private readonly ConveyorPathManager _conveyor;
        private readonly QueueManager _queue;

        private readonly List<CollectorSnapshot> _collectorSnapshots = new();
        private readonly List<BlockSnapshot> _blockSnapshots = new();

        public DeadlockContextAdapter(
            GridManager grid,
            ConveyorPathManager conveyor,
            QueueManager queue)
        {
            _grid = grid;
            _conveyor = conveyor;
            _queue = queue;
        }

        public DeadlockContextAdapter(GameplayContext context)
            : this(context?.Grid, context?.Conveyor, context?.Queue)
        {
        }

        public int ConveyorActiveCount => _conveyor != null ? _conveyor.ActiveCount : 0;
        public int ConveyorMaxCapacity => _conveyor != null ? _conveyor.MaxCapacity : 0;
        public int QueueOccupiedSlots => _queue != null ? _queue.OccupiedSlots : 0;
        public int QueueMaxSlots => _queue != null ? _queue.MaxSlots : 0;

        public IReadOnlyList<CollectorSnapshot> ActiveConveyorCollectors
        {
            get
            {
                RefreshCollectorSnapshots();
                return _collectorSnapshots;
            }
        }

        public IReadOnlyList<BlockSnapshot> RemainingBlocks
        {
            get
            {
                RefreshBlockSnapshots();
                return _blockSnapshots;
            }
        }

        public bool CanCollectorReachBlock(CollectorSnapshot collector, BlockSnapshot block)
        {
            // P2-19 samples the path ahead and implements this reachability test.
            return false;
        }

        private void RefreshCollectorSnapshots()
        {
            _collectorSnapshots.Clear();
            if (_conveyor == null)
                return;

            IReadOnlyList<CollectorUnit> units = _conveyor.ActiveUnits;
            for (int i = 0; i < units.Count; i++)
            {
                CollectorUnit unit = units[i];
                if (unit == null || unit.State != CollectorState.OnConveyor)
                    continue;

                _collectorSnapshots.Add(new CollectorSnapshot(
                    unit.Color,
                    unit.Capacity,
                    unit.transform.position,
                    unit.CurrentMoveDirection));
            }
        }

        private void RefreshBlockSnapshots()
        {
            _blockSnapshots.Clear();
            if (_grid == null)
                return;

            foreach (PixelBlock block in _grid.Blocks.Values)
            {
                if (block == null || block.IsConsumed)
                    continue;

                _blockSnapshots.Add(new BlockSnapshot(
                    block.Color,
                    block.transform.position));
            }
        }
    }
}
