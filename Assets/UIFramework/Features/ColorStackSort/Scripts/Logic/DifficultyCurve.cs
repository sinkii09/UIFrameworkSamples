using System;

namespace ColorStackSort.Logic
{
    /// <summary>
    /// Maps a level number onto the shape of its board, and onto the seed that generates it.
    /// <para>
    /// Difficulty is moved by the board's SHAPE — more colours, fewer spare containers — never by
    /// <see cref="LevelParams.ScrambleSteps"/>. A longer random walk is not a harder puzzle: the
    /// walk can wander back toward easy states, and solution length is not a difficulty measure.
    /// See the remarks on <see cref="LevelParams.ScrambleSteps"/>.
    /// </para>
    /// </summary>
    public static class DifficultyCurve
    {
        /// <summary>Levels are 1-based — level 0 does not exist and is rejected.</summary>
        public const int FirstLevel = 1;

        private const int FixedCapacity = 4;
        private const int MinColors = 3;
        private const int MaxColors = 8;

        /// <summary>Levels per added colour: 3 colours at level 1, 8 by level 21.</summary>
        private const int LevelsPerColor = 4;

        /// <summary>Two spare containers make the early game forgiving; one is the real puzzle.</summary>
        private const int SpareContainerDropLevel = 10;

        /// <summary>
        /// Scramble budget, proportional to board size so a bigger board gets a proportionally
        /// longer walk. Not a difficulty dial — see the class remarks.
        /// </summary>
        private const int ScrambleStepsPerSlot = 8;

        public static LevelParams ForLevel(int levelIndex)
        {
            if (levelIndex < FirstLevel)
                throw new ArgumentOutOfRangeException(
                    nameof(levelIndex), levelIndex, $"Levels are 1-based; the first level is {FirstLevel}.");

            var colors = Math.Min(MinColors + (levelIndex - FirstLevel) / LevelsPerColor, MaxColors);
            var emptyContainers = levelIndex < SpareContainerDropLevel ? 2 : 1;
            var scrambleSteps = colors * FixedCapacity * ScrambleStepsPerSlot;

            var parameters = new LevelParams(colors, FixedCapacity, emptyContainers, scrambleSteps);

            // Validate here rather than trusting the arithmetic: a future retune that pushes a knob
            // out of range should fail at the curve, not deep inside the generator.
            parameters.Validate();
            return parameters;
        }

        /// <summary>
        /// The seed for a level. Deterministic, so level N is always the same board — which is what
        /// makes Restart reproduce the board the player just failed rather than a fresh one.
        /// <para>
        /// <paramref name="baseSeed"/> is a salt, not a difficulty knob: changing it reshuffles
        /// every level at once, which is how a reported bad board gets reproduced or retired.
        /// The multiplier is a large prime so adjacent levels land far apart in the sequence
        /// instead of generating near-identical boards.
        /// </para>
        /// </summary>
        public static int SeedForLevel(int levelIndex, int baseSeed) =>
            unchecked(baseSeed * 486187739 + levelIndex);
    }
}
