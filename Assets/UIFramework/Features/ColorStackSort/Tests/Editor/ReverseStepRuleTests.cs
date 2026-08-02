using System;
using ColorStackSort.Logic;
using NUnit.Framework;

namespace ColorStackSort.Tests
{
    /// <summary>
    /// Covers <see cref="LevelGenerator.IsLegalReverseStep"/> — the GENERATOR's rule, which is
    /// deliberately looser than the player's in <see cref="BoardStateTests"/>. These two rules look
    /// alike and are not interchangeable; this fixture exists so that anyone tempted to merge them
    /// breaks a test rather than shipping unsolvable levels.
    /// </summary>
    [TestFixture]
    public class ReverseStepRuleTests
    {
        [Test]
        public void IsLegalReverseStep_AmountReachesPastTheTopRun_IsRejected()
        {
            // The regression case. Container 0 is [Red, Red, Blue]: the top RUN is one Blue, but
            // the container COUNT is three. The first draft of the constraint keyed on Count and
            // so admitted amount = 3, dragging two Reds along as if they belonged to the Blue run.
            // Forward replay would carry back only the Blue and strand the Reds — unsolvable.
            var board = TestBoards.Board(4, new[] { 0, 0, 1 }, TestBoards.Empty);

            Assert.IsFalse(LevelGenerator.IsLegalReverseStep(board, new ReverseStep(0, 1, 3)),
                "amount == Count must be rejected when the top run is shorter than the container.");
            Assert.IsFalse(LevelGenerator.IsLegalReverseStep(board, new ReverseStep(0, 1, 2)),
                "Any amount past the top run must be rejected.");
        }

        [Test]
        public void IsLegalReverseStep_WholeRunFromNonMonochromeSource_IsRejected()
        {
            // Taking the entire run exposes a different colour underneath, so the forward move that
            // puts the run back would be illegal. Only a partial take keeps the step invertible.
            var board = TestBoards.Board(4, new[] { 0, 1, 1, 1 }, TestBoards.Empty);

            Assert.IsFalse(LevelGenerator.IsLegalReverseStep(board, new ReverseStep(0, 1, 3)));
        }

        [Test]
        public void IsLegalReverseStep_PartialRun_IsAllowed()
        {
            var board = TestBoards.Board(4, new[] { 0, 1, 1, 1 }, TestBoards.Empty);

            Assert.IsTrue(LevelGenerator.IsLegalReverseStep(board, new ReverseStep(0, 1, 1)));
            Assert.IsTrue(LevelGenerator.IsLegalReverseStep(board, new ReverseStep(0, 1, 2)));
        }

        [Test]
        public void IsLegalReverseStep_WholeRunFromMonochromeSource_IsAllowed()
        {
            // The source empties, and dropping onto an empty container is always a legal forward
            // move — so this is the one case where taking the whole run stays invertible.
            var board = TestBoards.Board(4, new[] { 1, 1, 1 }, TestBoards.Empty);

            Assert.IsTrue(LevelGenerator.IsLegalReverseStep(board, new ReverseStep(0, 1, 3)));
        }

        [Test]
        public void IsLegalReverseStep_DestinationTopMatchesRunColour_IsRejected()
        {
            // If the destination's top already matched, the placed items would merge into a longer
            // run, and the forward move would scoop up the destination's own items too.
            var board = TestBoards.Board(4, new[] { 0, 1, 1 }, new[] { 1 });

            Assert.IsFalse(LevelGenerator.IsLegalReverseStep(board, new ReverseStep(0, 1, 1)));
        }

        [Test]
        public void IsLegalReverseStep_DestinationHasNoRoom_IsRejected()
        {
            var board = TestBoards.Board(3, new[] { 0, 1, 1 }, new[] { 0, 0, 0 });

            Assert.IsFalse(LevelGenerator.IsLegalReverseStep(board, new ReverseStep(0, 1, 1)));
        }

        [Test]
        public void IsLegalReverseStep_EmptySource_IsRejected()
        {
            var board = TestBoards.Board(4, TestBoards.Empty, new[] { 0 });

            Assert.IsFalse(LevelGenerator.IsLegalReverseStep(board, new ReverseStep(0, 1, 1)));
        }

        [Test]
        public void IsLegalReverseStep_SameContainer_IsRejected()
        {
            var board = TestBoards.Board(4, new[] { 0, 0 }, TestBoards.Empty);

            Assert.IsFalse(LevelGenerator.IsLegalReverseStep(board, new ReverseStep(0, 0, 1)));
        }

        [Test]
        public void IsLegalReverseStep_NonPositiveAmount_IsRejected()
        {
            var board = TestBoards.Board(4, new[] { 0, 0 }, TestBoards.Empty);

            Assert.IsFalse(LevelGenerator.IsLegalReverseStep(board, new ReverseStep(0, 1, 0)));
        }

        [Test]
        public void IsLegalReverseStep_OutOfRangeDestination_IsRejected()
        {
            var board = TestBoards.Board(4, new[] { 0, 0 }, TestBoards.Empty);

            Assert.IsFalse(LevelGenerator.IsLegalReverseStep(board, new ReverseStep(0, 9, 1)));
        }

        [Test]
        public void IsLegalReverseStep_OutOfRangeSource_IsRejected()
        {
            // Must return false, not throw IndexOutOfRangeException.
            var board = TestBoards.Board(4, new[] { 0, 0 }, TestBoards.Empty);

            Assert.IsFalse(LevelGenerator.IsLegalReverseStep(board, new ReverseStep(9, 0, 1)));
        }

        [Test]
        public void IsLegalReverseStep_NullBoard_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => LevelGenerator.IsLegalReverseStep(null, new ReverseStep(0, 1, 1)));
        }

        [Test]
        public void IsImmediateUndo_ExactReversal_IsTrue()
        {
            // Mutation testing found this helper could be gutted with the whole suite still green.
            var previous = new ReverseStep(0, 1, 2);

            Assert.IsTrue(LevelGenerator.IsImmediateUndo(new ReverseStep(1, 0, 2), previous));
        }

        [TestCase(1, 0, 3, TestName = "IsImmediateUndo_DifferentAmount_IsFalse")]
        [TestCase(1, 2, 2, TestName = "IsImmediateUndo_DifferentDestination_IsFalse")]
        [TestCase(2, 0, 2, TestName = "IsImmediateUndo_DifferentSource_IsFalse")]
        [TestCase(0, 1, 2, TestName = "IsImmediateUndo_SameStepRepeated_IsFalse")]
        public void IsImmediateUndo_NonReversals_AreFalse(int from, int to, int amount)
        {
            var previous = new ReverseStep(0, 1, 2);

            Assert.IsFalse(LevelGenerator.IsImmediateUndo(new ReverseStep(from, to, amount), previous));
        }

        [Test]
        public void ToForwardMove_SendsTheRunBackWhereItCameFrom()
        {
            Assert.AreEqual(new Move(1, 0), new ReverseStep(0, 1, 2).ToForwardMove());
        }
    }
}
