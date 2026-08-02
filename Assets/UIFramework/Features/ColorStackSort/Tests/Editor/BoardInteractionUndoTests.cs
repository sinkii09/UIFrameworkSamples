using System;
using ColorStackSort.Logic;
using NUnit.Framework;

namespace ColorStackSort.Tests
{
    /// <summary>
    /// The history side of undo: what gets recorded, in what order it comes back, and what happens
    /// when a record no longer matches the board. <see cref="MoveRecordUndoTests"/> covers the
    /// board mutation itself.
    /// </summary>
    [TestFixture]
    public class BoardInteractionUndoTests
    {
        private static BoardInteraction On(BoardState board) => new BoardInteraction(board);

        [Test]
        public void TryUndo_WithNoHistory_ReturnsFalse()
        {
            var interaction = On(TestBoards.Board(4, new[] { 0, 0 }, TestBoards.Empty));

            Assert.IsFalse(interaction.TryUndo(out _));
        }

        [Test]
        public void TryUndo_AfterMove_ReversesItAndReportsWhatMoved()
        {
            var board = TestBoards.Board(4, new[] { 1, 0, 0 }, TestBoards.Empty);
            var before = TestBoards.Describe(board);
            var interaction = On(board);

            interaction.Tap(0);
            interaction.Tap(1);

            Assert.IsTrue(interaction.TryUndo(out var record));
            Assert.AreEqual(new MoveRecord(0, 1, 2, new ColorId(0)), record);
            Assert.AreEqual(before, TestBoards.Describe(board));
            Assert.IsFalse(interaction.CanUndo);
        }

        [Test]
        public void TryUndo_Repeatedly_WalksAllTheWayBackToTheStart()
        {
            var board = TestBoards.Board(4, new[] { 0, 1, 1 }, new[] { 1 }, TestBoards.Empty);
            var before = TestBoards.Describe(board);
            var interaction = On(board);

            // Container 0's two-blue run onto container 1, then its remaining red to container 2.
            interaction.Tap(0);
            interaction.Tap(1);
            interaction.Tap(0);
            interaction.Tap(2);
            Assert.AreEqual(2, interaction.HistoryCount);

            while (interaction.CanUndo) interaction.TryUndo(out _);

            Assert.AreEqual(before, TestBoards.Describe(board));
        }

        [Test]
        public void TryUndo_ClearsAnyPendingSelection()
        {
            var interaction = On(TestBoards.Board(4, new[] { 1, 0, 0 }, TestBoards.Empty));
            interaction.Tap(0);
            interaction.Tap(1);

            interaction.Tap(1); // re-select the moved run
            Assert.IsTrue(interaction.HasSelection);

            interaction.TryUndo(out _);

            Assert.IsFalse(interaction.HasSelection, "a selection can point at a run undo just moved");
        }

        [Test]
        public void TryUndo_RejectedMove_LeavesHistoryUntouched()
        {
            // Only applied moves are recorded — a bounced tap must not become an undo step.
            var interaction = On(TestBoards.Board(4, new[] { 0, 0 }, new[] { 1, 1, 1, 1 }));

            interaction.Tap(0);
            var result = interaction.Tap(1);

            Assert.AreEqual(TapOutcome.Rejected, result.Outcome);
            Assert.AreEqual(0, interaction.HistoryCount);
        }

        [Test]
        public void TryUndo_WhenTheRecordIsStale_LeavesBoardAndHistoryUntouched()
        {
            var board = TestBoards.Board(4, new[] { 1, 0, 0 }, TestBoards.Empty, TestBoards.Empty);
            var interaction = On(board);

            interaction.Tap(0);
            interaction.Tap(1);
            Assert.AreEqual(1, interaction.HistoryCount);

            // Mutate the board behind the interaction's back, so its top record no longer describes
            // reality. This is the only way to make UndoMove reject from inside TryUndo.
            board.Apply(new Move(1, 2));
            var afterTampering = TestBoards.Describe(board);

            Assert.Throws<InvalidOperationException>(() => interaction.TryUndo(out _));

            Assert.AreEqual(afterTampering, TestBoards.Describe(board), "a rejected undo must not mutate");
            // The ordering guarantee: history is popped only after the board actually changes, so a
            // rejection cannot silently discard the record that would reconcile the two.
            Assert.AreEqual(1, interaction.HistoryCount, "a rejected undo must not consume history");
        }
    }
}
