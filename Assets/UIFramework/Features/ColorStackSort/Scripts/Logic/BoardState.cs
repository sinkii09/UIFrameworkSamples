using System;
using System.Collections.Generic;

namespace ColorStackSort.Logic
{
    /// <summary>
    /// The full board, and the owner of the PLAYER's move rule.
    /// <para>
    /// Do not merge <see cref="IsLegal"/> with <see cref="LevelGenerator.IsLegalReverseStep"/>.
    /// The generator deliberately uses a LOOSER rule (it may move part of a run); this one only
    /// ever moves a whole maximal run. They look similar and are not interchangeable — collapsing
    /// them produces levels the player cannot solve.
    /// </para>
    /// </summary>
    public sealed class BoardState
    {
        private readonly StackContainer[] _containers;

        public BoardState(int containerCount, int capacity)
        {
            if (containerCount < 2)
                throw new ArgumentOutOfRangeException(
                    nameof(containerCount), containerCount, "A board needs at least 2 containers.");

            _containers = new StackContainer[containerCount];
            for (var i = 0; i < containerCount; i++) _containers[i] = new StackContainer(capacity);
        }

        private BoardState(StackContainer[] containers) => _containers = containers;

        public int ContainerCount => _containers.Length;
        public IReadOnlyList<StackContainer> Containers => _containers;

        public StackContainer this[int index] =>
            index >= 0 && index < _containers.Length
                ? _containers[index]
                : throw new ArgumentOutOfRangeException(
                    nameof(index), index, $"Valid range is 0..{_containers.Length - 1}.");

        /// <summary>
        /// The player's rule: move the whole maximal top run of <c>From</c> onto <c>To</c>.
        /// Legal when the source is non-empty, the two differ, the destination has room for the
        /// entire run, and the destination is empty or its top colour matches the run's.
        /// </summary>
        public bool IsLegal(Move move)
        {
            if (move.From == move.To) return false;
            if (move.From < 0 || move.From >= _containers.Length) return false;
            if (move.To < 0 || move.To >= _containers.Length) return false;

            var source = _containers[move.From];
            if (source.IsEmpty) return false;

            var destination = _containers[move.To];
            if (destination.FreeSpace < source.TopRunLength) return false;

            return destination.IsEmpty || destination.Top == source.TopRunColor;
        }

        public void Apply(Move move)
        {
            if (!IsLegal(move))
                throw new InvalidOperationException($"Illegal move {move}.");

            var source = _containers[move.From];
            var runLength = source.TopRunLength;
            var color = source.PopRange(runLength);

            _containers[move.To].PushRange(color, runLength);
        }

        /// <summary>
        /// Reverses an already-applied move. This is the board's SECOND write path, and it is not
        /// interchangeable with <see cref="Apply"/>.
        /// <para>
        /// Undoing "3 reds onto 2 reds" means lifting exactly 3 items off a run of 5 — not a legal
        /// player move, which always takes a whole maximal run. Do not route undo through
        /// <see cref="Apply"/> or <see cref="IsLegal"/>, and do not merge the two rule sets. This is
        /// the same trap as <see cref="LevelGenerator.IsLegalReverseStep"/> vs <see cref="IsLegal"/>,
        /// third occurrence.
        /// </para>
        /// <para>
        /// Every check below is load-bearing. <see cref="StackContainer.PopRange"/> cannot protect
        /// this method: it derives the expected colour from whatever is on top and only verifies the
        /// range is internally uniform, so undoing a red record against a blue-topped container pops
        /// blues and pushes reds back without throwing — silently breaking the
        /// <see cref="LevelParams.ItemsPerColor"/> invariant that <see cref="IsSolved"/> depends on,
        /// and leaving an unsolvable board with no exception and no log.
        /// </para>
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The record does not describe the board's current state — it is stale, out of order, or
        /// from a different board entirely.
        /// </exception>
        public void UndoMove(MoveRecord record)
        {
            // Re-checked rather than trusted to the MoveRecord constructor: default(MoveRecord) is
            // a Count-0, From==To==0 value that bypasses it entirely, and this method is public.
            // Without this, the ordering rationale below collapses — a Count of 0 passes the
            // container-depth check vacuously and Top throws on an empty container instead.
            if (record.Count < 1)
                throw new InvalidOperationException($"Cannot undo {record}: it carries no items.");
            if (record.From == record.To)
                throw new InvalidOperationException($"Cannot undo {record}: it starts and ends in one container.");

            if (record.From >= _containers.Length)
                throw new ArgumentOutOfRangeException(
                    nameof(record), record.From, $"From is out of range 0..{_containers.Length - 1}.");
            if (record.To >= _containers.Length)
                throw new ArgumentOutOfRangeException(
                    nameof(record), record.To, $"To is out of range 0..{_containers.Length - 1}.");

            // "landing" is where the run went and where undo lifts it from; "origin" is where it
            // came from and where undo puts it back. Naming them by role rather than by From/To
            // keeps the reversal from reading backwards.
            var origin = _containers[record.From];
            var landing = _containers[record.To];

            // Order matters: Top throws on an empty container, so the count check must come first.
            if (landing.Count < record.Count)
                throw new InvalidOperationException(
                    $"Cannot undo {record}: container {record.To} holds only {landing.Count} item(s).");

            if (landing.Top != record.Color)
                throw new InvalidOperationException(
                    $"Cannot undo {record}: container {record.To} is topped by {landing.Top}, not {record.Color}.");

            if (landing.TopRunLength < record.Count)
                throw new InvalidOperationException(
                    $"Cannot undo {record}: container {record.To}'s top run is only {landing.TopRunLength} long.");

            if (origin.FreeSpace < record.Count)
                throw new InvalidOperationException(
                    $"Cannot undo {record}: container {record.From} has only {origin.FreeSpace} free slot(s).");

            // Deliberately !=. Apply pops the MAXIMAL run, so after a correct move the item left
            // exposed on the origin is by definition a different colour (or the origin is empty) —
            // that is the expected state, not the error state. Finding the record's colour back on
            // top means the board has moved on since the record was made, i.e. the history is being
            // replayed out of order.
            if (!origin.IsEmpty && origin.Top == record.Color)
                throw new InvalidOperationException(
                    $"Cannot undo {record}: container {record.From} is still topped by {record.Color}, " +
                    "so this record is stale or out of order.");

            landing.PopRange(record.Count);
            origin.PushRange(record.Color, record.Count);
        }

        /// <summary>
        /// Solved when every non-empty container holds a single colour AND no colour is split
        /// across two containers. The second clause matters: without it, a board whose reds sit
        /// monochrome in two separate containers would read as solved.
        /// <para>
        /// Pairwise rather than a HashSet so the check allocates nothing — it runs after every
        /// player move, and container counts are small enough that O(n²) is free.
        /// </para>
        /// </summary>
        public bool IsSolved
        {
            get
            {
                for (var i = 0; i < _containers.Length; i++)
                {
                    var container = _containers[i];
                    if (container.IsEmpty) continue;
                    if (!container.IsMonochrome) return false;

                    for (var j = i + 1; j < _containers.Length; j++)
                    {
                        var other = _containers[j];
                        if (!other.IsEmpty && other.Top == container.Top) return false;
                    }
                }

                return true;
            }
        }

        public BoardState Clone()
        {
            var clones = new StackContainer[_containers.Length];
            for (var i = 0; i < _containers.Length; i++) clones[i] = _containers[i].Clone();

            return new BoardState(clones);
        }
    }
}
