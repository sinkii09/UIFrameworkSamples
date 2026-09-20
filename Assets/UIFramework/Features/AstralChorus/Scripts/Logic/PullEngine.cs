using System;
using System.Collections.Generic;

namespace AstralChorus.Logic
{
    /// <summary>
    /// Runs a player's pulls. One Shard is one pull, so the Shard budget and the pull count are the
    /// same number (GDD §9).
    /// </summary>
    public sealed class PullEngine
    {
        private readonly EconomyConfig _config;

        public PullEngine(EconomyConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <param name="focusRarity">Rarity the player has focused, or -1 for none.</param>
        /// <param name="focusIndex">Character index within that rarity.</param>
        public PullOutcome Run(int pulls, DeterministicRandom rng, int focusRarity = -1, int focusIndex = 0)
        {
            var snapshots = RunWithCheckpoints(new[] { pulls }, rng, focusRarity, focusIndex);
            return snapshots[pulls];
        }

        /// <summary>
        /// Runs to the largest checkpoint once and snapshots along the way, so comparing budgets does
        /// not mean re-simulating a player from zero for each one.
        /// </summary>
        public IReadOnlyDictionary<int, PullOutcome> RunWithCheckpoints(
            IReadOnlyList<int> checkpoints,
            DeterministicRandom rng,
            int focusRarity = -1,
            int focusIndex = 0)
        {
            if (checkpoints == null) throw new ArgumentNullException(nameof(checkpoints));
            if (checkpoints.Count == 0)
                throw new ArgumentException("At least one checkpoint is required.", nameof(checkpoints));
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            ValidateFocus(focusRarity, focusIndex);

            var wanted = new HashSet<int>(checkpoints);
            var last = 0;
            for (var i = 0; i < checkpoints.Count; i++)
            {
                if (checkpoints[i] < 1)
                    throw new ArgumentException("Checkpoints must be positive.", nameof(checkpoints));
                if (checkpoints[i] > last) last = checkpoints[i];
            }

            var pity = new PityState(_config);
            var outcome = new PullOutcome(_config);
            var snapshots = new Dictionary<int, PullOutcome>(wanted.Count);

            for (var pull = 1; pull <= last; pull++)
            {
                // Read BEFORE the roll: soft pity's "exactly ★4" qualifier is conditioned on the ★4
                // roster still being incomplete at the moment the pull is made (GDD §9).
                var fourStarComplete =
                    outcome.OwnedCount(4) == _config.RosterSizeOf(4);

                var rarity = pity.NextRarity(rng, fourStarComplete);
                var rarityIndex = EconomyConfig.RarityIndex(rarity);

                var focusForThisRarity = rarity == focusRarity ? focusIndex : -1;
                var picked = FocusWeighting.Pick(
                    _config, rarity, outcome.Owned[rarityIndex], focusForThisRarity, rng,
                    out var focusContested);

                outcome.RecordPull(rarity, picked, focusContested, picked == focusForThisRarity);

                if (wanted.Contains(pull)) snapshots[pull] = outcome.Snapshot();
            }

            return snapshots;
        }

        /// <summary>
        /// Rejects a focus that names a character outside its rarity's roster. Without this the run
        /// survives until the first pull of that rarity and then throws from deep inside the pick,
        /// which is a long way from the caller that got it wrong.
        /// </summary>
        private void ValidateFocus(int focusRarity, int focusIndex)
        {
            if (focusRarity < 0) return;

            if (focusRarity < EconomyConfig.LowestRarity || focusRarity > EconomyConfig.HighestRarity)
                throw new ArgumentOutOfRangeException(
                    nameof(focusRarity), focusRarity, "Focus rarity must be 3, 4, 5, or -1 for none.");

            var size = _config.RosterSizeOf(focusRarity);
            if (focusIndex < 0 || focusIndex >= size)
                throw new ArgumentOutOfRangeException(
                    nameof(focusIndex), focusIndex,
                    "The ★" + focusRarity + " roster holds " + size + " characters.");
        }
    }
}
