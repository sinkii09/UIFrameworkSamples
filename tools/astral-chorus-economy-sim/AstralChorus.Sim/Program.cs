using System;
using System.Collections.Generic;
using AstralChorus.Logic;

namespace AstralChorus.Sim
{
    /// <summary>
    /// Prints the economy tables in the shape GDD §10–§11 publishes them, so re-tuning a number is a
    /// one-command operation rather than an archaeology exercise:
    /// <code>dotnet run --project tools/astral-chorus-economy-sim/AstralChorus.Sim.csproj</code>
    /// </summary>
    internal static class Program
    {
        private const int Players = 30_000;
        private const int Seed = 20260920;

        private static int Main()
        {
            var config = new EconomyConfig();
            var floor = config.CampaignShard;
            var ceiling = config.TotalShardBudget;
            var budgets = new[] { floor, ceiling };

            Console.WriteLine("AstralChorus economy — {0} players, same-rarity {1}:1, cross-rarity {2}:1",
                Players, config.SameRarityConversionCost, config.CrossRarityConversionCost);
            Console.WriteLine("Shard budget: {0} campaign + {1} Ascent ({2} floors x {3}) = {4}\n",
                config.CampaignShard, config.AscentShard, config.AscentFloors,
                config.AscentShardPerFloor, ceiling);

            var reports = new EconomySimulator(config).Run(budgets, Players, Seed);

            foreach (var budget in budgets)
            {
                var report = reports[budget];
                PrintAnchorTable(config, report, budget == floor ? "FLOOR" : "CEILING", budget);
                PrintDistribution(config, report);
            }

            PrintFocus(config);
            PrintGrind(config, reports[ceiling].CatalystPercentile(0.90));
            PrintRejectedModels(config);
            return 0;
        }

        private static void PrintAnchorTable(EconomyConfig config, SimulationReport report,
                                             string label, int budget)
        {
            var means = EconomySimulator.MeanHits(report);
            var rows = AnchorTable.Build(config, means);

            Console.WriteLine(new string('=', 78));
            Console.WriteLine("{0} — {1} pulls   hits: {2:F2} *5 | {3:F2} *4 | {4:F2} *3",
                label, budget, means[2], means[1], means[0]);
            Console.WriteLine("rarity | dup x2 | affinity | inflow | total | need | convert up | leftover");
            foreach (var row in rows)
            {
                Console.WriteLine(" *{0}    | {1,6} | {2,8} | {3,6} | {4,5} | {5,4} | {6,10} | {7,8}",
                    row.Rarity, row.DisplayDuplicates, row.AffinityFragments, row.Inflow,
                    row.DisplayTotal, row.NeedToMax, row.ConvertedUp,
                    row.DisplayShortfall > 0 ? "-" + row.DisplayShortfall : row.DisplayLeftover.ToString());
            }

            Console.WriteLine("Catalyst (mean decomposition): {0}", AnchorTable.CatalystSpent(rows));
        }

        private static void PrintDistribution(EconomyConfig config, SimulationReport report)
        {
            Console.WriteLine("-- per-character Monte-Carlo --");
            Console.WriteLine("  own full roster: {0:P2} | all 14 maxed: {1:P2}",
                report.OwnFullRosterShare, report.AllMaxedShare);
            for (var rarity = EconomyConfig.LowestRarity; rarity <= EconomyConfig.HighestRarity; rarity++)
            {
                Console.WriteLine("  *{0}: all maxed {1,7:P2} | none maxed {2,7:P2} | p01={3} p50={4}",
                    rarity, report.FullyMaxedShare(rarity, config.RosterSizeOf(rarity)),
                    report.ZeroMaxedShare(rarity), report.MaxedPercentile(rarity, 0.01),
                    report.MaxedPercentile(rarity, 0.50));
            }

            Console.WriteLine("  Catalyst p50/p90: {0}/{1} | runs using a sideways conversion: {2:P2}\n",
                report.CatalystPercentile(0.50), report.CatalystPercentile(0.90),
                report.SameRarityConversionShare);
        }

        private static void PrintFocus(EconomyConfig config)
        {
            var reports = new EconomySimulator(config)
                .Run(new[] { config.TotalShardBudget }, 8_000, 4242, EconomyConfig.HighestRarity, 0);
            Console.WriteLine("FOCUS x{0} on the *5 rarity ({1} characters): {2:P1} of contested pulls\n",
                config.FocusWeight, config.RosterSizeOf(EconomyConfig.HighestRarity),
                reports[config.TotalShardBudget].FocusHitRate);
        }

        private static void PrintGrind(EconomyConfig config, int catalystNeeded)
        {
            var result = DustGrindSolver.Solve(config, catalystNeeded);
            Console.WriteLine("GRIND — {0} Catalyst at {1} Dust, {2} Dust/run, {3}k levels, {4}k campaign",
                catalystNeeded, config.CatalystPriceDust, config.DustPerRun,
                config.LevelDustTotal / 1000, config.CampaignDust / 1000);
            Console.WriteLine("  {0} runs = {1:F1} h  ({2}k Dust required)\n",
                result.Runs, result.Hours, result.DustRequired / 1000);
        }

        private static void PrintRejectedModels(EconomyConfig config)
        {
            Console.WriteLine("REJECTED MODELS — kept so a future edit cannot quietly restore them");

            var flat = config.Clone();
            flat.CrossRarityConversionCost = flat.SameRarityConversionCost;
            var sweep = new List<int> { 180, 220, 260, 300, 340, 380, 420 };
            Console.WriteLine("  cross-rarity 5:1 → everyone maxed at {0} Shard (design uses {1}:1 → {2})",
                new EconomySimulator(flat).SmallestBudgetMaxingEveryone(sweep, 8_000, Seed),
                config.CrossRarityConversionCost,
                new EconomySimulator(config).SmallestBudgetMaxingEveryone(sweep, 8_000, Seed));

            Console.Write("  modes dropping 0.25 Catalyst/run, price 600/900/1200/1500 → hours:");
            foreach (var price in new[] { 600, 900, 1200, 1500 })
            {
                var variant = config.Clone();
                variant.CatalystPriceDust = price;
                Console.Write(" {0:F1}", DustGrindSolver.Solve(variant, 76, 0.25).Hours);
            }

            Console.WriteLine("   (5.7 h of spread collapses to 0.1 h -> the shop price stops mattering)");
        }
    }
}
