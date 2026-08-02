using System;
using ColorStackSort.Logic;
using NUnit.Framework;

namespace ColorStackSort.Tests
{
    [TestFixture]
    public class StackContainerTests
    {
        [Test]
        public void TopRunLength_EmptyContainer_IsZero()
        {
            Assert.AreEqual(0, TestBoards.Container(4).TopRunLength);
        }

        [Test]
        public void TopRunLength_MixedStack_CountsOnlyTheTopRun()
        {
            // [Red, Red, Blue] — the top run is one Blue, NOT the whole stack. Conflating the two
            // is what made the first draft of the generator produce unsolvable levels.
            var container = TestBoards.Container(4, 0, 0, 1);

            Assert.AreEqual(1, container.TopRunLength);
            Assert.AreEqual(3, container.Count);
        }

        [Test]
        public void TopRunLength_Monochrome_EqualsCount()
        {
            var container = TestBoards.Container(4, 1, 1, 1);

            Assert.AreEqual(3, container.TopRunLength);
            Assert.IsTrue(container.IsMonochrome);
        }

        [Test]
        public void IsMonochrome_MixedStack_IsFalse()
        {
            Assert.IsFalse(TestBoards.Container(4, 0, 1, 1).IsMonochrome);
        }

        [Test]
        public void IsMonochrome_EmptyContainer_IsFalse()
        {
            Assert.IsFalse(TestBoards.Container(4).IsMonochrome);
        }

        [Test]
        public void Top_EmptyContainer_Throws()
        {
            var container = TestBoards.Container(4);

            Assert.Throws<InvalidOperationException>(() => _ = container.Top);
        }

        [Test]
        public void Push_WhenFull_Throws()
        {
            var container = TestBoards.Container(2, 0, 0);

            Assert.Throws<InvalidOperationException>(() => container.Push(new ColorId(1)));
        }

        [Test]
        public void PushRange_ExceedingFreeSpace_Throws()
        {
            var container = TestBoards.Container(4, 0, 0);

            Assert.Throws<InvalidOperationException>(() => container.PushRange(new ColorId(1), 3));
        }

        [Test]
        public void PopRange_SpanningTwoColours_Throws()
        {
            // Guards the exact bug class that breaks solvability: a caller computing a length that
            // reaches past the run must fail loudly, not silently return the top colour.
            var container = TestBoards.Container(4, 0, 0, 1);

            Assert.Throws<InvalidOperationException>(() => container.PopRange(2));
        }

        [Test]
        public void PopRange_WithinRun_ReturnsColourAndShrinks()
        {
            var container = TestBoards.Container(4, 0, 1, 1);

            var color = container.PopRange(2);

            Assert.AreEqual(new ColorId(1), color);
            Assert.AreEqual(1, container.Count);
            Assert.AreEqual(new ColorId(0), container.Top);
        }

        [Test]
        public void PopRange_MoreThanPresent_Throws()
        {
            var container = TestBoards.Container(4, 0);

            Assert.Throws<InvalidOperationException>(() => container.PopRange(2));
        }

        [Test]
        public void FreeSpace_TracksPushes()
        {
            var container = TestBoards.Container(4, 0, 0);

            Assert.AreEqual(2, container.FreeSpace);
            Assert.IsFalse(container.IsFull);
        }

        [Test]
        public void Clone_IsIndependentOfTheOriginal()
        {
            var original = TestBoards.Container(4, 0, 0);
            var clone = original.Clone();

            clone.Push(new ColorId(1));

            Assert.AreEqual(2, original.Count, "Mutating the clone must not touch the original.");
            Assert.AreEqual(3, clone.Count);
        }

        [Test]
        public void Constructor_ZeroCapacity_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new StackContainer(0));
        }
    }
}
