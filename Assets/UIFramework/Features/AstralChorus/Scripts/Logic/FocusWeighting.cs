using System;
using System.Collections.Generic;

namespace AstralChorus.Logic
{
    /// <summary>
    /// Which character a pull of a given rarity lands on.
    /// <para>
    /// Two rules interact here, and their ORDER is the design. New-character-first picks the
    /// candidate pool; Focus only reweights inside whatever pool that leaves. So while a rarity
    /// still has unowned members, Focus can steer WHICH new character arrives but can never make a
    /// duplicate happen — the ownership guarantee in GDD §3 survives the targeting feature in §9
    /// precisely because Focus is applied second.
    /// </para>
    /// </summary>
    public static class FocusWeighting
    {
        /// <summary>
        /// Picks a character index for <paramref name="rarity"/>.
        /// </summary>
        /// <param name="focusIndex">The focused character, or -1 for no Focus on this rarity.</param>
        /// <param name="focusContested">
        /// True only when Focus competed against the FULL roster — every character owned, so the pool
        /// is all of them. That is the regime GDD §9's table describes (3/(3+1+1) = 60% on a
        /// three-character rarity). Pulls taken while characters are still missing run against a
        /// smaller pool and pay a HIGHER rate, so folding them in would quietly bias the measurement
        /// upward and make the published 60% look like sampling noise instead of a mixture.
        /// </param>
        public static int Pick(
            EconomyConfig config,
            int rarity,
            IReadOnlyList<bool> owned,
            int focusIndex,
            DeterministicRandom rng,
            out bool focusContested)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (owned == null) throw new ArgumentNullException(nameof(owned));
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            var size = config.RosterSizeOf(rarity);
            if (focusIndex >= size)
                throw new ArgumentOutOfRangeException(
                    nameof(focusIndex), focusIndex,
                    "Focus index is outside the ★" + rarity + " roster of " + size + ".");

            // New-character-first: the pool is the unowned members, and only falls back to the whole
            // rarity once every one of them is owned.
            var pool = new List<int>(size);
            for (var i = 0; i < size; i++)
                if (!owned[i]) pool.Add(i);

            var everyoneOwned = pool.Count == 0;
            if (everyoneOwned)
            {
                for (var i = 0; i < size; i++) pool.Add(i);
            }

            var focusInPool = focusIndex >= 0 && pool.Contains(focusIndex);
            focusContested = focusInPool && everyoneOwned && size > 1;

            if (pool.Count == 1) return pool[0];
            if (!focusInPool) return pool[rng.Next(pool.Count)];

            var weights = new int[pool.Count];
            for (var i = 0; i < pool.Count; i++)
                weights[i] = pool[i] == focusIndex ? config.FocusWeight : 1;

            return pool[rng.NextWeightedIndex(weights)];
        }
    }
}
