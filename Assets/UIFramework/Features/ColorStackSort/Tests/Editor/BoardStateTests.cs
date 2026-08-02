using System;
using ColorStackSort.Logic;
using NUnit.Framework;

namespace ColorStackSort.Tests
{
    /// <summary>Covers the PLAYER's move rule. The generator's looser rule lives in ReverseStepRuleTests.</summary>
    [TestFixture]
    public class BoardStateTests
    {
        [Test]
        public void IsLegal_SourceEmpty_IsFalse()
        {
            var board = TestBoards.Board(4, TestBoards.Empty, new[] { 0 });

            Assert.IsFalse(board.IsLegal(new Move(0, 1)));
        }

        [Test]
        public void IsLegal_SameContainer_IsFalse()
        {
            var board = TestBoards.Board(4, new[] { 0 }, TestBoards.Empty);

            Assert.IsFalse(board.IsLegal(new Move(0, 0)));
        }

        [Test]
        public void IsLegal_DestinationOutOfRange_IsFalse()
        {
            var board = TestBoards.Board(4, new[] { 0 }, TestBoards.Empty);

            Assert.IsFalse(board.IsLegal(new Move(0, 7)));
        }

        [Test]
        public void IsLegal_ColourMismatch_IsFalse()
        {
            var board = TestBoards.Board(4, new[] { 0 }, new[] { 1 });

            Assert.IsFalse(board.IsLegal(new Move(0, 1)));
        }

        [Test]
        public void IsLegal_DestinationEmpty_IsTrue()
        {
            var board = TestBoards.Board(4, new[] { 0, 0 }, TestBoards.Empty);

            Assert.IsTrue(board.IsLegal(new Move(0, 1)));
        }

        [Test]
        public void IsLegal_InsufficientSpaceForWholeRun_IsFalse()
        {
            // Source's top run is three Reds; destination has room for two. The move is all-or-
            // nothing, so a partial fit is no fit.
            var board = TestBoards.Board(4, new[] { 0, 0, 0 }, new[] { 1, 0 });

            Assert.IsFalse(board.IsLegal(new Move(0, 1)));
        }

        [Test]
        public void IsLegal_ExactFit_IsTrue()
        {
            // Run of two Reds into exactly two free slots — the boundary the previous test sits
            // one slot below.
            var board = TestBoards.Board(4, new[] { 1, 0, 0 }, new[] { 1, 0 });

            Assert.AreEqual(2, board[1].FreeSpace, "Fixture must leave room for exactly the run.");
            Assert.IsTrue(board.IsLegal(new Move(0, 1)));
        }

        [Test]
        public void Apply_MovesTheWholeRunNotJustTheTopItem()
        {
            var board = TestBoards.Board(4, new[] { 1, 0, 0 }, new[] { 0 });

            board.Apply(new Move(0, 1));

            Assert.AreEqual(1, board[0].Count, "Both Reds should have left the source.");
            Assert.AreEqual(new ColorId(1), board[0].Top);
            Assert.AreEqual(3, board[1].Count);
            Assert.IsTrue(board[1].IsMonochrome);
        }

        [Test]
        public void Apply_IllegalMove_Throws()
        {
            var board = TestBoards.Board(4, new[] { 0 }, new[] { 1 });

            Assert.Throws<InvalidOperationException>(() => board.Apply(new Move(0, 1)));
        }

        [Test]
        public void IsSolved_EachColourAloneInOneContainer_IsTrue()
        {
            var board = TestBoards.Board(3, new[] { 0, 0, 0 }, new[] { 1, 1, 1 }, TestBoards.Empty);

            Assert.IsTrue(board.IsSolved);
        }

        [Test]
        public void IsSolved_ColourSplitAcrossTwoContainers_IsFalse()
        {
            // Every container is monochrome, yet the Reds sit in two places. Without the
            // no-colour-in-two-containers clause this would wrongly read as a win.
            var board = TestBoards.Board(3, new[] { 0, 0 }, new[] { 0 }, new[] { 1, 1, 1 });

            Assert.IsFalse(board.IsSolved);
        }

        [Test]
        public void IsSolved_MixedContainer_IsFalse()
        {
            var board = TestBoards.Board(3, new[] { 0, 1 }, new[] { 1, 1 }, TestBoards.Empty);

            Assert.IsFalse(board.IsSolved);
        }

        [Test]
        public void IsSolved_EmptyBoard_IsTrue()
        {
            var board = TestBoards.Board(3, TestBoards.Empty, TestBoards.Empty);

            Assert.IsTrue(board.IsSolved);
        }

        [Test]
        public void Clone_IsIndependentOfTheOriginal()
        {
            var original = TestBoards.Board(4, new[] { 0, 0 }, TestBoards.Empty);
            var clone = original.Clone();

            clone.Apply(new Move(0, 1));

            Assert.AreEqual(2, original[0].Count, "Mutating the clone must not touch the original.");
            Assert.AreEqual(0, original[1].Count);
        }

        [Test]
        public void Constructor_FewerThanTwoContainers_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BoardState(1, 4));
        }
    }
}
