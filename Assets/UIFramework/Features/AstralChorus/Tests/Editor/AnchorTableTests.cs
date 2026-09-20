using AstralChorus.Logic;
using NUnit.Framework;

namespace AstralChorus.Tests
{
    /// <summary>
    /// The two anchor tables published in GDD §11.
    /// <para>
    /// These assert the MEAN-then-cascade computation. It is a different object from the per-player
    /// distribution in <see cref="EconomyDistributionTests"/>: one average player run through one
    /// cascade is not the median of many players run through many cascades. They land close together
    /// near the centre, but that is arithmetic luck, and asserting either against the other would
    /// quietly pass until the day it did not.
    /// </para>
    /// </summary>
    [TestFixture]
    public sealed class AnchorTableTests
    {
        // Mean hits under the shipping rules, measured by EconomyDistributionTests.
        private static readonly double[] FloorMeanHits = { 138.34, 35.35, 6.31 };
        private static readonly double[] CeilingMeanHits = { 322.34, 82.40, 15.26 };

        [Test]
        public void FloorTable_MatchesThePublishedRows()
        {
            var rows = AnchorTable.Build(new EconomyConfig(), FloorMeanHits);

            AssertRow(rows[0], rarity: 3, duplicates: 265, affinity: 12, inflow: 0,
                total: 277, need: 60, converted: 21, leftover: 7);
            AssertRow(rows[1], rarity: 4, duplicates: 61, affinity: 10, inflow: 21,
                total: 92, need: 50, converted: 4, leftover: 2);
            AssertRow(rows[2], rarity: 5, duplicates: 7, affinity: 6, inflow: 4,
                total: 17, need: 30, converted: 0, leftover: 0);

            Assert.That(rows[2].DisplayShortfall, Is.EqualTo(13),
                "★5 is 13 fragments short at the floor — this is the gap the Ascent exists to close.");
            Assert.That(AnchorTable.CatalystSpent(rows), Is.EqualTo(25));
        }

        [Test]
        public void CeilingTable_MatchesThePublishedRows()
        {
            var rows = AnchorTable.Build(new EconomyConfig(), CeilingMeanHits);

            AssertRow(rows[0], rarity: 3, duplicates: 633, affinity: 12, inflow: 0,
                total: 645, need: 60, converted: 58, leftover: 5);
            AssertRow(rows[1], rarity: 4, duplicates: 155, affinity: 10, inflow: 58,
                total: 223, need: 50, converted: 17, leftover: 3);
            AssertRow(rows[2], rarity: 5, duplicates: 25, affinity: 6, inflow: 17,
                total: 48, need: 30, converted: 0, leftover: 18);

            Assert.That(rows[2].DisplayShortfall, Is.Zero, "Nothing should be short at the ceiling.");
            Assert.That(AnchorTable.CatalystSpent(rows), Is.EqualTo(75));
        }

        [Test]
        public void CeilingLeftover_IsWorthFarLessThanItsFaceValue()
        {
            var config = new EconomyConfig();
            var rows = AnchorTable.Build(config, CeilingMeanHits);
            var leftover = rows[2].DisplayLeftover;

            // Those fragments sit on characters that are already maxed. Moving one to a character
            // that is not costs 5:1, so as a cushion against variance the 18 is worth about 3. The
            // GDD says so in prose; this is the line that keeps the prose honest.
            Assert.That(leftover, Is.EqualTo(18));
            Assert.That(leftover / config.SameRarityConversionCost, Is.EqualTo(3));
        }

        [Test]
        public void EveryPublishedRowReconcilesAgainstItsOwnPrintedNumbers()
        {
            // A reader audits the row with the PRINTED values, not the unrounded ones behind them.
            // Rounding the total and the remainder independently lets the two disagree by one wherever
            // rounding crosses zero, which prints a row that does not add up. Deriving leftover and
            // shortfall FROM the printed total makes that impossible rather than merely unlikely.
            var config = new EconomyConfig();
            var meanSets = new[]
            {
                FloorMeanHits, CeilingMeanHits,
                new[] { 138.22, 35.70, 6.25 },   // ★5 total lands on .5 — the crossing case
                new[] { 100.00, 20.00, 3.50 },
                new[] { 400.00, 95.00, 18.75 }
            };

            foreach (var means in meanSets)
            {
                foreach (var row in AnchorTable.Build(config, means))
                {
                    var reconciled = row.DisplayTotal - row.NeedToMax
                                     - row.ConvertedUp * row.ConversionCost;

                    Assert.That(row.DisplayLeftover - row.DisplayShortfall, Is.EqualTo(reconciled),
                        "Rarity " + row.Rarity + " row does not reconcile: total " + row.DisplayTotal +
                        " - need " + row.NeedToMax + " - " + row.ConvertedUp + "x" + row.ConversionCost);
                    Assert.That(row.DisplayLeftover, Is.GreaterThanOrEqualTo(0));
                    Assert.That(row.DisplayShortfall, Is.GreaterThanOrEqualTo(0));
                }
            }
        }

        private static void AssertRow(AnchorRow row, int rarity, int duplicates, int affinity,
                                      int inflow, int total, int need, int converted, int leftover)
        {
            Assert.That(row.Rarity, Is.EqualTo(rarity), "rarity");
            Assert.That(row.DisplayDuplicates, Is.EqualTo(duplicates), "duplicate fragments");
            Assert.That(row.AffinityFragments, Is.EqualTo(affinity), "affinity");
            Assert.That(row.Inflow, Is.EqualTo(inflow), "inflow from the rarity below");
            Assert.That(row.DisplayTotal, Is.EqualTo(total), "total");
            Assert.That(row.NeedToMax, Is.EqualTo(need), "need to max");
            Assert.That(row.ConvertedUp, Is.EqualTo(converted), "converted up");

            // Asserted unconditionally. The previous version skipped this whenever the row was short,
            // which exempted the single row where leftover was nonsense — the one case worth checking.
            Assert.That(row.DisplayLeftover, Is.EqualTo(leftover), "leftover");
        }
    }
}
