namespace AstralChorus.Logic
{
    /// <summary>
    /// What a player ends up with after spending everything through the Resonance rule.
    /// <para>
    /// Same-rarity and cross-rarity conversions are counted SEPARATELY on purpose. They cost the
    /// same one Catalyst each, so a single total would hide the finding that same-rarity conversion
    /// fires in essentially zero runs — which is the evidence that its published justification
    /// ("erase within-rarity variance") was wrong.
    /// </para>
    /// </summary>
    public sealed class CascadeResult
    {
        public CascadeResult(int[] maxedPerRarity, int[] leftoverPerRarity,
                             int sameRarityConversions, int crossRarityConversions)
        {
            MaxedPerRarity = maxedPerRarity;
            LeftoverPerRarity = leftoverPerRarity;
            SameRarityConversions = sameRarityConversions;
            CrossRarityConversions = crossRarityConversions;
        }

        /// <summary>Characters fully maxed, [rarityIndex].</summary>
        public int[] MaxedPerRarity { get; }

        /// <summary>Fragments still held after everything has been spent or pushed up, [rarityIndex].</summary>
        public int[] LeftoverPerRarity { get; }

        public int SameRarityConversions { get; }
        public int CrossRarityConversions { get; }

        /// <summary>One Catalyst per conversion, in either direction.</summary>
        public int CatalystSpent => SameRarityConversions + CrossRarityConversions;

        public int MaxedOf(int rarity) => MaxedPerRarity[EconomyConfig.RarityIndex(rarity)];

        public int LeftoverOf(int rarity) => LeftoverPerRarity[EconomyConfig.RarityIndex(rarity)];

        public int TotalMaxed
        {
            get
            {
                var total = 0;
                for (var i = 0; i < MaxedPerRarity.Length; i++) total += MaxedPerRarity[i];
                return total;
            }
        }
    }
}
