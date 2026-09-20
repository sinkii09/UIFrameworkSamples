using System;

namespace AstralChorus.Logic
{
    /// <summary>
    /// The two pity counters and the rule that reads them. Split out of <see cref="PullEngine"/>
    /// because this is the piece whose edge cases decide whether the published rates hold, and it
    /// is testable without any notion of characters or fragments.
    /// </summary>
    public sealed class PityState
    {
        private readonly EconomyConfig _config;

        public PityState(EconomyConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>Pulls since the last ★4-or-better, counting the last one.</summary>
        public int PullsSinceFourPlus { get; private set; }

        /// <summary>Pulls since the last ★5, counting the last one.</summary>
        public int PullsSinceFive { get; private set; }

        /// <summary>
        /// Advances one pull and returns the rarity rolled.
        /// <para>
        /// Order matters. Hard pity is checked first, so a pull that satisfies both counters pays out
        /// ★5 — the ★5 also resets the ★4 counter, which is why the observed ★4 rate runs slightly
        /// ABOVE the naive renewal estimate while ★5 runs below it.
        /// </para>
        /// <para>
        /// Soft pity guarantees ★4-or-better. GDD §9 adds a qualifier — "<i>khi roster ★4 chưa đủ</i>,
        /// this pull pays exactly ★4" — and states its reason: without it the ★4 floor drops to zero
        /// and the mechanical ownership guarantee in §3 fails. Once every ★4 is owned that reason is
        /// spent, so <see cref="EconomyConfig.SoftPityLapsesWhenFourStarRosterComplete"/> decides
        /// whether the qualifier lapses there. It is not cosmetic: it moves the ★5 hit count, which
        /// §10–§11 are built on.
        /// </para>
        /// </summary>
        /// <param name="fourStarRosterComplete">
        /// Whether every ★4 is already owned. Only consulted on a soft-pity pull.
        /// </param>
        public int NextRarity(DeterministicRandom rng, bool fourStarRosterComplete = false)
        {
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            PullsSinceFourPlus++;
            PullsSinceFive++;

            int rarity;
            if (PullsSinceFive >= _config.HardPityPulls)
            {
                rarity = 5;
            }
            else if (PullsSinceFourPlus >= _config.SoftPityPulls)
            {
                rarity = ResolveSoftPity(rng, fourStarRosterComplete);
            }
            else
            {
                var roll = rng.NextDouble();
                if (roll < _config.FiveStarRate) rarity = 5;
                else if (roll < _config.FiveStarRate + _config.FourStarRate) rarity = 4;
                else rarity = 3;
            }

            if (rarity >= 4) PullsSinceFourPlus = 0;
            if (rarity == 5) PullsSinceFive = 0;
            return rarity;
        }

        /// <summary>
        /// The guaranteed ★4+ pull. While any ★4 is still missing it pays exactly ★4. Afterwards it
        /// pays ★5 at the two rates' RELATIVE odds — 0.02 / (0.02 + 0.18) = 10% — because the pull is
        /// already known to be ★4-or-better, so the ★3 mass is redistributed, not simply dropped.
        /// </summary>
        private int ResolveSoftPity(DeterministicRandom rng, bool fourStarRosterComplete)
        {
            if (!fourStarRosterComplete || !_config.SoftPityLapsesWhenFourStarRosterComplete)
                return 4;

            var fourPlusRate = _config.FiveStarRate + _config.FourStarRate;
            if (fourPlusRate <= 0.0) return 4;

            return rng.NextDouble() < _config.FiveStarRate / fourPlusRate ? 5 : 4;
        }
    }
}
