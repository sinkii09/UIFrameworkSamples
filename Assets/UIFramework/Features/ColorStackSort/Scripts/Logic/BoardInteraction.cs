using System;
using System.Collections.Generic;

namespace ColorStackSort.Logic
{
    /// <summary>
    /// The tap state machine: which tube is selected, and what a second tap means.
    /// <para>
    /// This is game logic, not presentation, so it lives in the engine-free assembly and is covered
    /// by fast EditMode tests. View-model code must call through here and <b>never</b> mutate the
    /// board directly — this class owns the only write path. Two write paths would eventually
    /// disagree, and the board would drift from what the player sees.
    /// </para>
    /// </summary>
    public sealed class BoardInteraction
    {
        public const int NoSelection = -1;

        private readonly BoardState _board;

        // LIFO. Undo is only ever defined for the most recent move: reversing an older one would
        // have to reason about every move stacked on top of it, and BoardState.UndoMove's checks
        // assume the board is exactly as that move left it.
        private readonly List<MoveRecord> _history = new();

        private int _selected = NoSelection;

        public BoardInteraction(BoardState board)
        {
            _board = board ?? throw new ArgumentNullException(nameof(board));
        }

        public BoardState Board => _board;
        public int SelectedContainer => _selected;
        public bool HasSelection => _selected != NoSelection;
        public bool IsSolved => _board.IsSolved;

        /// <summary>Drops any selection without touching the board — for restarts and undo.</summary>
        public void ClearSelection() => _selected = NoSelection;

        public int HistoryCount => _history.Count;
        public bool CanUndo => _history.Count > 0;

        /// <summary>
        /// Reverses the most recent move and returns what was reversed, so the caller can animate
        /// it. Returns false when there is nothing to undo.
        /// <para>
        /// Try-shaped rather than throwing, for the same reason <see cref="Tap"/> ignores an
        /// out-of-range container: undo arrives from a button, and a tap landing one frame after
        /// the last undo emptied the history is a plausible runtime event, not a programmer error.
        /// </para>
        /// </summary>
        public bool TryUndo(out MoveRecord record)
        {
            if (_history.Count == 0)
            {
                record = default;
                return false;
            }

            record = _history[_history.Count - 1];

            // Board first, history second. UndoMove validates fully before it mutates anything, so
            // a rejected record leaves the board untouched — and popping only after it succeeds
            // means a throw leaves history and board still agreeing with each other, rather than
            // silently discarding the move that would have reconciled them.
            _board.UndoMove(record);
            _history.RemoveAt(_history.Count - 1);

            // A selection made before the undo points at a run that may no longer exist.
            _selected = NoSelection;
            return true;
        }

        /// <summary>
        /// Applies the tap rules. The board mutates only on <see cref="TapOutcome.Moved"/>.
        /// </summary>
        public TapResult Tap(int container)
        {
            // Out of range is a no-op rather than a throw: taps arrive from UI, and a stale
            // callback after a board rebuild is a plausible runtime event, not a programmer error.
            if (container < 0 || container >= _board.ContainerCount) return TapResult.None();

            if (!HasSelection) return TrySelect(container);

            if (container == _selected)
            {
                var deselected = _selected;
                _selected = NoSelection;
                return TapResult.Deselected(deselected);
            }

            return TryMove(container);
        }

        private TapResult TrySelect(int container)
        {
            // Selecting an empty tube would arm a move that can never be legal.
            if (_board[container].IsEmpty) return TapResult.None();

            _selected = container;
            return TapResult.Selected(container);
        }

        private TapResult TryMove(int destination)
        {
            var move = new Move(_selected, destination);

            // Selection is deliberately kept on rejection, so the player can immediately try a
            // different destination without re-picking the source.
            if (!_board.IsLegal(move)) return TapResult.Rejected(_selected, destination);

            // Captured BEFORE Apply — afterwards the source no longer holds the run, and the view
            // would have no way to learn how many balls to animate or what colour they were.
            var source = _board[_selected];
            var count = source.TopRunLength;
            var color = source.TopRunColor;

            _board.Apply(move);

            var from = _selected;
            _selected = NoSelection;

            _history.Add(new MoveRecord(from, destination, count, color));

            return TapResult.Moved(from, destination, count, color);
        }
    }
}
