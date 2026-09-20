using System;

namespace AstralChorus.Logic
{
    /// <summary>
    /// Spends a player's fragments bottom-up through the Resonance rule (GDD §11).
    /// <para>
    /// <b>This policy maximises the COUNT of maxed characters.</b> A real player maximises
    /// preference — they want a specific character finished, not the cheapest one. The two answers
    /// differ, and the difference is exactly what the same-rarity conversion exists to serve. Do not
    /// read this class as a statement about what players will do; it is a budget ceiling, not a
    /// behaviour model.
    /// </para>
    /// </summary>
    public static class ResonanceCascade
    {
        public static CascadeResult Run(EconomyConfig config, PullOutcome outcome)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (outcome == null) throw new ArgumentNullException(nameof(outcome));

            var maxed = new int[EconomyConfig.RarityCount];
            var leftover = new int[EconomyConfig.RarityCount];
            var sameConversions = 0;
            var crossConversions = 0;

            // Fragments handed up from the rarity below. They are TARGETED, so they land 1:1 on
            // whichever character needs them — unlike a sideways move, which costs 5:1.
            var inflow = 0;

            for (var rarity = EconomyConfig.LowestRarity; rarity <= EconomyConfig.HighestRarity; rarity++)
            {
                var index = EconomyConfig.RarityIndex(rarity);
                var size = config.RosterSizeOf(rarity);
                var need = config.FragmentsToMaxCharacter;

                var have = new int[size];
                for (var i = 0; i < size; i++)
                {
                    have[i] = outcome.Fragments[index][i]
                              + (outcome.Owned[index][i] ? config.AffinityFragmentsPerCharacter : 0);
                }

                // 1. Targeted inflow fills gaps 1:1, smallest gap first — that ordering is what
                //    maximises how many characters cross the line with a fixed amount of inflow.
                var free = inflow;
                while (free > 0)
                {
                    var target = SmallestDeficitIndex(have, need);
                    if (target < 0) break;

                    var take = Math.Min(need - have[target], free);
                    have[target] += take;
                    free -= take;
                }

                // 2. Whatever is left over inside this rarity can still repair a gap, but only at the
                //    sideways rate, and each repair burns a Catalyst.
                var surplus = free;
                for (var i = 0; i < size; i++)
                    if (have[i] > need) surplus += have[i] - need;

                while (surplus >= config.SameRarityConversionCost)
                {
                    var target = SmallestDeficitIndex(have, need);
                    if (target < 0) break;

                    // Only start a repair that can be FINISHED. Partial repairs burn Catalyst, max
                    // nobody, and still land in CatalystSpent — which is the figure the grind hours
                    // are derived from. The smallest deficit is the cheapest one available, so if the
                    // surplus cannot cover it, it cannot cover any of them either.
                    var deficit = need - have[target];
                    if (surplus < (long)deficit * config.SameRarityConversionCost) break;

                    have[target] += 1;
                    surplus -= config.SameRarityConversionCost;
                    sameConversions++;
                }

                for (var i = 0; i < size; i++)
                    if (have[i] >= need) maxed[index]++;

                // 3. Push upward only once this rarity is finished. Spending a rarity's surplus on
                //    the rarity above while its own characters are still short would be strictly
                //    worse — the fragments cost more up there and buy nothing down here.
                if (maxed[index] == size && rarity < EconomyConfig.HighestRarity)
                {
                    var promoted = surplus / config.CrossRarityConversionCost;
                    crossConversions += promoted;
                    surplus -= promoted * config.CrossRarityConversionCost;
                    inflow = promoted;
                }
                else
                {
                    inflow = 0;
                }

                leftover[index] = surplus;
            }

            return new CascadeResult(maxed, leftover, sameConversions, crossConversions);
        }

        /// <summary>Index of the character closest to being maxed but not there, or -1 if none.</summary>
        private static int SmallestDeficitIndex(int[] have, int need)
        {
            var best = -1;
            var bestDeficit = int.MaxValue;
            for (var i = 0; i < have.Length; i++)
            {
                if (have[i] >= need) continue;
                var deficit = need - have[i];
                if (deficit >= bestDeficit) continue;
                bestDeficit = deficit;
                best = i;
            }

            return best;
        }
    }
}
