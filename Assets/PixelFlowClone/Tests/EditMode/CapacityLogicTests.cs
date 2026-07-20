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

        [Test]
        public void TryConsume_DecrementsFromTenDownToZero_WhenColorsMatch()
        {
            int capacity = 10;

            for (int expected = 9; expected >= 0; expected--)
            {
                bool consumed = CapacityLogic.TryConsume(
                    capacity,
                    ColorId.Blue,
                    ColorId.Blue,
                    out capacity);

                Assert.That(consumed, Is.True);
                Assert.That(capacity, Is.EqualTo(expected));
            }

            Assert.That(capacity, Is.EqualTo(0));
        }

        [Test]
        public void TryConsume_ReturnsFalse_WhenCapacityIsZero()
        {
            bool consumed = CapacityLogic.TryConsume(
                0,
                ColorId.Red,
                ColorId.Red,
                out int newCapacity);

            Assert.That(consumed, Is.False);
            Assert.That(newCapacity, Is.EqualTo(0));
        }

        [Test]
        public void TryConsume_ReturnsFalse_WhenCapacityIsNegative()
        {
            bool consumed = CapacityLogic.TryConsume(
                -1,
                ColorId.Green,
                ColorId.Green,
                out int newCapacity);

            Assert.That(consumed, Is.False);
            Assert.That(newCapacity, Is.EqualTo(-1));
        }

        [Test]
        public void TryConsume_DoesNotChangeCapacity_WhenWrongColorAtFull()
        {
            const int start = 10;

            bool consumed = CapacityLogic.TryConsume(
                start,
                ColorId.Yellow,
                ColorId.Purple,
                out int newCapacity);

            Assert.That(consumed, Is.False);
            Assert.That(newCapacity, Is.EqualTo(start));
        }
    }
}
