using NUnit.Framework;
using PixelFlowClone.Data;
using PixelFlowClone.Utils;
using UnityEngine;

namespace PixelFlowClone.Tests.EditMode
{
    public class DeadlockDetectorTests
    {
        [Test]
        public void NotDeadlocked_WhenConveyorNotFull()
        {
            var ctx = new MockDeadlockContext(
                conveyorActive: 3,
                conveyorMax: 5,
                queueOccupied: 5,
                queueMax: 5);

            Assert.That(DeadlockDetector.IsDeadlocked(ctx), Is.False);
        }

        [Test]
        public void NotDeadlocked_WhenQueueNotFull()
        {
            var ctx = new MockDeadlockContext(
                conveyorActive: 5,
                conveyorMax: 5,
                queueOccupied: 2,
                queueMax: 5);

            Assert.That(DeadlockDetector.IsDeadlocked(ctx), Is.False);
        }

        [Test]
        public void IsDeadlocked_WhenBothFull_NoReachablePair()
        {
            var collectors = new[]
            {
                new CollectorSnapshot(ColorId.Red, 5, Vector2.zero, Vector2.right)
            };
            var blocks = new[]
            {
                new BlockSnapshot(ColorId.Blue, Vector2.up)
            };

            var ctx = new MockDeadlockContext(
                conveyorActive: 5,
                conveyorMax: 5,
                queueOccupied: 5,
                queueMax: 5,
                collectors,
                blocks,
                canReach: (_, _) => false);

            Assert.That(DeadlockDetector.IsDeadlocked(ctx), Is.True);
        }

        [Test]
        public void NotDeadlocked_WhenBothFull_HasReachablePair()
        {
            var collectors = new[]
            {
                new CollectorSnapshot(ColorId.Red, 3, Vector2.zero, Vector2.right)
            };
            var blocks = new[]
            {
                new BlockSnapshot(ColorId.Red, Vector2.up)
            };

            var ctx = new MockDeadlockContext(
                conveyorActive: 5,
                conveyorMax: 5,
                queueOccupied: 5,
                queueMax: 5,
                collectors,
                blocks,
                canReach: (c, b) => c.Color == b.Color);

            Assert.That(DeadlockDetector.IsDeadlocked(ctx), Is.False);
        }

        [Test]
        public void NotDeadlocked_WhenNoBlocksRemaining()
        {
            var collectors = new[]
            {
                new CollectorSnapshot(ColorId.Red, 5, Vector2.zero, Vector2.right)
            };

            var ctx = new MockDeadlockContext(
                conveyorActive: 5,
                conveyorMax: 5,
                queueOccupied: 5,
                queueMax: 5,
                collectors,
                blocks: System.Array.Empty<BlockSnapshot>(),
                canReach: (_, _) => false);

            Assert.That(DeadlockDetector.IsDeadlocked(ctx), Is.False);
        }
    }
}
