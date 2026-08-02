using Sinkii09.UIFramework;

namespace ColorStackSort
{
    /// <summary>
    /// What the win panel reports. Display data only — the next level comes from
    /// <see cref="LevelProgressService"/>, not from here, so the panel cannot disagree with the
    /// service about where the player is.
    /// </summary>
    public sealed class ColorStackSortWinArgs : IViewArgs
    {
        public ColorStackSortWinArgs(int level, int moveCount)
        {
            Level = level;
            MoveCount = moveCount;
        }

        public int Level { get; }
        public int MoveCount { get; }
    }
}
