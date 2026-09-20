using System;

namespace AstralChorus.Logic
{
    /// <summary>How much repeatable grinding a full completion costs.</summary>
    public sealed class GrindResult
    {
        public GrindResult(int runs, double hours, long dustRequired, int catalystBought)
        {
            Runs = runs;
            Hours = hours;
            DustRequired = dustRequired;
            CatalystBought = catalystBought;
        }

        public int Runs { get; }
        public double Hours { get; }

        /// <summary>Total Dust the player must end up holding: levels plus purchased Catalyst.</summary>
        public long DustRequired { get; }

        /// <summary>Catalyst that had to be bought rather than dropped.</summary>
        public int CatalystBought { get; }
    }

    /// <summary>
    /// Solves Catalyst price and Dust income TOGETHER (GDD §10).
    /// <para>
    /// They are one problem, not two: Catalyst is bought with Dust, so raising the price and lowering
    /// the income both lengthen the same grind, and tuning either alone gives an answer that is wrong
    /// by the amount the other one moved.
    /// </para>
    /// <para>
    /// <paramref name="catalystPerRun"/> models Catalyst dropped by game modes. The design forbids it
    /// — this parameter exists so the test suite can keep demonstrating WHY: at a quarter of a
    /// Catalyst per run the shop price stops changing the answer at all.
    /// </para>
    /// </summary>
    public static class DustGrindSolver
    {
        private const int MaxRuns = 1_000_000;

        public static GrindResult Solve(EconomyConfig config, int catalystNeeded,
                                        double catalystPerRun = 0.0)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (catalystNeeded < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(catalystNeeded), catalystNeeded, "Cannot need negative Catalyst.");
            // NaN fails every comparison, so a bare `< 0.0` check waves it through — and then
            // Math.Ceiling(NaN) casts to int.MinValue, which makes the required Dust hugely negative
            // and hands back "0 runs" as a confident answer. Reject it by shape, not by comparison.
            if (double.IsNaN(catalystPerRun) || double.IsInfinity(catalystPerRun))
                throw new ArgumentOutOfRangeException(
                    nameof(catalystPerRun), catalystPerRun, "Must be a finite number.");
            if (catalystPerRun < 0.0)
                throw new ArgumentOutOfRangeException(
                    nameof(catalystPerRun), catalystPerRun, "Cannot drop negative Catalyst.");
            if (config.DustPerRun < 1)
                throw new ArgumentException("Dust per run must be positive.", nameof(config));

            // A negative price makes the required Dust GROW with the run count, which breaks the
            // monotonicity the binary search below depends on — it would return a non-minimal answer
            // rather than fail.
            if (config.CatalystPriceDust < 0)
                throw new ArgumentException("Catalyst price must not be negative.", nameof(config));

            if (!IsAffordable(config, catalystNeeded, catalystPerRun, MaxRuns))
                throw new InvalidOperationException(
                    "No run count under " + MaxRuns + " satisfies this configuration.");

            // Affordability is monotone in the run count — more runs means more Dust and, when modes
            // drop Catalyst, fewer to buy — so binary search finds the first feasible count exactly.
            var low = 0;
            var high = MaxRuns;
            while (low < high)
            {
                var mid = low + (high - low) / 2;
                if (IsAffordable(config, catalystNeeded, catalystPerRun, mid)) high = mid;
                else low = mid + 1;
            }

            var bought = CatalystToBuy(catalystNeeded, catalystPerRun, low);
            var dust = (long)config.LevelDustTotal + (long)bought * config.CatalystPriceDust;
            return new GrindResult(low, low * config.RunMinutes / 60.0, dust, bought);
        }

        private static bool IsAffordable(EconomyConfig config, int catalystNeeded,
                                         double catalystPerRun, int runs)
        {
            var dustEarned = (long)runs * config.DustPerRun + config.CampaignDust;
            var bought = CatalystToBuy(catalystNeeded, catalystPerRun, runs);
            var dustRequired = (long)config.LevelDustTotal + (long)bought * config.CatalystPriceDust;
            return dustEarned >= dustRequired;
        }

        private static int CatalystToBuy(int catalystNeeded, double catalystPerRun, int runs)
        {
            var dropped = catalystPerRun * runs;
            var shortfall = catalystNeeded - dropped;
            return shortfall <= 0.0 ? 0 : (int)Math.Ceiling(shortfall);
        }
    }
}
