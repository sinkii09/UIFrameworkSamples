namespace RecyclerShowcase
{
    /// <summary>Cheap stable integer hash (Knuth multiplicative) for deterministic demo sizes.</summary>
    internal static class ShowcaseHash
    {
        public static int Of(int value)
        {
            unchecked
            {
                return (int)(((uint)value * 2654435761u) >> 16);
            }
        }
    }
}
