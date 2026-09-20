using AstralChorus.Logic;
using NUnit.Framework;

namespace AstralChorus.Tests
{
    /// <summary>
    /// The grind figure in GDD §10. Integer arithmetic throughout, so every assertion is exact.
    /// </summary>
    [TestFixture]
    public sealed class DustGrindSolverTests
    {
        private const int CeilingCatalyst = 76;   // p90 at the 420-Shard ceiling

        [Test]
        public void FullCompletion_CostsTwentyTwoHoursAtTheDesignedRate()
        {
            var config = new EconomyConfig();
            var result = DustGrindSolver.Solve(config, CeilingCatalyst);

            // 76 x 1,200 = 91,200 Catalyst Dust; + 215,000 levels = 306,200 required;
            // - 40,000 campaign = 266,200 to farm; / 600 per run = 444 runs; x 3 min = 22.2 h.
            Assert.That(result.CatalystBought, Is.EqualTo(76));
            Assert.That(result.DustRequired, Is.EqualTo(306_200));
            Assert.That(result.Runs, Is.EqualTo(444));
            Assert.That(result.Hours, Is.EqualTo(22.2).Within(0.05));
        }

        [Test]
        public void TheDustRateIsTheLever_NotTheCatalystPrice()
        {
            var config = new EconomyConfig();
            var baseline = DustGrindSolver.Solve(config, CeilingCatalyst);

            var halfIncome = config.Clone();
            halfIncome.DustPerRun = 300;

            var cheaper = config.Clone();
            cheaper.CatalystPriceDust = 900;

            var incomeEffect = DustGrindSolver.Solve(halfIncome, CeilingCatalyst).Hours - baseline.Hours;
            var priceEffect = baseline.Hours - DustGrindSolver.Solve(cheaper, CeilingCatalyst).Hours;

            // Halving the income adds 22.2 h; cutting the price by a quarter saves 1.9 h. The price
            // is very nearly an inert knob, which is why GDD §10 tells the reader not to reach for it.
            Assert.That(incomeEffect, Is.EqualTo(22.2).Within(0.05));
            Assert.That(priceEffect, Is.EqualTo(1.9).Within(0.05));
            Assert.That(incomeEffect, Is.GreaterThan(priceEffect * 10));
        }

        [Test]
        public void RejectedModel_ModeDroppedCatalystErasesTheShopPriceEntirely()
        {
            // A regression on a DESIGN decision. This is the measurement that removed GrantCatalyst
            // from IModeRewardSink: once modes drop a quarter of a Catalyst per run, the Dust
            // constraint binds first and the shop price almost stops mattering. A shop whose price
            // moves the grind by six minutes out of fifteen hours is decoration, not a sink.
            var prices = new[] { 600, 900, 1_200, 1_500 };

            Assert.That(SpreadAcrossPrices(prices, 0.00), Is.EqualTo(5.70).Within(0.01),
                "Without mode drops the price is worth 5.7 h across this range.");
            Assert.That(SpreadAcrossPrices(prices, 0.25), Is.EqualTo(0.10).Within(0.01),
                "With mode drops the same range is worth 0.1 h — a 57x collapse.");
        }

        /// <summary>Widest minus narrowest grind across a set of Catalyst prices.</summary>
        private static double SpreadAcrossPrices(int[] prices, double catalystPerRun)
        {
            var lowest = double.MaxValue;
            var highest = double.MinValue;

            foreach (var price in prices)
            {
                var variant = new EconomyConfig { CatalystPriceDust = price };
                var hours = DustGrindSolver.Solve(variant, CeilingCatalyst, catalystPerRun).Hours;
                if (hours < lowest) lowest = hours;
                if (hours > highest) highest = hours;
            }

            return highest - lowest;
        }

        [Test]
        public void NoCatalystNeeded_CostsOnlyTheLevellingDust()
        {
            var config = new EconomyConfig();
            var result = DustGrindSolver.Solve(config, 0);

            // (215,000 - 40,000) / 600 = 291.67 -> 292 runs.
            Assert.That(result.CatalystBought, Is.Zero);
            Assert.That(result.DustRequired, Is.EqualTo(215_000));
            Assert.That(result.Runs, Is.EqualTo(292));
        }

        [Test]
        public void SolvedRunCount_IsTheSmallestThatWorks()
        {
            var config = new EconomyConfig();
            var result = DustGrindSolver.Solve(config, CeilingCatalyst);

            var earned = (long)result.Runs * config.DustPerRun + config.CampaignDust;
            var oneFewer = (long)(result.Runs - 1) * config.DustPerRun + config.CampaignDust;

            Assert.That(earned, Is.GreaterThanOrEqualTo(result.DustRequired));
            Assert.That(oneFewer, Is.LessThan(result.DustRequired),
                "Binary search must land on the first feasible run count, not merely a feasible one.");
        }
    }
}
