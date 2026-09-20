using AstralChorus.Logic;
using NUnit.Framework;

namespace AstralChorus.Tests
{
    /// <summary>
    /// Structural invariants — no tolerance, no dependence on how a particular seed fell.
    /// <para>
    /// These matter more than the distribution tests. Matching a distribution cannot catch
    /// compensating errors, and this design has already produced one: the ★5 count ran low while the
    /// ★4 count ran high, the two cancelled in the total, and the error survived three review rounds
    /// because every aggregate still looked right.
    /// </para>
    /// </summary>
    [TestFixture]
    public sealed class EconomyInvariantTests
    {
        private static EconomyConfig Config() => new EconomyConfig();

        [Test]
        public void PityState_NeverLeavesMoreThanHardPityPullsBetweenFiveStars()
        {
            var config = Config();
            var pity = new PityState(config);
            var rng = new DeterministicRandom(11);

            var gap = 0;
            for (var pull = 0; pull < 200_000; pull++)
            {
                gap++;
                if (pity.NextRarity(rng) == 5)
                {
                    Assert.That(gap, Is.LessThanOrEqualTo(config.HardPityPulls),
                        "Hard pity is the guarantee the ownership floor in GDD §3 rests on.");
                    gap = 0;
                }
            }

            Assert.That(gap, Is.LessThan(config.HardPityPulls));
        }

        [Test]
        public void PityState_NeverLeavesMoreThanSoftPityPullsBetweenFourPlus()
        {
            var config = Config();
            var pity = new PityState(config);
            var rng = new DeterministicRandom(12);

            var gap = 0;
            for (var pull = 0; pull < 200_000; pull++)
            {
                gap++;
                if (pity.NextRarity(rng) >= 4)
                {
                    Assert.That(gap, Is.LessThanOrEqualTo(config.SoftPityPulls));
                    gap = 0;
                }
            }
        }

        [Test]
        public void PityState_SoftPityPullReturnsExactlyFourUnlessHardPityAlsoFired()
        {
            var config = Config();
            var pity = new PityState(config);
            var rng = new DeterministicRandom(13);

            for (var pull = 0; pull < 200_000; pull++)
            {
                var soft = pity.PullsSinceFourPlus + 1 >= config.SoftPityPulls;
                var hard = pity.PullsSinceFive + 1 >= config.HardPityPulls;
                var rarity = pity.NextRarity(rng);

                if (!soft || hard) continue;

                // A soft-pity pull that paid ★5 would drop the ★4 floor to zero, and the ownership
                // guarantee for ★4 in GDD §3 is stated as mechanical, not statistical.
                Assert.That(rarity, Is.EqualTo(4),
                    "Soft pity must pay exactly ★4 when hard pity did not fire.");
            }
        }

        [Test]
        public void NewCharacterFirst_ProducesNoDuplicateWhileAnyCharacterIsUnowned()
        {
            var config = Config();
            var engine = new PullEngine(config);
            var rng = new DeterministicRandom(14);

            for (var player = 0; player < 300; player++)
            {
                var outcome = engine.Run(config.TotalShardBudget, rng);

                for (var rarity = EconomyConfig.LowestRarity; rarity <= EconomyConfig.HighestRarity; rarity++)
                {
                    var index = EconomyConfig.RarityIndex(rarity);
                    var fragments = 0;
                    for (var i = 0; i < outcome.Fragments[index].Length; i++)
                        fragments += outcome.Fragments[index][i];

                    if (fragments > 0)
                        Assert.That(outcome.OwnedCount(rarity),
                            Is.EqualTo(config.RosterSizeOf(rarity)),
                            "A duplicate appeared while that rarity still had unowned characters.");
                }
            }
        }

        [Test]
        public void Focus_NeverOverridesNewCharacterFirst()
        {
            var config = Config();
            var engine = new PullEngine(config);
            var rng = new DeterministicRandom(15);

            // Focus hammers one ★3 for the whole run. If Focus were applied before the pool was
            // narrowed to unowned characters, this is where duplicates of that one character would
            // pile up while its five rarity-mates were still missing.
            for (var player = 0; player < 300; player++)
            {
                var outcome = engine.Run(config.TotalShardBudget, rng, EconomyConfig.LowestRarity, 0);
                var index = EconomyConfig.RarityIndex(EconomyConfig.LowestRarity);

                if (outcome.Fragments[index][0] > 0)
                    Assert.That(outcome.OwnedCount(EconomyConfig.LowestRarity),
                        Is.EqualTo(config.RosterSizeOf(EconomyConfig.LowestRarity)));
            }
        }

        [Test]
        public void Cascade_ChargesOneCatalystPerConversionAndConservesFragments()
        {
            var config = Config();
            var outcome = new PullOutcome(config);

            // Own everything, and hand ★3 a surplus that is an exact multiple of the cross rate so
            // the conversion count is arithmetic rather than a judgement call.
            for (var rarity = EconomyConfig.LowestRarity; rarity <= EconomyConfig.HighestRarity; rarity++)
            {
                var index = EconomyConfig.RarityIndex(rarity);
                for (var i = 0; i < config.RosterSizeOf(rarity); i++)
                {
                    outcome.Owned[index][i] = true;
                    outcome.Fragments[index][i] = config.FragmentsToMaxCharacter
                                                 - config.AffinityFragmentsPerCharacter;
                }
            }

            const int surplusMultiples = 7;
            var threeIndex = EconomyConfig.RarityIndex(EconomyConfig.LowestRarity);
            outcome.Fragments[threeIndex][0] += surplusMultiples * config.CrossRarityConversionCost;

            var result = ResonanceCascade.Run(config, outcome);

            Assert.That(result.CrossRarityConversions, Is.EqualTo(surplusMultiples),
                "Surplus was an exact multiple of the 10:1 rate, so the count is determined.");
            Assert.That(result.SameRarityConversions, Is.Zero,
                "Nothing was short, so no sideways repair should have been needed.");
            Assert.That(result.CatalystSpent,
                Is.EqualTo(result.SameRarityConversions + result.CrossRarityConversions));
            Assert.That(result.LeftoverOf(EconomyConfig.LowestRarity), Is.Zero,
                "An exact multiple must leave no remainder behind.");
        }

        [Test]
        public void Cascade_RepairsItsOwnRarityBeforePushingUpward()
        {
            var config = Config();
            var outcome = new PullOutcome(config);

            // Every ★3 owned; one of them starved, the rest carrying a large surplus. Pushing that
            // surplus up would be strictly worse — it costs more up there and buys nothing here.
            var threeIndex = EconomyConfig.RarityIndex(EconomyConfig.LowestRarity);
            for (var i = 0; i < config.RosterSizeOf(EconomyConfig.LowestRarity); i++)
            {
                outcome.Owned[threeIndex][i] = true;
                outcome.Fragments[threeIndex][i] = i == 0 ? 0 : 100;
            }

            var result = ResonanceCascade.Run(config, outcome);

            Assert.That(result.MaxedOf(EconomyConfig.LowestRarity),
                Is.EqualTo(config.RosterSizeOf(EconomyConfig.LowestRarity)),
                "The starved ★3 should have been repaired sideways before anything moved up.");
            Assert.That(result.SameRarityConversions, Is.GreaterThan(0),
                "This is the one situation the same-rarity rule exists for.");
        }
    }
}
