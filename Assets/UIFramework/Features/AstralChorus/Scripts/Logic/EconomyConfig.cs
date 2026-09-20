using System;

namespace AstralChorus.Logic
{
    /// <summary>
    /// Every tunable number the economy depends on, in one place.
    /// <para>
    /// The defaults are the values published in GDD §9–§11. They are duplicated here rather than
    /// generated from the document because the document's numbers sit inside prose arguments that
    /// codegen would destroy — so <c>ConfigDriftTests</c> exists to fail the moment the two disagree.
    /// </para>
    /// <para>
    /// Rarity-indexed arrays are indexed by <see cref="RarityIndex"/>, i.e. ★3→0, ★4→1, ★5→2.
    /// </para>
    /// </summary>
    public sealed class EconomyConfig
    {
        public const int LowestRarity = 3;
        public const int HighestRarity = 5;
        public const int RarityCount = 3;

        // ---- roster (GDD §4) ----
        /// <summary>Characters per rarity: 6 ★3, 5 ★4, 3 ★5.</summary>
        public int[] RosterSize { get; set; } = { 6, 5, 3 };

        // ---- pull rates and pity (GDD §9) ----
        public double FiveStarRate { get; set; } = 0.02;
        public double FourStarRate { get; set; } = 0.18;

        /// <summary>Nth pull with no ★4+ returns a ★4 — exactly ★4, never ★5, or the ★4 floor is 0.</summary>
        public int SoftPityPulls { get; set; } = 10;

        /// <summary>Nth pull with no ★5 returns a ★5.</summary>
        public int HardPityPulls { get; set; } = 40;

        /// <summary>
        /// Whether the "soft pity pays exactly ★4" qualifier stops applying once every ★4 is owned.
        /// <para>
        /// GDD §9 writes the qualifier as conditional ("<i>khi roster ★4 chưa đủ</i>") and gives its
        /// reason: it protects the ★4 ownership floor in §3. That reason expires the moment the last
        /// ★4 is owned — which, under new-character-first, is within the first handful of ★4 hits.
        /// So for the overwhelming majority of a run this flag, not the qualifier, decides the rule.
        /// </para>
        /// <para>
        /// This is a real design lever, not a detail: it changes how many ★5 a player sees, and every
        /// number in §10–§11 is downstream of that.
        /// </para>
        /// </summary>
        public bool SoftPityLapsesWhenFourStarRosterComplete { get; set; } = true;

        /// <summary>Focus multiplies the chosen character's weight inside its own rarity.</summary>
        public int FocusWeight { get; set; } = 3;

        // ---- fragments and progression (GDD §11) ----
        public int DuplicateFragments { get; set; } = 2;

        /// <summary>Affinity grants this many fragments per owned character, at the final milestone only.</summary>
        public int AffinityFragmentsPerCharacter { get; set; } = 2;

        /// <summary>1+2+3+4 across four skill nodes.</summary>
        public int FragmentsToMaxCharacter { get; set; } = 10;

        /// <summary>Resonance, same rarity: erases nothing measurable — it exists so the player can
        /// redirect fragments to the character they want. See GDD §11.</summary>
        public int SameRarityConversionCost { get; set; } = 5;

        /// <summary>Resonance, one rarity up. This is the overflow drain, and the rate that decides
        /// whether The Ascent has anything to pay out.</summary>
        public int CrossRarityConversionCost { get; set; } = 10;

        // ---- Astral Shard budget (GDD §10) ----
        public int CampaignShard { get; set; } = 180;
        public int AscentShardPerFloor { get; set; } = 6;
        public int AscentFloors { get; set; } = 40;

        public int AscentShard => AscentShardPerFloor * AscentFloors;

        /// <summary>One pull costs one Shard, so the Shard budget IS the pull count.</summary>
        public int TotalShardBudget => CampaignShard + AscentShard;

        // ---- Resonance Dust (GDD §10) ----
        // Both of these are hand-set DESIGN INPUTS, not derived from the §8 level formula.
        public int LevelDustTotal { get; set; } = 215_000;
        public int CampaignDust { get; set; } = 40_000;

        public int CatalystPriceDust { get; set; } = 1_200;
        public int DustPerRun { get; set; } = 600;
        public double RunMinutes { get; set; } = 3.0;

        public static int RarityIndex(int rarity)
        {
            if (rarity < LowestRarity || rarity > HighestRarity)
                throw new ArgumentOutOfRangeException(
                    nameof(rarity), rarity, "Rarity must be 3, 4 or 5.");
            return rarity - LowestRarity;
        }

        public int RosterSizeOf(int rarity) => RosterSize[RarityIndex(rarity)];

        /// <summary>Fragments needed to max every character of this rarity: 60 / 50 / 30.</summary>
        public int FragmentsToMaxRarity(int rarity) => RosterSizeOf(rarity) * FragmentsToMaxCharacter;

        /// <summary>Affinity total for a fully-owned rarity: +12 / +10 / +6.</summary>
        public int AffinityFragmentsFor(int rarity) =>
            RosterSizeOf(rarity) * AffinityFragmentsPerCharacter;

        /// <summary>
        /// Deep enough to be safe: <see cref="RosterSize"/> is copied, because a shallow clone would
        /// share that array and a test that shrinks the roster on a clone would silently rewrite the
        /// original's design constants too.
        /// </summary>
        public EconomyConfig Clone()
        {
            var copy = (EconomyConfig)MemberwiseClone();
            copy.RosterSize = (int[])RosterSize.Clone();
            return copy;
        }
    }
}
