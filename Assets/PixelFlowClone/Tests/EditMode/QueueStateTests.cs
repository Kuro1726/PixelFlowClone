using NUnit.Framework;
using PixelFlowClone.Entities;
using PixelFlowClone.Utils;

namespace PixelFlowClone.Tests.EditMode
{
    public class QueueStateTests
    {
        [Test]
        public void EnqueueFails_WhenAllSlotsOccupied()
        {
            bool canEnqueue = QueueStateLogic.CanEnqueueFromLap(
                CollectorState.OnConveyor,
                capacity: 5,
                occupiedSlots: 5,
                maxSlots: 5);

            Assert.That(canEnqueue, Is.False);
        }

        [Test]
        public void DispatchFromQueueFails_WhenConveyorFull()
        {
            bool canDispatch = QueueStateLogic.CanDispatchFromQueue(
                CollectorState.InQueueSlot,
                isInQueueSlot: true,
                conveyorActive: 5,
                conveyorMax: 5);

            Assert.That(canDispatch, Is.False);
        }

        [Test]
        public void EnqueueSucceeds_WhenQueueHasEmptySlot()
        {
            bool canEnqueue = QueueStateLogic.CanEnqueueFromLap(
                CollectorState.OnConveyor,
                capacity: 3,
                occupiedSlots: 4,
                maxSlots: 5);

            Assert.That(canEnqueue, Is.True);
        }

        [Test]
        public void DispatchFromQueueSucceeds_WhenConveyorHasSpace()
        {
            bool canDispatch = QueueStateLogic.CanDispatchFromQueue(
                CollectorState.InQueueSlot,
                isInQueueSlot: true,
                conveyorActive: 4,
                conveyorMax: 5);

            Assert.That(canDispatch, Is.True);
        }

        [Test]
        public void DispatchFromWaiting_Succeeds_WhenConveyorHasSpace()
        {
            bool canDispatch = QueueStateLogic.CanDispatchFromWaiting(
                CollectorState.InWaitingStack,
                isWaitingFront: true,
                conveyorActive: 2,
                conveyorMax: 5);

            Assert.That(canDispatch, Is.True);
        }

        [Test]
        public void DispatchFromWaiting_Fails_WhenConveyorFull()
        {
            bool canDispatch = QueueStateLogic.CanDispatchFromWaiting(
                CollectorState.InWaitingStack,
                isWaitingFront: true,
                conveyorActive: 5,
                conveyorMax: 5);

            Assert.That(canDispatch, Is.False);
        }
    }
}
