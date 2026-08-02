using System;
using System.Collections.Generic;

namespace ColorStackSort.Logic
{
    /// <summary>
    /// Builds levels that are solvable BY CONSTRUCTION: start from a solved board and scramble it
    /// with steps that are each individually invertible by a legal player move. The recorded
    /// inverses, replayed in reverse order, are a guaranteed solution.
    /// <para>
    /// Guaranteed-solvable is not the same as un-losable. A player can still legal-move into a
    /// dead board; that is inherent to the genre and is answered by undo, not here.
    /// </para>
    /// </summary>
    public static class LevelGenerator
    {
        private const int MaxAttempts = 8;

        /// <summary>
        /// Extra steps allowed past the budget purely to escape a board that scrambled back into a
        /// solved state. Each step roughly halves the chance of still being solved, so 32 makes the
        /// escape effectively certain.
        /// </summary>
        private const int SolvedEscapeSteps = 32;

        /// <summary>
        /// Deterministic: the same <paramref name="seed"/> always yields the same level, on every
        /// runtime — see <see cref="DeterministicRandom"/> for why that needs its own generator.
        /// </summary>
        public static GeneratedLevel Generate(LevelParams parameters, int seed)
        {
            parameters.Validate();
            var random = new DeterministicRandom(seed);

            for (var attempt = 0; attempt < MaxAttempts; attempt++)
            {
                var board = BuildSolvedBoard(parameters);
                var steps = Scramble(board, parameters, random);

                // A scramble can land back on a solved board — e.g. moving a whole colour into an
                // empty container leaves every container monochrome and unique. Retry rather than
                // hand back a level that is already won.
                if (steps.Count > 0 && !board.IsSolved)
                    return new GeneratedLevel(board, BuildSolution(steps), parameters, seed);
            }

            throw new InvalidOperationException(
                $"Could not scramble a board for [{parameters}] in {MaxAttempts} attempts — every " +
                $"attempt landed back on a solved board even after {SolvedEscapeSteps} escape steps.");
        }

        /// <summary>
        /// The GENERATOR's rule — deliberately looser than <see cref="BoardState.IsLegal"/>,
        /// which is the player's. This one may move PART of a run; the player's never can.
        /// The two must not be merged: this rule exists to keep the scramble invertible, and
        /// collapsing them yields levels the player cannot solve.
        /// </summary>
        public static bool IsLegalReverseStep(BoardState board, ReverseStep step)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));

            if (step.Amount < 1) return false;
            if (step.From == step.To) return false;
            if (step.From < 0 || step.From >= board.ContainerCount) return false;
            if (step.To < 0 || step.To >= board.ContainerCount) return false;

            var source = board[step.From];
            if (source.IsEmpty) return false;

            var runLength = source.TopRunLength;

            // Never take more than the top run. A mixed handful is not a run, and the forward move
            // undoing it would carry only the run back, stranding the rest — for [Red,Red,Blue] the
            // top run is one Blue, so taking three would strand two Reds and break solvability.
            if (step.Amount > runLength) return false;

            // Taking the WHOLE run is allowed only when that empties the source. Otherwise the item
            // it exposes has a different colour (the run was maximal), so moving the run back onto
            // the source would be an illegal player move.
            if (step.Amount == runLength && runLength != source.Count) return false;

            var destination = board[step.To];
            if (destination.FreeSpace < step.Amount) return false;

            // The destination's top must DIFFER, so what we place forms a run of exactly Amount.
            // If it matched, the forward move would scoop up the destination's own items too.
            return destination.IsEmpty || destination.Top != source.Top;
        }

        private static BoardState BuildSolvedBoard(LevelParams parameters)
        {
            var board = new BoardState(parameters.ContainerCount, parameters.Capacity);
            for (var i = 0; i < parameters.ColorCount; i++)
                board[i].PushRange(new ColorId(i), parameters.Capacity);

            // Guards the invariant IsSolved leans on: exactly one full container per colour.
            if (!board.IsSolved)
                throw new InvalidOperationException(
                    $"Constructed board for [{parameters}] does not read as solved — " +
                    "the items-per-colour == capacity invariant has been broken.");

            return board;
        }

        private static List<ReverseStep> Scramble(
            BoardState board, LevelParams parameters, DeterministicRandom random)
        {
            var applied = new List<ReverseStep>(parameters.ScrambleSteps);
            var candidates = new List<ReverseStep>();
            ReverseStep? previous = null;

            var maxSteps = parameters.ScrambleSteps + SolvedEscapeSteps;
            for (var step = 0; step < maxSteps; step++)
            {
                // Spend the budget, then keep going while the board still reads as solved. A
                // scramble can wander back onto a win — moving a whole colour into an empty
                // container leaves every container monochrome and unique — and handing the player
                // an already-won level is worse than overspending the budget by a few steps.
                // Measured: without this, ScrambleSteps = 1 threw for ~0.4% of seeds.
                if (step >= parameters.ScrambleSteps && !board.IsSolved) break;

                CollectCandidates(board, previous, candidates);

                // A stall is usually not a dead end. Near saturation the only surviving candidate
                // is often the exact undo of the last step, which the filter drops — so the filter
                // turns a momentary stall into a permanent stop. Retry unfiltered before giving up.
                // Measured on a 4-colour board: this lifts a hard ceiling of ~12 moves to the full
                // budget, with zero loss of solvability across 200 replayed levels per setting.
                if (candidates.Count == 0) CollectCandidates(board, null, candidates);

                // Genuinely nothing legal left. Stop with what we have; the steps recorded so far
                // still form a valid solution. Never throw, never spin.
                if (candidates.Count == 0) break;

                var chosen = candidates[random.Next(candidates.Count)];
                board[chosen.To].PushRange(board[chosen.From].PopRange(chosen.Amount), chosen.Amount);

                applied.Add(chosen);
                previous = chosen;
            }

            return applied;
        }

        private static void CollectCandidates(BoardState board, ReverseStep? previous, List<ReverseStep> into)
        {
            into.Clear();

            for (var from = 0; from < board.ContainerCount; from++)
            {
                var source = board[from];
                if (source.IsEmpty) continue;

                // The `amount <= runLength` bound is load-bearing, not just an optimisation: it is
                // what stops a partial take from ever reaching past the top run. Mutation testing
                // showed that breaking IsLegalReverseStep's matching guard alone is invisible to
                // the solution-replay test, because this bound never offers the bad candidate.
                // Widen this loop and that guard becomes the only thing standing between the
                // generator and unsolvable levels.
                var runLength = source.TopRunLength;
                for (var amount = 1; amount <= runLength; amount++)
                for (var to = 0; to < board.ContainerCount; to++)
                {
                    var step = new ReverseStep(from, to, amount);
                    if (!IsLegalReverseStep(board, step)) continue;
                    if (previous.HasValue && IsImmediateUndo(step, previous.Value)) continue;

                    into.Add(step);
                }
            }
        }

        /// <summary>
        /// Stops the scramble oscillating between two states. Only a preference, not a rule — see
        /// <see cref="Scramble"/>, which deliberately ignores it rather than stall out.
        /// </summary>
        internal static bool IsImmediateUndo(ReverseStep step, ReverseStep previous) =>
            step.From == previous.To && step.To == previous.From && step.Amount == previous.Amount;

        /// <summary>
        /// Scramble steps R1..Rn take the solved board to the scrambled one, so the solution is
        /// their inverses applied in REVERSE order: inv(Rn) first, inv(R1) last.
        /// </summary>
        private static Move[] BuildSolution(List<ReverseStep> applied)
        {
            var solution = new Move[applied.Count];
            for (var i = 0; i < applied.Count; i++)
                solution[i] = applied[applied.Count - 1 - i].ToForwardMove();

            return solution;
        }
    }
}
