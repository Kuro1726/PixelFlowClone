using System;
using System.Collections.Generic;
using PixelFlowClone.Utils;

namespace PixelFlowClone.Tests.EditMode
{
    /// <summary>
    /// In-memory <see cref="IDeadlockContext"/> for Edit Mode deadlock tests.
    /// </summary>
    public sealed class MockDeadlockContext : IDeadlockContext
    {
        private readonly Func<CollectorSnapshot, BlockSnapshot, bool> _canReach;
        private readonly IReadOnlyList<CollectorSnapshot> _collectors;
        private readonly IReadOnlyList<BlockSnapshot> _blocks;

        public MockDeadlockContext(
            int conveyorActive,
            int conveyorMax,
            int queueOccupied,
            int queueMax,
            IReadOnlyList<CollectorSnapshot> collectors = null,
            IReadOnlyList<BlockSnapshot> blocks = null,
            Func<CollectorSnapshot, BlockSnapshot, bool> canReach = null)
        {
            ConveyorActiveCount = conveyorActive;
            ConveyorMaxCapacity = conveyorMax;
            QueueOccupiedSlots = queueOccupied;
            QueueMaxSlots = queueMax;
            _collectors = collectors ?? Array.Empty<CollectorSnapshot>();
            _blocks = blocks ?? Array.Empty<BlockSnapshot>();
            _canReach = canReach ?? ((_, _) => false);
        }

        public int ConveyorActiveCount { get; }
        public int ConveyorMaxCapacity { get; }
        public int QueueOccupiedSlots { get; }
        public int QueueMaxSlots { get; }
        public IReadOnlyList<CollectorSnapshot> ActiveConveyorCollectors => _collectors;
        public IReadOnlyList<BlockSnapshot> RemainingBlocks => _blocks;

        public bool CanCollectorReachBlock(CollectorSnapshot collector, BlockSnapshot block) =>
            _canReach(collector, block);
    }
}
