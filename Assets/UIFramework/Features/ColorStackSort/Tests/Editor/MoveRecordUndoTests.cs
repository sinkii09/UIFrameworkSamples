using System;
using ColorStackSort.Logic;
using NUnit.Framework;

namespace ColorStackSort.Tests
{
    /// <summary>
    /// Undo is the board's second write path and cannot lean on <see cref="StackContainer.PopRange"/>
    /// for safety — PopRange derives its expected colour from whatever is on top, so a wrong record
    /// pops the wrong balls without throwing. Every rejection case below is therefore a test of
    /// <see cref="BoardState.UndoMove"/>'s own checks, not of PopRange's.
    /// </summary>
    [TestFixture]
    public class MoveRecordUndoTests
    {
        private static BoardInteraction On(BoardState board) => new BoardInteraction(board);

        [Test]
        public void UndoMove_AfterMoveOntoEmpty_RestoresExactBoard()
        {
            var board = TestBoards.Board(4, new[] { 1, 0, 0 }, TestBoards.Empty);
            var before = TestBoards.Describe(board);

            board.Apply(new Move(0, 1));
            board.UndoMove(new MoveRecord(0, 1, 2, new ColorId(0)));

            Assert.AreEqual(before, TestBoards.Describe(board));
        }

        [Test]
        public void UndoMove_AfterRunMergedWithSameColor_LiftsOnlyTheRecordedCount()
        {
            // The case the board cannot reconstruct on its own: 2 reds land on 1 red, and the
            // destination's top run is now 3. Undo must lift 2, not the whole run.
            var board = TestBoards.Board(4, new[] { 1, 0, 0 }, new[] { 0 });
            var before = TestBoards.Describe(board);

            board.Apply(new Move(0, 1));
            Assert.AreEqual(3, board[1].TopRunLength, "sanity: the runs merged");

            board.UndoMove(new MoveRecord(0, 1, 2, new ColorId(0)));

            Assert.AreEqual(before, TestBoards.Describe(board));
        }

        [Test]
        public void UndoMove_ValidShapedRecordAgainstADifferentBoard_Throws()
        {
            // The corruption case. Container 1 is topped by 3 blues; the record claims 2 reds went
            // there. PopRange alone would happily pop two blues and push back two reds, silently
            // breaking the items-per-colour invariant that IsSolved depends on.
            var board = TestBoards.Board(4, new[] { 0, 0 }, new[] { 1, 1, 1 });

            Assert.Throws<InvalidOperationException>(
                () => board.UndoMove(new MoveRecord(0, 1, 2, new ColorId(0))));
        }

        [Test]
        public void UndoMove_WhenOriginStillToppedByTheSameColor_Throws()
        {
            // Check 6. Apply pops the MAXIMAL run, so a correct move always leaves a different
            // colour (or nothing) exposed on the origin. Finding the record's colour back on top
            // means the history is being replayed out of order.
            var board = TestBoards.Board(4, new[] { 0, 0 }, new[] { 0 });

            Assert.Throws<InvalidOperationException>(
                () => board.UndoMove(new MoveRecord(0, 1, 1, new ColorId(0))));
        }

        [Test]
        public void UndoMove_WhenOriginIsADifferentColor_Succeeds()
        {
            // The inverse of the test above, and the reason check 6 is != rather than ==. Revision 2
            // of the plan had this operator backwards, which would have rejected every undo except
            // ones that emptied their source.
            var board = TestBoards.Board(4, new[] { 1 }, new[] { 0, 0 });

            Assert.DoesNotThrow(() => board.UndoMove(new MoveRecord(0, 1, 2, new ColorId(0))));
            Assert.AreEqual(3, board[0].Count);
            Assert.IsTrue(board[1].IsEmpty);
        }

        [Test]
        public void UndoMove_WhenLandingsTopRunIsShorterThanRecorded_ThrowsFromItsOwnCheck()
        {
            // Landing is topped by the right colour and holds enough items, but only one of them
            // belongs to the top run. PopRange would also reject this, with the same exception
            // type — so the message is asserted to prove the rejection came from UndoMove's own
            // check rather than by luck from the container's. Delete check 4 and this goes red.
            var board = TestBoards.Board(4, new[] { 1 }, new[] { 1, 0 });

            var ex = Assert.Throws<InvalidOperationException>(
                () => board.UndoMove(new MoveRecord(0, 1, 2, new ColorId(0))));

            StringAssert.Contains("top run", ex.Message);
        }

        [Test]
        public void UndoMove_DefaultRecord_ThrowsRatherThanReadingAnEmptyContainer()
        {
            // default(MoveRecord) bypasses the constructor's guards: Count 0, From == To == 0.
            // Without the re-check, the depth test passes vacuously and Top throws on empty.
            var board = TestBoards.Board(4, TestBoards.Empty, TestBoards.Empty);

            var ex = Assert.Throws<InvalidOperationException>(() => board.UndoMove(default));

            StringAssert.Contains("no items", ex.Message);
        }

        [Test]
        public void UndoMove_WhenLandingHoldsFewerItemsThanRecorded_Throws()
        {
            var board = TestBoards.Board(4, new[] { 1 }, new[] { 0 });

            Assert.Throws<InvalidOperationException>(
                () => board.UndoMove(new MoveRecord(0, 1, 3, new ColorId(0))));
        }

        [Test]
        public void UndoMove_WhenOriginHasNoRoom_Throws()
        {
            var board = TestBoards.Board(3, new[] { 1, 1, 1 }, new[] { 0, 0 });

            Assert.Throws<InvalidOperationException>(
                () => board.UndoMove(new MoveRecord(0, 1, 2, new ColorId(0))));
        }

        [Test]
        public void UndoMove_OutOfRangeContainer_Throws()
        {
            var board = TestBoards.Board(4, new[] { 0 }, TestBoards.Empty);

            Assert.Throws<ArgumentOutOfRangeException>(
                () => board.UndoMove(new MoveRecord(0, 9, 1, new ColorId(0))));
        }

        [Test]
        public void MoveRecord_SameSourceAndDestination_Throws()
        {
            Assert.Throws<ArgumentException>(() => new MoveRecord(2, 2, 1, new ColorId(0)));
        }

        [Test]
        public void MoveRecord_ZeroCount_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MoveRecord(0, 1, 0, new ColorId(0)));
        }

    }
}
