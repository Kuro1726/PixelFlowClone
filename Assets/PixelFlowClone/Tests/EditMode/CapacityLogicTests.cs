using NUnit.Framework;
using PixelFlowClone.Data;
using PixelFlowClone.Utils;

namespace PixelFlowClone.Tests.EditMode
{
    public class CapacityLogicTests
    {
        [Test]
        public void TryConsume_DecrementsCapacity_WhenColorsMatch()
        {
            bool consumed = CapacityLogic.TryConsume(
                3,
                ColorId.Red,
                ColorId.Red,
                out int newCapacity);

            Assert.That(consumed, Is.True);
            Assert.That(newCapacity, Is.EqualTo(2));
        }

        [Test]
        public void TryConsume_DoesNotDecrementCapacity_WhenColorsDiffer()
        {
            bool consumed = CapacityLogic.TryConsume(
                3,
                ColorId.Red,
                ColorId.Blue,
                out int newCapacity);

            Assert.That(consumed, Is.False);
            Assert.That(newCapacity, Is.EqualTo(3));
        }
    }
}
