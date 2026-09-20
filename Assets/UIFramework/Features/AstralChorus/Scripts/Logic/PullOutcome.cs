using System;

namespace AstralChorus.Logic
{
    /// <summary>
    /// One player's pull history, held PER CHARACTER rather than per rarity.
    /// <para>
    /// That distinction is the whole point of this model. Fragments live on individual characters
    /// (<c>Dictionary&lt;string,int&gt;</c> keyed by <c>packId:entityId</c> in the save), and moving
    /// one sideways costs 5:1 plus a Catalyst. A per-rarity total silently assumes free pooling and
    /// therefore cannot answer the question the sim exists to answer.
    /// </para>
    /// </summary>
    public sealed class PullOutcome
    {
        private readonly EconomyConfig _config;

        public PullOutcome(EconomyConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            Fragments = new int[EconomyConfig.RarityCount][];
            Owned = new bool[EconomyConfig.RarityCount][];
            for (var rarity = EconomyConfig.LowestRarity; rarity <= EconomyConfig.HighestRarity; rarity++)
            {
                var index = EconomyConfig.RarityIndex(rarity);
                Fragments[index] = new int[config.RosterSizeOf(rarity)];
                Owned[index] = new bool[config.RosterSizeOf(rarity)];
            }

            Hits = new int[EconomyConfig.RarityCount];
        }

        /// <summary>Duplicate fragments held, [rarityIndex][characterIndex]. Excludes affinity.</summary>
        public int[][] Fragments { get; private set; }

        public bool[][] Owned { get; private set; }

        /// <summary>Pulls that landed on each rarity, [rarityIndex].</summary>
        public int[] Hits { get; private set; }

        /// <summary>Pulls where Focus was actually in play (pool size &gt; 1 and focus in pool).</summary>
        public int FocusChances { get; private set; }

        /// <summary>Of those, the ones that landed on the focused character.</summary>
        public int FocusHits { get; private set; }

        public int HitsOf(int rarity) => Hits[EconomyConfig.RarityIndex(rarity)];

        public int OwnedCount(int rarity)
        {
            var owned = Owned[EconomyConfig.RarityIndex(rarity)];
            var count = 0;
            for (var i = 0; i < owned.Length; i++)
                if (owned[i]) count++;
            return count;
        }

        public int TotalOwned()
        {
            var total = 0;
            for (var rarity = EconomyConfig.LowestRarity; rarity <= EconomyConfig.HighestRarity; rarity++)
                total += OwnedCount(rarity);
            return total;
        }

        internal void RecordPull(int rarity, int characterIndex, bool focusWasInPlay, bool landedOnFocus)
        {
            var index = EconomyConfig.RarityIndex(rarity);
            Hits[index]++;

            if (focusWasInPlay)
            {
                FocusChances++;
                if (landedOnFocus) FocusHits++;
            }

            if (Owned[index][characterIndex])
                Fragments[index][characterIndex] += _config.DuplicateFragments;
            else
                Owned[index][characterIndex] = true;
        }

        /// <summary>
        /// Independent copy, so a run can be sampled at several Shard budgets without the later
        /// pulls mutating an earlier snapshot.
        /// </summary>
        public PullOutcome Snapshot()
        {
            var copy = new PullOutcome(_config)
            {
                Hits = (int[])Hits.Clone(),
                FocusChances = FocusChances,
                FocusHits = FocusHits
            };

            for (var i = 0; i < EconomyConfig.RarityCount; i++)
            {
                copy.Fragments[i] = (int[])Fragments[i].Clone();
                copy.Owned[i] = (bool[])Owned[i].Clone();
            }

            return copy;
        }
    }
}
