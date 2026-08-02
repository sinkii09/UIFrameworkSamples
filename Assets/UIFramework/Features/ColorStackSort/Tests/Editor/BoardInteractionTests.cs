using System;
using ColorStackSort.Logic;
using NUnit.Framework;

namespace ColorStackSort.Tests
{
    [TestFixture]
    public class BoardInteractionTests
    {
        private static BoardInteraction On(BoardState board) => new BoardInteraction(board);

        [Test]
        public void Tap_EmptyTubeWithNoSelection_DoesNothing()
        {
            // Selecting an empty tube would arm a move that could never be legal.
            var interaction = On(TestBoards.Board(4, TestBoards.Empty, new[] { 0 }));

            var result = interaction.Tap(0);

            Assert.AreEqual(TapOutcome.None, result.Outcome);
            Assert.IsFalse(interaction.HasSelection);
        }

        [Test]
        public void Tap_NonEmptyTubeWithNoSelection_Selects()
        {
            var interaction = On(TestBoards.Board(4, new[] { 0, 0 }, TestBoards.Empty));

            var result = interaction.Tap(0);

            Assert.AreEqual(TapOutcome.Selected, result.Outcome);
            Assert.AreEqual(0, result.From);
            Assert.AreEqual(0, interaction.SelectedContainer);
        }

        [Test]
        public void Tap_SameTubeTwice_Deselects()
        {
            var interaction = On(TestBoards.Board(4, new[] { 0, 0 }, TestBoards.Empty));
            interaction.Tap(0);

            var result = interaction.Tap(0);

            Assert.AreEqual(TapOutcome.Deselected, result.Outcome);
            Assert.AreEqual(0, result.From);
            Assert.IsFalse(interaction.HasSelection);
        }

        [Test]
        public void Tap_LegalDestination_MovesAndClearsSelection()
        {
            var board = TestBoards.Board(4, new[] { 1, 0, 0 }, new[] { 0 });
            var interaction = On(board);
            interaction.Tap(0);

            var result = interaction.Tap(1);

            Assert.AreEqual(TapOutcome.Moved, result.Outcome);
            Assert.AreEqual(0, result.From);
            Assert.AreEqual(1, result.To);
            Assert.IsFalse(interaction.HasSelection, "Selection must clear once a move lands.");
            Assert.AreEqual(1, board[0].Count);
            Assert.AreEqual(3, board[1].Count);
        }

        [Test]
        public void Tap_LegalDestination_ReportsTheRunCapturedBeforeTheMove()
        {
            // The view cannot recover this after the fact — the source no longer holds the run.
            var board = TestBoards.Board(4, new[] { 1, 0, 0 }, new[] { 0 });
            var interaction = On(board);
            interaction.Tap(0);

            var result = interaction.Tap(1);

            Assert.AreEqual(2, result.Count, "Should report the whole top run, not one ball.");
            Assert.AreEqual(new ColorId(0), result.Color);
        }

        [Test]
        public void Tap_IllegalDestination_RejectsAndKeepsSelection()
        {
            var board = TestBoards.Board(4, new[] { 0 }, new[] { 1 });
            var interaction = On(board);
            interaction.Tap(0);

            var result = interaction.Tap(1);

            Assert.AreEqual(TapOutcome.Rejected, result.Outcome);
            Assert.AreEqual(0, result.From);
            Assert.AreEqual(1, result.To);
            Assert.AreEqual(0, interaction.SelectedContainer,
                "Selection is kept so the player can try another destination immediately.");
            Assert.AreEqual(1, board[0].Count, "A rejected tap must not mutate the board.");
            Assert.AreEqual(1, board[1].Count);
        }

        [TestCase(-1)]
        [TestCase(99)]
        public void Tap_IndexOutOfRange_IsANoOpNotAThrow(int index)
        {
            var interaction = On(TestBoards.Board(4, new[] { 0 }, TestBoards.Empty));

            TapResult result = default;
            Assert.DoesNotThrow(() => result = interaction.Tap(index));
            Assert.AreEqual(TapOutcome.None, result.Outcome);
        }

        [Test]
        public void ClearSelection_DropsSelectionWithoutTouchingTheBoard()
        {
            var board = TestBoards.Board(4, new[] { 0, 0 }, TestBoards.Empty);
            var interaction = On(board);
            interaction.Tap(0);

            interaction.ClearSelection();

            Assert.IsFalse(interaction.HasSelection);
            Assert.AreEqual(2, board[0].Count);
        }

        [Test]
        public void Constructor_NullBoard_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new BoardInteraction(null));
        }

        /// <summary>
        /// The headline test: every move the generator considers legal must be reachable through the
        /// tap interface. A tap rule that quietly forbids one would make some levels unwinnable, and
        /// nothing else in the suite would notice.
        /// <para>
        /// Asserting each second tap returns <see cref="TapOutcome.Moved"/> is what makes this
        /// non-vacuous. A rejected tap keeps the selection, so a wrong rule silently reinterprets the
        /// following tap — checking only the final board could still stumble into a solved state.
        /// </para>
        /// </summary>
        [TestCase(3, 4, 1, 40)]
        [TestCase(4, 4, 2, 60)]
        [TestCase(6, 5, 2, 120)]
        public void Tap_CanDriveAGeneratedLevelToSolved(int colors, int capacity, int empties, int steps)
        {
            var parameters = new LevelParams(colors, capacity, empties, steps);

            for (var seed = 0; seed < 25; seed++)
            {
                var level = LevelGenerator.Generate(parameters, seed);
                var interaction = new BoardInteraction(level.Board.Clone());

                for (var i = 0; i < level.Solution.Count; i++)
                {
                    var move = level.Solution[i];

                    var pick = interaction.Tap(move.From);
                    Assert.AreEqual(TapOutcome.Selected, pick.Outcome,
                        $"seed {seed} step {i}: could not select source {move.From}.");

                    var drop = interaction.Tap(move.To);
                    Assert.AreEqual(TapOutcome.Moved, drop.Outcome,
                        $"seed {seed} step {i}: tap rules refused legal move {move}.");
                }

                Assert.IsTrue(interaction.IsSolved, $"seed {seed}: taps did not solve the board.");
                Assert.IsFalse(interaction.HasSelection);
            }
        }
    }
}
