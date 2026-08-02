using ColorStackSort.Logic;
using NUnit.Framework;

namespace ColorStackSort.Tests
{
    /// <summary>
    /// Specific defects that must not come back. Kept apart from
    /// <see cref="LevelGeneratorTests"/>, which asserts general properties — these each pin one
    /// bug that shipped green through the general suite.
    /// </summary>
    [TestFixture]
    public class LevelGeneratorRegressionTests
    {
        /// <summary>
        /// A dead difficulty knob. The scramble used to abandon itself the first time the
        /// immediate-undo filter emptied the candidate list, which capped a 4-colour board at ~11.7
        /// moves no matter how large ScrambleSteps was — level 5 and level 500 would have been the
        /// same puzzle. The old assertion was `LessOrEqual(length, budget)`, which passed happily at
        /// 9-of-40; the name promised an equality it never checked. This asserts the budget is
        /// actually reachable.
        /// </summary>
        [Test]
        public void Generate_LargeBudget_IsNotCappedByAStalledScramble()
        {
            var parameters = new LevelParams(4, 4, 2, 200);
            long total = 0;
            var longest = 0;

            for (var seed = 0; seed < 60; seed++)
            {
                var length = LevelGenerator.Generate(parameters, seed).Solution.Count;
                total += length;
                if (length > longest) longest = length;
            }

            Assert.Greater(total / 60.0, 25.0,
                "Average solution length collapsed back towards the old ~11.7 ceiling.");
            Assert.Greater(longest, 60, "No seed got anywhere near the budget.");
        }

        /// <summary>
        /// A one-step scramble often lands back on a solved board — moving a whole colour into an
        /// empty container leaves every container monochrome and unique. Restarting from scratch
        /// only 8 times left ~0.4% of seeds throwing on completely valid params, which would have
        /// crashed specific tutorial levels once Phase 4 maps level index to seed.
        /// </summary>
        [TestCase(2, 2, 1, 184)]
        [TestCase(2, 2, 2, 184)]
        [TestCase(3, 2, 1, 116)]
        [TestCase(2, 3, 1, 13919)]
        public void Generate_SingleScrambleStep_DoesNotThrow(int colors, int capacity, int empties, int seed)
        {
            var parameters = new LevelParams(colors, capacity, empties, 1);

            var level = LevelGenerator.Generate(parameters, seed);

            Assert.IsFalse(level.Board.IsSolved);
            Assert.Greater(level.Solution.Count, 0);
        }

        [Test]
        public void Generate_MinimalParamsAcrossManySeeds_NeverThrows()
        {
            var parameters = new LevelParams(2, 2, 1, 1);

            for (var seed = 0; seed < 3000; seed++)
            {
                Assert.DoesNotThrow(() => LevelGenerator.Generate(parameters, seed),
                    $"seed {seed} threw on valid params.");
            }
        }

        /// <summary>
        /// Pins one whole board, so a change to the scramble or the PRNG cannot silently rewrite
        /// every level in the game. Level identity is the seed and nothing is stored, so there is
        /// no other tripwire for that.
        /// </summary>
        [Test]
        public void Generate_KnownSeed_ProducesTheRecordedBoard()
        {
            var level = LevelGenerator.Generate(new LevelParams(3, 4, 1, 40), 12345);

            Assert.AreEqual("0,|1,1,1,2,|2,2,0,|0,1,2,0,|", TestBoards.Describe(level.Board));
        }
    }
}
