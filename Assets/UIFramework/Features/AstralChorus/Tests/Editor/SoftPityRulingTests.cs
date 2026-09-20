using AstralChorus.Logic;
using NUnit.Framework;

namespace AstralChorus.Tests
{
    /// <summary>
    /// Both readings of GDD §9's soft-pity qualifier, each pinned down.
    /// <para>
    /// §9 says the guaranteed ★4+ pull pays exactly ★4 "<i>khi roster ★4 chưa đủ</i>", and states the
    /// reason: without it the ★4 ownership floor in §3 drops to zero. That reason expires the instant
    /// the last ★4 is owned — which, under new-character-first, happens within the first few ★4 hits.
    /// So the qualifier governs a sliver of a run and the lapse governs the rest.
    /// </para>
    /// <para>
    /// The difference is not cosmetic: it moves mean ★5 hits at 180 pulls from 6.08 to 6.31, and the
    /// share of campaign-only players who max no ★5 at all from 49% to 47%. The shipping default
    /// follows the document; the frozen Python oracle does not, which is why both branches are tested
    /// rather than one being deleted.
    /// </para>
    /// </summary>
    [TestFixture]
    public sealed class SoftPityRulingTests
    {
        [Test]
        public void SoftPityPaysFiveStars_OnceEveryFourStarIsOwned()
        {
            var config = new EconomyConfig();
            var rng = new DeterministicRandom(16);
            var fiveStars = 0;
            var softPityPulls = 0;

            for (var run = 0; run < 40_000; run++)
            {
                var pity = new PityState(config);

                // Walk toward the soft-pity boundary; abandon this run the moment a natural ★4+
                // resets the counter, so only genuine forced pulls are counted.
                for (var i = 0; i < config.SoftPityPulls - 1; i++)
                {
                    if (pity.NextRarity(rng, true) >= 4) break;
                }

                if (pity.PullsSinceFourPlus != config.SoftPityPulls - 1) continue;
                if (pity.PullsSinceFive + 1 >= config.HardPityPulls) continue;

                softPityPulls++;
                if (pity.NextRarity(rng, true) == 5) fiveStars++;
            }

            Assert.That(softPityPulls, Is.GreaterThan(500),
                "Too few samples reached the soft-pity boundary for this measurement to mean anything.");

            // The pull is already known to be ★4-or-better, so the ★3 mass is redistributed across the
            // two survivors rather than dropped: 0.02 / (0.02 + 0.18) = 10%.
            var expected = config.FiveStarRate / (config.FiveStarRate + config.FourStarRate);
            Assert.That((double)fiveStars / softPityPulls, Is.EqualTo(expected).Within(0.03));
        }

        [Test]
        public void SoftPityKeepsPayingExactlyFourStar_WhenTheLapseIsDisabled()
        {
            // The oracle's reading, still reachable and still verified — otherwise OracleParityTests
            // would be comparing the port against a branch that nothing checks.
            var config = new EconomyConfig { SoftPityLapsesWhenFourStarRosterComplete = false };
            var pity = new PityState(config);
            var rng = new DeterministicRandom(17);

            for (var pull = 0; pull < 100_000; pull++)
            {
                var soft = pity.PullsSinceFourPlus + 1 >= config.SoftPityPulls;
                var hard = pity.PullsSinceFive + 1 >= config.HardPityPulls;

                var rarity = pity.NextRarity(rng, true);   // roster complete, but the lapse is off

                if (soft && !hard) Assert.That(rarity, Is.EqualTo(4));
            }
        }

        [Test]
        public void SoftPityIgnoresTheLapse_WhileAnyFourStarIsStillMissing()
        {
            // The floor the qualifier exists to protect. With the roster incomplete, the forced pull
            // must be ★4 regardless of the flag — otherwise GDD §3's mechanical ★4 guarantee is only
            // a statistical one.
            var config = new EconomyConfig();
            var pity = new PityState(config);
            var rng = new DeterministicRandom(19);

            for (var pull = 0; pull < 100_000; pull++)
            {
                var soft = pity.PullsSinceFourPlus + 1 >= config.SoftPityPulls;
                var hard = pity.PullsSinceFive + 1 >= config.HardPityPulls;

                var rarity = pity.NextRarity(rng, false);  // roster incomplete

                if (soft && !hard) Assert.That(rarity, Is.EqualTo(4));
            }
        }
    }
}
