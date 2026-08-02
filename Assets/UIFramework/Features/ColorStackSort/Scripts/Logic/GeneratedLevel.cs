using System;
using System.Collections.Generic;

namespace ColorStackSort.Logic
{
    /// <summary>
    /// A scrambled board plus a move sequence that provably solves it.
    /// <para>
    /// <see cref="Solution"/> is one solution, not necessarily the shortest — it is the inverse
    /// of the scramble. Its purpose is to prove solvability, not to power a hint system; a hint
    /// would need to re-solve from the player's current state, which is a different problem.
    /// </para>
    /// </summary>
    public sealed class GeneratedLevel
    {
        public GeneratedLevel(BoardState board, IReadOnlyList<Move> solution, LevelParams parameters, int seed)
        {
            Board = board ?? throw new ArgumentNullException(nameof(board));
            Solution = solution ?? throw new ArgumentNullException(nameof(solution));
            Params = parameters;
            Seed = seed;
        }

        public BoardState Board { get; }
        public IReadOnlyList<Move> Solution { get; }
        public LevelParams Params { get; }
        public int Seed { get; }
    }
}
