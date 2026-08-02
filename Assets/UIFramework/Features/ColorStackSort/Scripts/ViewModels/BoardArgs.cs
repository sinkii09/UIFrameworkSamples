using ColorStackSort.Logic;
using Sinkii09.UIFramework;

namespace ColorStackSort
{
    /// <summary>
    /// Which level to build. Carries only the recipe — the board itself is regenerated from the
    /// seed, so nothing about a level is ever stored or passed around as data.
    /// <para>
    /// Must implement <see cref="IViewArgs"/>: the framework constrains
    /// <c>IViewModel&lt;TArgs&gt;</c> to it.
    /// </para>
    /// </summary>
    public sealed class BoardArgs : IViewArgs
    {
        public BoardArgs(LevelParams parameters, int seed, int level)
        {
            Params = parameters;
            Seed = seed;
            Level = level;
        }

        public LevelParams Params { get; }
        public int Seed { get; }

        /// <summary>Carried for display only — the recipe above already encodes the difficulty.</summary>
        public int Level { get; }
    }
}
