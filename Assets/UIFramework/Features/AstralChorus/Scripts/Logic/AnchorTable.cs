using System;
using System.Collections.Generic;

namespace AstralChorus.Logic
{
    /// <summary>One published anchor-table row. Carries the unrounded value AND what gets printed.</summary>
    public sealed class AnchorRow
    {
        public AnchorRow(int rarity, double duplicateFragments, int affinityFragments,
                         int inflow, double total, int needToMax, int convertedUp,
                         double leftover, int conversionCost)
        {
            Rarity = rarity;
            DuplicateFragments = duplicateFragments;
            AffinityFragments = affinityFragments;
            Inflow = inflow;
            Total = total;
            NeedToMax = needToMax;
            ConvertedUp = convertedUp;
            Leftover = leftover;
            ConversionCost = conversionCost;
        }

        public int Rarity { get; }
        public double DuplicateFragments { get; }
        public int AffinityFragments { get; }
        public int Inflow { get; }
        public double Total { get; }
        public int NeedToMax { get; }
        public int ConvertedUp { get; }

        /// <summary>Unrounded remainder. <see cref="DisplayLeftover"/> is what the GDD prints.</summary>
        public double Leftover { get; }

        /// <summary>Fragments burned per fragment promoted to the next rarity.</summary>
        public int ConversionCost { get; }

        public int DisplayDuplicates => Round(DuplicateFragments);
        public int DisplayTotal => Round(Total);

        // The printed row is derived from the PRINTED total, not from the unrounded one, so it always
        // adds up: total - need - 10*converted = leftover, exactly, as shown. Rounding total and the
        // remainder independently lets them disagree by one whenever rounding crosses zero — a
        // published row that does not reconcile is worse than one that is half a fragment off.
        public int DisplayLeftover => Math.Max(0, DisplayTotal - NeedToMax - ConvertedUp * ConversionCost);

        /// <summary>Shortfall against a full max-out, or 0 if there is none.</summary>
        public int DisplayShortfall => Math.Max(0, NeedToMax - DisplayTotal);

        private static int Round(double value) =>
            (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Builds the two anchor tables published in GDD §11 from MEAN hit counts.
    /// <para>
    /// <b>This is a different computation from <see cref="EconomySimulator"/>, not a summary of it.</b>
    /// One mean player run through one cascade is not the same object as the median of many players
    /// run through many cascades; the two agree near the centre by arithmetic luck, not by
    /// construction. They are built and asserted separately so a future edit cannot quietly swap one
    /// for the other.
    /// </para>
    /// </summary>
    public static class AnchorTable
    {
        /// <param name="meanHitsPerRarity">Mean pulls landing on each rarity, [rarityIndex].</param>
        public static IReadOnlyList<AnchorRow> Build(EconomyConfig config, double[] meanHitsPerRarity)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (meanHitsPerRarity == null) throw new ArgumentNullException(nameof(meanHitsPerRarity));
            if (meanHitsPerRarity.Length != EconomyConfig.RarityCount)
                throw new ArgumentException(
                    "Expected one mean per rarity.", nameof(meanHitsPerRarity));

            var rows = new List<AnchorRow>(EconomyConfig.RarityCount);
            var inflow = 0;

            for (var rarity = EconomyConfig.LowestRarity; rarity <= EconomyConfig.HighestRarity; rarity++)
            {
                var index = EconomyConfig.RarityIndex(rarity);
                var roster = config.RosterSizeOf(rarity);

                // Every pull past the first `roster` of a rarity is a duplicate, because
                // new-character-first guarantees the first hits are all distinct characters.
                var duplicates = (meanHitsPerRarity[index] - roster) * config.DuplicateFragments;
                var affinity = config.AffinityFragmentsFor(rarity);
                var total = duplicates + affinity + inflow;
                var need = config.FragmentsToMaxRarity(rarity);
                var surplus = total - need;

                var converted = 0;
                if (surplus > 0 && rarity < EconomyConfig.HighestRarity)
                    converted = (int)Math.Floor(surplus / config.CrossRarityConversionCost);

                var leftover = surplus - (double)converted * config.CrossRarityConversionCost;

                rows.Add(new AnchorRow(rarity, duplicates, affinity, inflow, total, need,
                                       converted, leftover, config.CrossRarityConversionCost));
                inflow = converted;
            }

            return rows;
        }

        /// <summary>Catalyst implied by the table: one per conversion performed.</summary>
        public static int CatalystSpent(IReadOnlyList<AnchorRow> rows)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));

            var total = 0;
            for (var i = 0; i < rows.Count; i++) total += rows[i].ConvertedUp;
            return total;
        }
    }
}
