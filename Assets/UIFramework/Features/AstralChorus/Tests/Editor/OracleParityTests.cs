using System.Collections.Generic;
using AstralChorus.Logic;
using NUnit.Framework;

namespace AstralChorus.Tests
{
    /// <summary>
    /// Proves the C# port is faithful to the frozen Python oracle at
    /// <c>tools/astral-chorus-economy-sim/oracle/</c>.
    /// <para>
    /// The oracle applies soft pity's "exactly ★4" unconditionally. The shipping default follows
    /// GDD §9 and lets that qualifier lapse once every ★4 is owned, which is a different model and
    /// produces different numbers — so parity is checked here with the flag turned back to the
    /// oracle's reading. Without this separation the port would have no reference at all: the
    /// distribution fixture would be asserting the port against itself.
    /// </para>
    /// <para>
    /// The two implementations use different generators (PCG here, Mersenne Twister there), so
    /// sequences cannot match and only distributions can. The oracle is never edited to agree with
    /// the port.
    /// </para>
    /// </summary>
    [TestFixture]
    public sealed class OracleParityTests
    {
        private const int Players = 30_000;
        private const int Seed = 20260920;

        private EconomyConfig _config;
        private IReadOnlyDictionary<int, SimulationReport> _reports;

        [OneTimeSetUp]
        public void RunSimulation()
        {
            _config = new EconomyConfig { SoftPityLapsesWhenFourStarRosterComplete = false };
            _reports = new EconomySimulator(_config)
                .Run(new[] { _config.CampaignShard, _config.TotalShardBudget }, Players, Seed);
        }

        private SimulationReport Floor => _reports[_config.CampaignShard];
        private SimulationReport Ceiling => _reports[_config.TotalShardBudget];

        [Test]
        public void HitRates_ReproduceTheOracle()
        {
            Assert.That(Floor.MeanHits(5), Is.EqualTo(6.08).Within(0.08));
            Assert.That(Floor.MeanHits(4), Is.EqualTo(35.70).Within(0.20));
            Assert.That(Floor.MeanHits(3), Is.EqualTo(138.22).Within(0.25));
            Assert.That(Ceiling.MeanHits(5), Is.EqualTo(14.68).Within(0.15));
            Assert.That(Ceiling.MeanHits(4), Is.EqualTo(83.30).Within(0.35));
            Assert.That(Ceiling.MeanHits(3), Is.EqualTo(322.01).Within(0.50));
        }

        [Test]
        public void OutcomeShares_ReproduceTheOracle()
        {
            Assert.That(Floor.OwnFullRosterShare, Is.EqualTo(1.0));
            Assert.That(Floor.FullyMaxedShare(3, _config.RosterSizeOf(3)), Is.EqualTo(1.0));
            Assert.That(Floor.FullyMaxedShare(4, _config.RosterSizeOf(4)), Is.EqualTo(1.0));
            Assert.That(Floor.ZeroMaxedShare(5), Is.EqualTo(0.4937).Within(0.015));
            Assert.That(Ceiling.AllMaxedShare, Is.EqualTo(1.0));
        }

        [Test]
        public void CatalystDemand_ReproducesTheOracle()
        {
            Assert.That(Floor.CatalystPercentile(0.50), Is.EqualTo(25));
            Assert.That(Floor.CatalystPercentile(0.90), Is.EqualTo(26));
            Assert.That(Ceiling.CatalystPercentile(0.50), Is.EqualTo(75));
            Assert.That(Ceiling.CatalystPercentile(0.90), Is.EqualTo(76));
        }

        [Test]
        public void TheTwoReadingsDisagreeEnoughToMatter()
        {
            // Guards against the flag quietly becoming a no-op. If the two readings ever converge,
            // either the soft-pity branch stopped working or the rates changed underneath it — and
            // every published ★5 figure would be resting on an assumption that no longer holds.
            var shipping = new EconomySimulator(new EconomyConfig())
                .Run(new[] { _config.CampaignShard }, 8_000, Seed)[_config.CampaignShard];

            var oracleFiveStars = Floor.MeanHits(5);
            var shippingFiveStars = shipping.MeanHits(5);

            Assert.That(shippingFiveStars, Is.GreaterThan(oracleFiveStars + 0.10),
                "Letting soft pity lapse must ADD ★5 hits — it converts a forced ★4 into a 10% ★5 roll.");
        }
    }
}
