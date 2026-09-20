using System;
using System.Collections.Generic;

namespace AstralChorus.Logic
{
    /// <summary>
    /// Per-player outcomes at one Shard budget, plus the aggregates the GDD quotes.
    /// </summary>
    public sealed class SimulationReport
    {
        private readonly List<int> _totalMaxed = new List<int>();
        private readonly List<int> _catalyst = new List<int>();
        private readonly List<int>[] _maxedPerRarity;
        private readonly double[] _hitTotals = new double[EconomyConfig.RarityCount];

        private int _ownedFullRoster;
        private int _sameConversionRuns;
        private long _focusHits;
        private long _focusChances;

        public SimulationReport(int shardBudget, int rosterTotal)
        {
            ShardBudget = shardBudget;
            RosterTotal = rosterTotal;

            _maxedPerRarity = new List<int>[EconomyConfig.RarityCount];
            for (var i = 0; i < EconomyConfig.RarityCount; i++) _maxedPerRarity[i] = new List<int>();
        }

        public int ShardBudget { get; }
        public int RosterTotal { get; }
        public int PlayerCount => _totalMaxed.Count;

        public void Add(PullOutcome outcome, CascadeResult cascade)
        {
            if (outcome == null) throw new ArgumentNullException(nameof(outcome));
            if (cascade == null) throw new ArgumentNullException(nameof(cascade));

            _totalMaxed.Add(cascade.TotalMaxed);
            _catalyst.Add(cascade.CatalystSpent);

            for (var i = 0; i < EconomyConfig.RarityCount; i++)
            {
                _maxedPerRarity[i].Add(cascade.MaxedPerRarity[i]);
                _hitTotals[i] += outcome.Hits[i];
            }

            if (outcome.TotalOwned() == RosterTotal) _ownedFullRoster++;
            if (cascade.SameRarityConversions > 0) _sameConversionRuns++;

            _focusHits += outcome.FocusHits;
            _focusChances += outcome.FocusChances;
        }

        public double MeanHits(int rarity) =>
            _hitTotals[EconomyConfig.RarityIndex(rarity)] / Math.Max(1, PlayerCount);

        /// <summary>Share of players owning every character. Expected to be exactly 1 — GDD §3.</summary>
        public double OwnFullRosterShare => Share(_ownedFullRoster);

        public double AllMaxedShare
        {
            get
            {
                var count = 0;
                for (var i = 0; i < _totalMaxed.Count; i++)
                    if (_totalMaxed[i] == RosterTotal) count++;
                return Share(count);
            }
        }

        /// <summary>Share of players who maxed EVERY character of one rarity.</summary>
        public double FullyMaxedShare(int rarity, int rosterSize)
        {
            var list = _maxedPerRarity[EconomyConfig.RarityIndex(rarity)];
            var count = 0;
            for (var i = 0; i < list.Count; i++)
                if (list[i] >= rosterSize) count++;
            return Share(count);
        }

        /// <summary>Share of players who maxed NONE of one rarity. The ★5 figure behind risk 13.</summary>
        public double ZeroMaxedShare(int rarity)
        {
            var list = _maxedPerRarity[EconomyConfig.RarityIndex(rarity)];
            var count = 0;
            for (var i = 0; i < list.Count; i++)
                if (list[i] == 0) count++;
            return Share(count);
        }

        /// <summary>Share of runs where any sideways conversion happened at all.</summary>
        public double SameRarityConversionShare => Share(_sameConversionRuns);

        public double FocusHitRate => _focusChances == 0 ? 0.0 : (double)_focusHits / _focusChances;

        public int TotalMaxedPercentile(double quantile) => Percentile(_totalMaxed, quantile);

        public int CatalystPercentile(double quantile) => Percentile(_catalyst, quantile);

        public int MaxedPercentile(int rarity, double quantile) =>
            Percentile(_maxedPerRarity[EconomyConfig.RarityIndex(rarity)], quantile);

        private double Share(int count) => PlayerCount == 0 ? 0.0 : (double)count / PlayerCount;

        /// <summary>
        /// Nearest-rank percentile: the smallest value at or below which at least
        /// <paramref name="quantile"/> of the data sits, i.e. the ⌈q·N⌉-th smallest, 1-indexed.
        /// <para>
        /// Stated explicitly because the obvious <c>(int)(q · N)</c> is the floor convention and sits
        /// one rank higher for every non-integer product. Every percentile this sim publishes lands
        /// on a saturated or flat part of its distribution, so the two conventions agree on all of
        /// them — which means no assertion here could ever have caught the difference. Sorting a copy
        /// keeps repeated queries independent of the order a previous one left behind.
        /// </para>
        /// </summary>
        private static int Percentile(List<int> values, double quantile)
        {
            if (values.Count == 0)
                throw new InvalidOperationException("No players recorded.");
            if (quantile < 0.0 || quantile > 1.0)
                throw new ArgumentOutOfRangeException(nameof(quantile), quantile, "Must be in [0, 1].");

            var sorted = new List<int>(values);
            sorted.Sort();

            var rank = (int)Math.Ceiling(quantile * sorted.Count) - 1;
            if (rank < 0) rank = 0;
            if (rank >= sorted.Count) rank = sorted.Count - 1;
            return sorted[rank];
        }

        /// <summary>Exposed so the percentile convention itself can be tested on known data.</summary>
        public static int PercentileOf(IReadOnlyList<int> values, double quantile)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            return Percentile(new List<int>(values), quantile);
        }
    }
}
