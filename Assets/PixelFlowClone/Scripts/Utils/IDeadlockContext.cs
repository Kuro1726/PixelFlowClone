using System.Collections.Generic;
using PixelFlowClone.Data;
using UnityEngine;

namespace PixelFlowClone.Utils
{
    /// <summary>
    /// Read-only gameplay data required by the pure deadlock detector.
    /// Runtime managers are adapted to this contract in P2-17.
    /// </summary>
    public interface IDeadlockContext
    {
        int ConveyorActiveCount { get; }
        int ConveyorMaxCapacity { get; }
        int QueueOccupiedSlots { get; }
        int QueueMaxSlots { get; }
        IReadOnlyList<CollectorSnapshot> ActiveConveyorCollectors { get; }
        IReadOnlyList<BlockSnapshot> RemainingBlocks { get; }

        bool CanCollectorReachBlock(CollectorSnapshot collector, BlockSnapshot block);
    }

    public readonly struct CollectorSnapshot
    {
        public ColorId Color { get; }
        public int Capacity { get; }
        public Vector2 WorldPosition { get; }
        public Vector2 MoveDirection { get; }

        public CollectorSnapshot(
            ColorId color,
            int capacity,
            Vector2 worldPosition,
            Vector2 moveDirection)
        {
            Color = color;
            Capacity = capacity;
            WorldPosition = worldPosition;
            MoveDirection = moveDirection;
        }
    }

    public readonly struct BlockSnapshot
    {
        public ColorId Color { get; }
        public Vector2 WorldPosition { get; }

        public BlockSnapshot(ColorId color, Vector2 worldPosition)
        {
            Color = color;
            WorldPosition = worldPosition;
        }
    }
}
