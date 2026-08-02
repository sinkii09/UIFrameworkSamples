using System;
using System.Collections.Generic;
using ColorStackSort.Logic;
using NUnit.Framework;

namespace ColorStackSort.Tests
{
    [TestFixture]
    public class DifficultyCurveTests
    {
        [Test]
        public void ForLevel_BelowFirstLevel_Throws()
        {
            // Levels are 1-based. A 0 here would mean an off-by-one somewhere upstream, and
            // silently serving level 1 would hide it.
            Assert.Throws<ArgumentOutOfRangeException>(() => DifficultyCurve.ForLevel(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => DifficultyCurve.ForLevel(-1));
        }

        [Test]
        public void ForLevel_AcrossTwoHundredLevels_AlwaysProducesValidParams()
        {
            // Validate() throws on any out-of-range knob, so a retune that breaks the curve fails
            // here rather than deep inside the generator.
            for (var level = DifficultyCurve.FirstLevel; level <= 200; level++)
            {
                var parameters = DifficultyCurve.ForLevel(level);
                Assert.DoesNotThrow(parameters.Validate, $"level {level} produced {parameters}");
            }
        }

        [Test]
        public void ForLevel_AcrossTwoHundredLevels_GeneratesASolvableBoard()
        {
            // Spot-checked rather than exhaustive: generation is the expensive part, and the
            // interesting boundaries are the colour steps and the spare-container drop.
            foreach (var level in new[] { 1, 4, 5, 9, 10, 11, 21, 50, 200 })
            {
                var parameters = DifficultyCurve.ForLevel(level);
                var seed = DifficultyCurve.SeedForLevel(level, 0);

                var generated = LevelGenerator.Generate(parameters, seed);

                // Asserting the solution, not merely that generation returned. "DoesNotThrow" would
                // let this test keep its name while proving nothing about solvability.
                Assert.IsNotNull(generated.Board, $"level {level} ({parameters}) produced no board");
                Assert.IsNotEmpty(
                    generated.Solution, $"level {level} ({parameters}) generated a level with no solution");
            }
        }

        [Test]
        public void ForLevel_ColorCount_RisesMonotonicallyAndClamps()
        {
            var previous = 0;
            for (var level = DifficultyCurve.FirstLevel; level <= 200; level++)
            {
                var colors = DifficultyCurve.ForLevel(level).ColorCount;

                Assert.GreaterOrEqual(colors, previous, $"colour count fell at level {level}");
                Assert.LessOrEqual(colors, 8, $"colour count overran at level {level}");
                previous = colors;
            }

            Assert.AreEqual(3, DifficultyCurve.ForLevel(1).ColorCount);
            Assert.AreEqual(8, DifficultyCurve.ForLevel(200).ColorCount, "should have clamped long before 200");
        }

        [Test]
        public void ForLevel_SpareContainers_DropFromTwoToOne()
        {
            Assert.AreEqual(2, DifficultyCurve.ForLevel(1).EmptyContainers);
            Assert.AreEqual(2, DifficultyCurve.ForLevel(9).EmptyContainers);
            Assert.AreEqual(1, DifficultyCurve.ForLevel(10).EmptyContainers);
        }

        [Test]
        public void ForLevel_IsPure()
        {
            // Restart depends on this: asking twice must describe the same board, or the player
            // gets a different puzzle than the one they just failed.
            Assert.AreEqual(
                DifficultyCurve.ForLevel(7).ToString(),
                DifficultyCurve.ForLevel(7).ToString());
        }

        [Test]
        public void SeedForLevel_IsDeterministic()
        {
            Assert.AreEqual(DifficultyCurve.SeedForLevel(12, 99), DifficultyCurve.SeedForLevel(12, 99));
        }

        [Test]
        public void SeedForLevel_DiffersPerLevel()
        {
            var seen = new HashSet<int>();
            for (var level = DifficultyCurve.FirstLevel; level <= 200; level++)
                Assert.IsTrue(seen.Add(DifficultyCurve.SeedForLevel(level, 0)), $"seed collision at level {level}");
        }

        [Test]
        public void SeedForLevel_BaseSeedReshufflesEveryLevel()
        {
            // The salt is how a reported bad board gets retired without touching the curve.
            Assert.AreNotEqual(DifficultyCurve.SeedForLevel(3, 0), DifficultyCurve.SeedForLevel(3, 1));
        }

        [Test]
        public void SeedForLevel_ExtremeBaseSeed_DoesNotThrow()
        {
            // The multiply is unchecked on purpose; overflow wraps rather than throwing.
            Assert.DoesNotThrow(() => DifficultyCurve.SeedForLevel(1, int.MaxValue));
            Assert.DoesNotThrow(() => DifficultyCurve.SeedForLevel(1, int.MinValue));
        }
    }
}
