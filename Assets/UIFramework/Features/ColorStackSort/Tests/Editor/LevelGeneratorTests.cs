using System;
using System.Collections.Generic;
using ColorStackSort.Logic;
using NUnit.Framework;

namespace ColorStackSort.Tests
{
    [TestFixture]
    public class LevelGeneratorTests
    {
        private const int SeedsPerParams = 40;

        /// <summary>A spread from the smallest legal board up to a realistic late-game one.</summary>
        private static readonly LevelParams[] ParamSpread =
        {
            new LevelParams(2, 3, 1, 20),
            new LevelParams(3, 4, 1, 40),
            new LevelParams(4, 4, 2, 60),
            new LevelParams(6, 5, 2, 120),
            new LevelParams(8, 4, 3, 200)
        };

        /// <summary>
        /// The test that earns its keep. Replays each generated level's recorded solution and
        /// asserts every step is legal under the PLAYER's rule and the board ends solved.
        /// <para>
        /// This verifies the invertibility argument empirically rather than trusting the reasoning
        /// in the design doc — which was wrong on its first draft, and would be wrong again here
        /// the moment either move rule drifts.
        /// </para>
        /// </summary>
        [TestCaseSource(nameof(ParamSpread))]
        public void ReplayingSolution_ClearsBoard(LevelParams parameters)
        {
            for (var seed = 0; seed < SeedsPerParams; seed++)
            {
                var level = LevelGenerator.Generate(parameters, seed);

                // Clone, so assertions after the loop still see the level as it was handed out.
                var board = level.Board.Clone();

                Assert.Greater(level.Solution.Count, 0, $"[{parameters}] seed {seed}: empty solution.");

                for (var step = 0; step < level.Solution.Count; step++)
                {
                    var move = level.Solution[step];
                    Assert.IsTrue(board.IsLegal(move),
                        $"[{parameters}] seed {seed}: solution step {step} ({move}) is not a legal player move.");

                    board.Apply(move);
                }

                Assert.IsTrue(board.IsSolved,
                    $"[{parameters}] seed {seed}: replaying the whole solution did not solve the board.");

                Assert.IsFalse(level.Board.IsSolved,
                    $"[{parameters}] seed {seed}: replay mutated the level's own board.");
            }
        }

        [TestCaseSource(nameof(ParamSpread))]
        public void Generate_ConservesColourCountsAndCapacity(LevelParams parameters)
        {
            for (var seed = 0; seed < SeedsPerParams; seed++)
            {
                var board = LevelGenerator.Generate(parameters, seed).Board;
                var counts = new int[parameters.ColorCount];

                for (var i = 0; i < board.ContainerCount; i++)
                {
                    var container = board[i];
                    Assert.LessOrEqual(container.Count, parameters.Capacity,
                        $"[{parameters}] seed {seed}: container {i} exceeds capacity.");

                    for (var j = 0; j < container.Count; j++) counts[container[j].Value]++;
                }

                for (var color = 0; color < counts.Length; color++)
                {
                    Assert.AreEqual(parameters.ItemsPerColor, counts[color],
                        $"[{parameters}] seed {seed}: colour {color} count changed during scrambling.");
                }
            }
        }

        [TestCaseSource(nameof(ParamSpread))]
        public void Generate_NeverReturnsAnAlreadySolvedBoard(LevelParams parameters)
        {
            for (var seed = 0; seed < SeedsPerParams; seed++)
            {
                Assert.IsFalse(LevelGenerator.Generate(parameters, seed).Board.IsSolved,
                    $"[{parameters}] seed {seed}: handed back a level that is already won.");
            }
        }

        [TestCaseSource(nameof(ParamSpread))]
        public void Generate_SameSeed_ProducesAnIdenticalBoard(LevelParams parameters)
        {
            // Determinism is what lets a level index map to a seed, so no level data is ever stored.
            var first = TestBoards.Describe(LevelGenerator.Generate(parameters, 12345).Board);
            var second = TestBoards.Describe(LevelGenerator.Generate(parameters, 12345).Board);

            Assert.AreEqual(first, second);
        }

        [Test]
        public void Generate_DifferentSeeds_ProduceDifferentBoards()
        {
            var parameters = new LevelParams(6, 5, 2, 120);
            var distinct = new HashSet<string>();

            for (var seed = 0; seed < 20; seed++)
                distinct.Add(TestBoards.Describe(LevelGenerator.Generate(parameters, seed).Board));

            // Not asserting all 20 differ — collisions are astronomically unlikely on a board this
            // size but not impossible, and a flaky test is worse than a slightly loose one.
            Assert.GreaterOrEqual(distinct.Count, 15, "Seeds are barely affecting the output.");
        }

        [TestCase(1, 4, 1, 10, TestName = "Generate_FewerThanTwoColours_Throws")]
        [TestCase(2, 1, 1, 10, TestName = "Generate_CapacityBelowTwo_Throws")]
        [TestCase(2, 4, 0, 10, TestName = "Generate_NoEmptyContainer_Throws")]
        [TestCase(2, 4, 1, 0, TestName = "Generate_NoScrambleSteps_Throws")]
        public void Generate_InvalidParams_Throws(int colors, int capacity, int empties, int steps)
        {
            var parameters = new LevelParams(colors, capacity, empties, steps);

            Assert.Throws<ArgumentException>(() => LevelGenerator.Generate(parameters, 0));
        }

        [Test]
        public void Generate_MetadataMatchesTheRequest()
        {
            var parameters = new LevelParams(3, 4, 1, 40);
            var level = LevelGenerator.Generate(parameters, 99);

            Assert.AreEqual(99, level.Seed);
            Assert.AreEqual(parameters.ContainerCount, level.Board.ContainerCount);
            Assert.AreEqual(parameters.ColorCount, level.Params.ColorCount);
        }
    }
}
