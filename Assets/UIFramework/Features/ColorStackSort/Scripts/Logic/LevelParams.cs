using System;

namespace ColorStackSort.Logic
{
    /// <summary>
    /// Difficulty knobs for one generated level.
    /// <para>
    /// Items-per-colour equals <see cref="Capacity"/> by construction — the solved board puts
    /// exactly one full container's worth of each colour. That invariant is what makes
    /// <see cref="BoardState.IsSolved"/>'s "no colour in two containers" clause satisfiable; a
    /// colour needing more than one container's worth could never be collected. Keep them equal.
    /// </para>
    /// </summary>
    public readonly struct LevelParams
    {
        public readonly int ColorCount;
        public readonly int Capacity;
        public readonly int EmptyContainers;

        /// <summary>
        /// How many scramble steps to apply. This is a BUDGET, not a guarantee: the scramble stops
        /// early if it genuinely runs out of legal steps, and may overrun slightly to escape a
        /// board that wandered back onto a solved state.
        /// <para>
        /// Do not treat this as the difficulty dial. More steps is a longer random walk, not a
        /// harder puzzle — the walk can wander back toward easy states, and solution length is not
        /// a difficulty measure. Real difficulty comes from the board's SHAPE: more colours, deeper
        /// containers, fewer empties. Phase 4's curve should move those.
        /// </para>
        /// </summary>
        public readonly int ScrambleSteps;

        public LevelParams(int colorCount, int capacity, int emptyContainers, int scrambleSteps)
        {
            ColorCount = colorCount;
            Capacity = capacity;
            EmptyContainers = emptyContainers;
            ScrambleSteps = scrambleSteps;
        }

        public int ContainerCount => ColorCount + EmptyContainers;
        public int ItemsPerColor => Capacity;

        public void Validate()
        {
            if (ColorCount < 2)
                throw new ArgumentException($"ColorCount must be at least 2, was {ColorCount}.", nameof(ColorCount));

            // ColorId stores a byte, so colour indices run 0..255.
            if (ColorCount > 256)
                throw new ArgumentException($"ColorCount must be at most 256, was {ColorCount}.", nameof(ColorCount));

            if (Capacity < 2)
                throw new ArgumentException($"Capacity must be at least 2, was {Capacity}.", nameof(Capacity));

            // Without a free container nothing can ever move: every container starts full.
            if (EmptyContainers < 1)
                throw new ArgumentException(
                    $"EmptyContainers must be at least 1, was {EmptyContainers}.", nameof(EmptyContainers));

            if (ScrambleSteps < 1)
                throw new ArgumentException(
                    $"ScrambleSteps must be at least 1, was {ScrambleSteps}.", nameof(ScrambleSteps));
        }

        public override string ToString() =>
            $"{ColorCount} colours x{Capacity}, +{EmptyContainers} empty, {ScrambleSteps} steps";
    }
}
