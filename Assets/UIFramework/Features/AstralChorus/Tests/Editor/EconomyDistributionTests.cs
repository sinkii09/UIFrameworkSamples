using System.Collections.Generic;
using AstralChorus.Logic;
using NUnit.Framework;

namespace AstralChorus.Tests
{
    /// <summary>
    /// Per-player distribution results under the SHIPPING rules.
    /// <para>
    /// These numbers differ from the frozen Python oracle on purpose: the oracle applies soft pity's
    /// "exactly ★4" unconditionally, while GDD §9 conditions it on the ★4 roster still being
    /// incomplete. <see cref="OracleParityTests"/> holds the oracle's reading and proves the port is
    /// faithful to it; this fixture holds what the document actually specifies.
    /// </para>
    /// <para>
    /// Bands are sized from the sampling error at <see cref="Players"/>, not from whatever happened
    /// to pass; saturated results are asserted exactly, because a band around 100% would only measure
    /// how much failure goes unnoticed.
    /// </para>
    /// </summary>
    [TestFixture]
    public sealed class EconomyDistributionTests
    {
        private const int Players = 30_000;
        private const int Seed = 20260920;

        private EconomyConfig _config;
        private IReadOnlyDictionary<int, SimulationReport> _reports;

        [OneTimeSetUp]
        public void RunSimulation()
        {
            _config = new EconomyConfig();
            _reports = new EconomySimulator(_config)
                .Run(new[] { _config.CampaignShard, _config.TotalShardBudget }, Players, Seed);
        }

        private SimulationReport Floor => _reports[_config.CampaignShard];
        private SimulationReport Ceiling => _reports[_config.TotalShardBudget];

        [Test]
        public void OwningEveryCharacter_IsMechanicallyGuaranteedByTheCampaignAlone()
        {
            // GDD §3 states this as mechanical, not statistical: 180 pulls forces at least four ★5
            // through hard pity and eighteen ★4+ through soft pity, and new-character-first makes
            // every one of those hits a distinct character. Exact, therefore.
            Assert.That(Floor.OwnFullRosterShare, Is.EqualTo(1.0));
        }

        [Test]
        public void Floor_MaxesEveryThreeAndFourStarForEveryPlayer()
        {
            Assert.That(Floor.FullyMaxedShare(3, _config.RosterSizeOf(3)), Is.EqualTo(1.0));
            Assert.That(Floor.FullyMaxedShare(4, _config.RosterSizeOf(4)), Is.EqualTo(1.0));
            Assert.That(Floor.MaxedPercentile(3, 0.01), Is.EqualTo(6));
            Assert.That(Floor.MaxedPercentile(4, 0.01), Is.EqualTo(5));
        }

        [Test]
        public void Floor_LeavesNearlyHalfOfCampaignOnlyPlayersWithoutAnyMaxedFiveStar()
        {
            // GDD risk 13. An earlier draft said ~10%, read off a percentile of the TOTAL maxed count
            // rather than of ★5 specifically. It is not a rounding slip — it is nearly half the
            // player base, and it is the strongest single argument for how the Ascent is paced.
            Assert.That(Floor.ZeroMaxedShare(5), Is.EqualTo(0.466).Within(0.015));
            Assert.That(Floor.MaxedPercentile(5, 0.50), Is.EqualTo(1));
            Assert.That(Floor.AllMaxedShare, Is.LessThan(0.001));
        }

        [Test]
        public void Ceiling_MaxesTheEntireRosterForEveryPlayer()
        {
            Assert.That(Ceiling.AllMaxedShare, Is.EqualTo(1.0));
            Assert.That(Ceiling.MaxedPercentile(5, 0.01), Is.EqualTo(3),
                "Even the unluckiest 1% must finish all three ★5.");
        }

        [Test]
        public void HitRates_DoNotMatchTheNaiveRenewalFormula()
        {
            Assert.That(Floor.MeanHits(5), Is.EqualTo(6.31).Within(0.08));
            Assert.That(Floor.MeanHits(4), Is.EqualTo(35.35).Within(0.20));
            Assert.That(Floor.MeanHits(3), Is.EqualTo(138.34).Within(0.25));
            Assert.That(Ceiling.MeanHits(5), Is.EqualTo(15.26).Within(0.15));
            Assert.That(Ceiling.MeanHits(4), Is.EqualTo(82.40).Within(0.35));
            Assert.That(Ceiling.MeanHits(3), Is.EqualTo(322.34).Within(0.50));

            // budget / E[cycle] gives 180/27.715 = 6.50 for ★5. That is a renewal-theory asymptote,
            // not a finite-horizon expectation, and the GDD quoted it for three drafts. The ★4 count
            // errs the other way because a hard-pity ★5 also resets the soft-pity counter, so the two
            // mistakes cancelled in the total and nothing looked wrong.
            Assert.That(Floor.MeanHits(5), Is.LessThan(6.45),
                "If this ever reaches 6.5 the model has drifted back to the formula.");
        }

        [Test]
        public void CatalystDemand_IsUnchangedByTheSoftPityReading()
        {
            // Worth its own name: the soft-pity ruling moves the ★5 hit count by ~4% and moves these
            // not at all, because Catalyst demand is set by the cross-rarity cascade, which is fed by
            // ★3 overflow — a stream soft pity never touches.
            Assert.That(Floor.CatalystPercentile(0.50), Is.EqualTo(25));
            Assert.That(Floor.CatalystPercentile(0.90), Is.EqualTo(26));
            Assert.That(Ceiling.CatalystPercentile(0.50), Is.EqualTo(75));
            Assert.That(Ceiling.CatalystPercentile(0.90), Is.EqualTo(76));
        }

        [Test]
        public void SameRarityConversion_EssentiallyNeverFires()
        {
            // The rule's published justification was that it erases within-rarity variance. It does
            // not: targeted inflow from the rarity below fills every gap 1:1 before variance becomes
            // a problem. The rule is kept because it lets a player redirect fragments to the
            // character they want — a different job, and one this count cannot measure.
            Assert.That(Floor.SameRarityConversionShare, Is.LessThan(0.001));
            Assert.That(Ceiling.SameRarityConversionShare, Is.LessThan(0.001));
        }

        [Test]
        public void Focus_SteersSixtyPercentOfContestedPullsOnAThreeCharacterRarity()
        {
            var reports = new EconomySimulator(_config).Run(
                new[] { _config.TotalShardBudget }, 8_000, 4242, EconomyConfig.HighestRarity, 0);

            // 3/(3+1+1) = 0.60 exactly. "Contested" counts only pulls taken once every ★5 is owned;
            // pulls taken while one is still missing run against a two-character pool and pay 3/4, so
            // folding them in would bias this upward and disguise a mixture as sampling noise.
            Assert.That(reports[_config.TotalShardBudget].FocusHitRate,
                Is.EqualTo(0.60).Within(0.015));
        }

        [Test]
        public void RejectedModel_FlatFiveToOneRateGutsTheAscent()
        {
            // A regression on a DESIGN decision, not on code. At a flat 5:1 everyone maxes everything
            // at 260 Shard while the campaign alone gifts 180 — leaving the whole tower economically
            // worth 80 Shard. If a future edit re-widens the cross-rarity pipe, this fails and says why.
            //
            // 8,000 players is enough here and only here: the neighbouring candidate (220) fails for
            // ~11% of players, a percent-scale gap this N resolves easily. Do not copy this call site
            // to pick a shipping constant — see the remarks on SmallestBudgetMaxingEveryone.
            var flat = _config.Clone();
            flat.CrossRarityConversionCost = flat.SameRarityConversionCost;

            var budget = new EconomySimulator(flat)
                .SmallestBudgetMaxingEveryone(new[] { 180, 220, 260, 300 }, 8_000, Seed);

            Assert.That(budget, Is.EqualTo(260));
            Assert.That(budget - flat.CampaignShard, Is.EqualTo(80));
        }
    }
}
