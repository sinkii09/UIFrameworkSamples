using AstralChorus.Logic;
using NUnit.Framework;

namespace AstralChorus.Tests
{
    /// <summary>
    /// The conventions and guards that no published assertion could ever have exercised.
    /// <para>
    /// Every percentile this sim reports sits on a saturated or flat part of its distribution, and
    /// every solver call it makes passes sane inputs — so the percentile convention could have been
    /// off by one rank, and the solver could have answered nonsense for NaN, with the whole suite
    /// green. These are the cases that can actually fail.
    /// </para>
    /// </summary>
    [TestFixture]
    public sealed class InputGuardTests
    {
        [Test]
        public void Percentile_UsesNearestRank_OnDataThatCanTellTheDifference()
        {
            var values = new[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };

            Assert.That(SimulationReport.PercentileOf(values, 0.00), Is.EqualTo(10));
            Assert.That(SimulationReport.PercentileOf(values, 0.01), Is.EqualTo(10));
            Assert.That(SimulationReport.PercentileOf(values, 0.10), Is.EqualTo(10));
            Assert.That(SimulationReport.PercentileOf(values, 0.50), Is.EqualTo(50));
            Assert.That(SimulationReport.PercentileOf(values, 0.90), Is.EqualTo(90));
            Assert.That(SimulationReport.PercentileOf(values, 1.00), Is.EqualTo(100));

            // The two conventions part company exactly when q x N is a whole number: nearest-rank
            // takes the (q·N)-th smallest, the floor convention `(int)(q * N)` takes the index one
            // past it. A fractional product rounds both to the same place, which is why 0.25 and 0.35
            // would prove nothing here.
            Assert.That(SimulationReport.PercentileOf(values, 0.30), Is.EqualTo(30),
                "0.30 x 10 = 3: nearest-rank gives the 3rd smallest (30), floor gives index 3 (40).");
            Assert.That(SimulationReport.PercentileOf(values, 0.70), Is.EqualTo(70),
                "0.70 x 10 = 7: nearest-rank gives 70, floor gives 80.");
        }

        [Test]
        public void Percentile_RejectsQuantilesOutsideTheUnitInterval()
        {
            var values = new[] { 1, 2, 3 };

            Assert.That(() => SimulationReport.PercentileOf(values, -0.01),
                Throws.InstanceOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => SimulationReport.PercentileOf(values, 1.01),
                Throws.InstanceOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void DustGrindSolver_RejectsNonFiniteCatalystDropRates()
        {
            // NaN fails every comparison, so a bare `< 0` guard waves it through; Math.Ceiling(NaN)
            // then casts to int.MinValue, the required Dust goes hugely negative, and the solver
            // answers "0 runs" with complete confidence.
            var config = new EconomyConfig();

            Assert.That(() => DustGrindSolver.Solve(config, 76, double.NaN),
                Throws.InstanceOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => DustGrindSolver.Solve(config, 76, double.PositiveInfinity),
                Throws.InstanceOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => DustGrindSolver.Solve(config, 76, -0.1),
                Throws.InstanceOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void DustGrindSolver_RejectsANegativePriceThatWouldBreakItsBinarySearch()
        {
            // With a negative price the required Dust GROWS as runs accumulate, so affordability stops
            // being monotone — the search would return a non-minimal answer rather than fail.
            var config = new EconomyConfig { CatalystPriceDust = -1 };

            Assert.That(() => DustGrindSolver.Solve(config, 76),
                Throws.InstanceOf<System.ArgumentException>());
        }

        [Test]
        public void PullEngine_RejectsAFocusOutsideItsRarityRoster()
        {
            // The ★5 roster holds three. Index 3 used to survive until the first ★5 pull and then
            // throw IndexOutOfRangeException from inside the pick — a long way from the caller at fault.
            var engine = new PullEngine(new EconomyConfig());
            var rng = new DeterministicRandom(18);

            Assert.That(() => engine.Run(100, rng, 5, 3),
                Throws.InstanceOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => engine.Run(100, rng, 5, -1),
                Throws.InstanceOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => engine.Run(100, rng, 9, 0),
                Throws.InstanceOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void PullEngine_AcceptsNoFocusAndEveryValidFocusIndex()
        {
            var config = new EconomyConfig();
            var engine = new PullEngine(config);
            var rng = new DeterministicRandom(20);

            Assert.That(() => engine.Run(50, rng), Throws.Nothing);

            for (var rarity = EconomyConfig.LowestRarity; rarity <= EconomyConfig.HighestRarity; rarity++)
            {
                for (var index = 0; index < config.RosterSizeOf(rarity); index++)
                {
                    var capturedRarity = rarity;
                    var capturedIndex = index;
                    Assert.That(() => engine.Run(50, rng, capturedRarity, capturedIndex),
                        Throws.Nothing);
                }
            }
        }
    }
}
