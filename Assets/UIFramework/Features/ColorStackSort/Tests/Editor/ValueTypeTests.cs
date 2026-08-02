using System;
using ColorStackSort.Logic;
using NUnit.Framework;

namespace ColorStackSort.Tests
{
    /// <summary>
    /// Guards on the small value types. Included because mutation testing showed several of these
    /// could be deleted outright with the rest of the suite still green.
    /// </summary>
    [TestFixture]
    public class ValueTypeTests
    {
        [Test]
        public void ColorId_EqualityAndHashing_AreByValue()
        {
            Assert.AreEqual(new ColorId(3), new ColorId(3));
            Assert.AreEqual(new ColorId(3).GetHashCode(), new ColorId(3).GetHashCode());
            Assert.IsTrue(new ColorId(3) == new ColorId(3));
            Assert.IsTrue(new ColorId(3) != new ColorId(4));
        }

        [TestCase(-1)]
        [TestCase(256)]
        public void ColorId_IndexOutsideAByte_Throws(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ColorId(value));
        }

        [Test]
        public void Move_EqualityAndInverse()
        {
            Assert.AreEqual(new Move(1, 2), new Move(1, 2));
            Assert.IsTrue(new Move(1, 2) != new Move(2, 1));
            Assert.AreEqual(new Move(2, 1), new Move(1, 2).Inverse());
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        public void Move_NegativeIndex_Throws(int from, int to)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Move(from, to));
        }

        [Test]
        public void ReverseStep_EqualityIsByAllThreeFields()
        {
            Assert.AreEqual(new ReverseStep(0, 1, 2), new ReverseStep(0, 1, 2));
            Assert.IsTrue(new ReverseStep(0, 1, 2) != new ReverseStep(0, 1, 3));
            Assert.IsTrue(new ReverseStep(0, 1, 2) == new ReverseStep(0, 1, 2));
        }

        [Test]
        public void LevelParams_ContainerCountIsColoursPlusEmpties()
        {
            var parameters = new LevelParams(5, 4, 2, 30);

            Assert.AreEqual(7, parameters.ContainerCount);
            Assert.AreEqual(4, parameters.ItemsPerColor, "Items per colour is defined as capacity.");
        }

        [TestCase(1, 4, 1, 10, TestName = "LevelParams_FewerThanTwoColours_Throws")]
        [TestCase(257, 4, 1, 10, TestName = "LevelParams_MoreColoursThanAByteHolds_Throws")]
        [TestCase(2, 1, 1, 10, TestName = "LevelParams_CapacityBelowTwo_Throws")]
        [TestCase(2, 4, 0, 10, TestName = "LevelParams_NoEmptyContainer_Throws")]
        [TestCase(2, 4, 1, 0, TestName = "LevelParams_NoScrambleSteps_Throws")]
        public void LevelParams_Validate_RejectsBadConfigs(int colors, int capacity, int empties, int steps)
        {
            var parameters = new LevelParams(colors, capacity, empties, steps);

            Assert.Throws<ArgumentException>(() => parameters.Validate());
        }

        [Test]
        public void LevelParams_Validate_AcceptsTheSmallestLegalBoard()
        {
            Assert.DoesNotThrow(() => new LevelParams(2, 2, 1, 1).Validate());
        }

        [Test]
        public void GeneratedLevel_NullArguments_Throw()
        {
            var parameters = new LevelParams(2, 2, 1, 1);

            Assert.Throws<ArgumentNullException>(
                () => new GeneratedLevel(null, new Move[0], parameters, 0));
            Assert.Throws<ArgumentNullException>(
                () => new GeneratedLevel(new BoardState(2, 2), null, parameters, 0));
        }
    }
}
