using AstralChorus.Logic;
using NUnit.Framework;

namespace AstralChorus.Tests
{
    /// <summary>
    /// Keeps <see cref="EconomyConfig"/> and the published design document from drifting apart.
    /// <para>
    /// The GDD is hand-written and stays that way — its numbers sit inside prose arguments that
    /// generating them would destroy. The cost of that choice is two copies of every constant, and
    /// this fixture is what makes the cost survivable: change one without the other and the suite
    /// says so, naming the section to go and fix.
    /// </para>
    /// </summary>
    [TestFixture]
    public sealed class ConfigDriftTests
    {
        private readonly EconomyConfig _config = new EconomyConfig();

        [Test]
        public void RosterMatchesGddSection4()
        {
            Assert.That(_config.RosterSizeOf(3), Is.EqualTo(6));
            Assert.That(_config.RosterSizeOf(4), Is.EqualTo(5));
            Assert.That(_config.RosterSizeOf(5), Is.EqualTo(3));
        }

        [Test]
        public void RatesAndPityMatchGddSection9()
        {
            Assert.That(_config.FiveStarRate, Is.EqualTo(0.02));
            Assert.That(_config.FourStarRate, Is.EqualTo(0.18));
            Assert.That(_config.SoftPityPulls, Is.EqualTo(10));
            Assert.That(_config.HardPityPulls, Is.EqualTo(40));
            Assert.That(_config.FocusWeight, Is.EqualTo(3));
        }

        [Test]
        public void ShardBudgetMatchesGddSection10()
        {
            Assert.That(_config.CampaignShard, Is.EqualTo(180));
            Assert.That(_config.AscentShardPerFloor, Is.EqualTo(6));
            Assert.That(_config.AscentFloors, Is.EqualTo(40));
            Assert.That(_config.AscentShard, Is.EqualTo(240));
            Assert.That(_config.TotalShardBudget, Is.EqualTo(420));
        }

        [Test]
        public void DustEconomyMatchesGddSection10()
        {
            Assert.That(_config.LevelDustTotal, Is.EqualTo(215_000));
            Assert.That(_config.CampaignDust, Is.EqualTo(40_000));
            Assert.That(_config.CatalystPriceDust, Is.EqualTo(1_200));
            Assert.That(_config.DustPerRun, Is.EqualTo(600));
            Assert.That(_config.RunMinutes, Is.EqualTo(3.0));
        }

        [Test]
        public void ProgressionMatchesGddSection11()
        {
            Assert.That(_config.DuplicateFragments, Is.EqualTo(2));
            Assert.That(_config.AffinityFragmentsPerCharacter, Is.EqualTo(2));
            Assert.That(_config.FragmentsToMaxCharacter, Is.EqualTo(10));
            Assert.That(_config.SameRarityConversionCost, Is.EqualTo(5));
            Assert.That(_config.CrossRarityConversionCost, Is.EqualTo(10));

            Assert.That(_config.FragmentsToMaxRarity(3), Is.EqualTo(60));
            Assert.That(_config.FragmentsToMaxRarity(4), Is.EqualTo(50));
            Assert.That(_config.FragmentsToMaxRarity(5), Is.EqualTo(30));

            Assert.That(_config.AffinityFragmentsFor(3), Is.EqualTo(12));
            Assert.That(_config.AffinityFragmentsFor(4), Is.EqualTo(10));
            Assert.That(_config.AffinityFragmentsFor(5), Is.EqualTo(6));
        }

        [Test]
        public void CrossRarityCostIsDearerThanSameRarity()
        {
            // Not a transcription check — a design invariant. If these ever equalise, the ★3 overflow
            // pipe reopens and the Ascent's 240-Shard payout has nothing left to buy.
            Assert.That(_config.CrossRarityConversionCost,
                Is.GreaterThan(_config.SameRarityConversionCost));
        }

        [Test]
        public void CloneDoesNotShareTheRosterArray()
        {
            var clone = _config.Clone();
            clone.RosterSize[0] = 99;

            Assert.That(_config.RosterSizeOf(3), Is.EqualTo(6),
                "A shallow clone would let a variant rewrite the design constants it was cloned from.");
        }
    }
}
