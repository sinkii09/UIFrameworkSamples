using System;
using System.Collections.Generic;

namespace AstralChorus.Logic
{
    /// <summary>
    /// Runs many players at several Shard budgets and reports the distribution.
    /// <para>
    /// Every player is simulated once to the largest budget and snapshotted along the way, so the
    /// budgets are nested samples of the SAME player rather than independent populations. That is
    /// deliberate: it makes "what does another 240 Shard buy this person" answerable, which two
    /// independent runs could not do.
    /// </para>
    /// </summary>
    public sealed class EconomySimulator
    {
        private readonly EconomyConfig _config;

        public EconomySimulator(EconomyConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <param name="focusRarity">Rarity the player focuses for the whole run, or -1 for none.</param>
        public IReadOnlyDictionary<int, SimulationReport> Run(
            IReadOnlyList<int> shardBudgets,
            int playerCount,
            int seed,
            int focusRarity = -1,
            int focusIndex = 0)
        {
            if (shardBudgets == null) throw new ArgumentNullException(nameof(shardBudgets));
            if (shardBudgets.Count == 0)
                throw new ArgumentException("At least one budget is required.", nameof(shardBudgets));
            if (playerCount < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(playerCount), playerCount, "Need at least one player.");

            var rosterTotal = 0;
            for (var rarity = EconomyConfig.LowestRarity; rarity <= EconomyConfig.HighestRarity; rarity++)
                rosterTotal += _config.RosterSizeOf(rarity);

            var reports = new Dictionary<int, SimulationReport>(shardBudgets.Count);
            for (var i = 0; i < shardBudgets.Count; i++)
                reports[shardBudgets[i]] = new SimulationReport(shardBudgets[i], rosterTotal);

            // One generator for the whole population: each player continues the stream rather than
            // reseeding, so two players never share a pull sequence the way per-player seeding
            // derived from an index can accidentally arrange.
            var rng = new DeterministicRandom(seed);
            var engine = new PullEngine(_config);

            for (var player = 0; player < playerCount; player++)
            {
                var snapshots = engine.RunWithCheckpoints(shardBudgets, rng, focusRarity, focusIndex);

                foreach (var pair in snapshots)
                {
                    var cascade = ResonanceCascade.Run(_config, pair.Value);
                    reports[pair.Key].Add(pair.Value, cascade);
                }
            }

            return reports;
        }

        /// <summary>
        /// Mean hits per rarity at one budget, in the shape <see cref="AnchorTable.Build"/> wants.
        /// </summary>
        public static double[] MeanHits(SimulationReport report)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));

            var means = new double[EconomyConfig.RarityCount];
            for (var rarity = EconomyConfig.LowestRarity; rarity <= EconomyConfig.HighestRarity; rarity++)
                means[EconomyConfig.RarityIndex(rarity)] = report.MeanHits(rarity);
            return means;
        }

        /// <summary>
        /// Smallest budget in <paramref name="candidates"/> at which every simulated player maxes the
        /// whole roster. Returns -1 if none does, so a caller cannot mistake "off the end of the
        /// sweep" for "the last candidate worked".
        /// </summary>
        /// <remarks>
        /// <b>This asks for a zero-failure observation, so its answer is only as sharp as
        /// <paramref name="playerCount"/> allows.</b> By the rule of three, seeing zero failures in N
        /// players bounds the true failure rate at roughly 3/N with 95% confidence — and nothing
        /// tighter. At N=8,000 that bound is 0.04%, which is looser than the differences between
        /// adjacent budgets near the answer.
        /// <para>
        /// Measured, 100,000 players x 3 seeds, at the shipped 10:1 rate: 340 fails for 2.3%, 360 for
        /// 0.19%, 380 for 0.018%, 400 for 0.002%, 420 for 0.000%. An 8,000-player sweep reports 380 as
        /// "everyone" perhaps a quarter of the time, purely because 0.018% of 8,000 is 1.4 expected
        /// failures. The design uses 420. Use this method for coarse comparisons between models, where
        /// the gap between candidates is percent-scale; do not use it to pick a shipping constant
        /// unless N is large enough to resolve the gap you care about.
        /// </para>
        /// </remarks>
        public int SmallestBudgetMaxingEveryone(
            IReadOnlyList<int> candidates, int playerCount, int seed)
        {
            var reports = Run(candidates, playerCount, seed);

            var ordered = new List<int>(candidates);
            ordered.Sort();

            for (var i = 0; i < ordered.Count; i++)
            {
                // Exact, not "close to 1": this is the figure the Ascent's payout is derived from,
                // and a 99.9% reading would move that payout by tens of Shard.
                if (reports[ordered[i]].AllMaxedShare >= 1.0) return ordered[i];
            }

            return -1;
        }
    }
}
