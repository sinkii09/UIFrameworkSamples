using System;
using ColorStackSort.Logic;

namespace ColorStackSort
{
    /// <summary>
    /// Turns taps and undo presses into board intents: reads the outcome off the ViewModel and
    /// decides which tube animates. Extracted from <see cref="BoardView"/> for the same reason
    /// <see cref="BoardRenderer"/> was — the view had grown past the file-size limit doing three
    /// unrelated jobs (framework lifecycle, scene-graph construction, input routing).
    /// </summary>
    /// <remarks>
    /// A plain class, not a MonoBehaviour: no lifecycle of its own, and a second Awake/OnDestroy
    /// ordering to reason about would be pure cost.
    /// <para>
    /// It deliberately keeps <b>no</b> copy of the busy/solved gate. That flag is cleared from
    /// <see cref="BoardView"/>'s animation <c>finally</c>, and a second copy drifting out of sync
    /// with the first is exactly how input deadlocks permanently — so it is read live through
    /// <see cref="_isBlocked"/> instead of mirrored here.
    /// </para>
    /// </remarks>
    // internal, matching BoardRenderer: it takes one in its constructor, and a public signature
    // exposing an internal type is a CS0051 accessibility error.
    internal sealed class BoardInputRouter
    {
        /// <summary>
        /// Starts a move animation. Named rather than a bare <c>Action&lt;,,,,&gt;</c> so the two
        /// ends cannot be swapped at a call site without a reader noticing.
        /// </summary>
        /// <param name="isForwardMove">
        /// Whether this is a move being made, as opposed to one being undone. Two consequences hang
        /// off it — the win panel and the completion celebration — so it is named for the fact it
        /// carries rather than for either consequence.
        /// </param>
        internal delegate void MovePlayer(
            int fromIndex, int toIndex, int count, bool isForwardMove, object description);

        private readonly BoardViewModel _viewModel;
        private readonly BoardRenderer _renderer;
        private readonly Func<bool> _isBlocked;
        private readonly MovePlayer _playMove;

        internal BoardInputRouter(
            BoardViewModel viewModel,
            BoardRenderer renderer,
            Func<bool> isBlocked,
            MovePlayer playMove)
        {
            _viewModel = viewModel;
            _renderer = renderer;
            _isBlocked = isBlocked;
            _playMove = playMove;
        }

        internal void HandleTap(int index)
        {
            if (_isBlocked()) return;

            var result = _viewModel.Tap(index);
            switch (result.Outcome)
            {
                case TapOutcome.Selected:
                    SetRunLifted(result.From, true);
                    break;
                case TapOutcome.Deselected:
                    SetRunLifted(result.From, false);
                    break;
                case TapOutcome.Moved:
                    _playMove(result.From, result.To, result.Count, true, result);
                    break;
                case TapOutcome.Rejected:
                    _renderer[result.To].Shake();
                    break;
            }
        }

        internal void HandleUndo()
        {
            if (_isBlocked() || !_viewModel.CanUndo.Value) return;

            // The lifted run belongs to the SELECTED tube, not to either end of the move being
            // reversed — and it is lowered BEFORE the board mutates, since SetRunLifted derives the
            // run length from the board and would otherwise strand balls in the air.
            var selected = _viewModel.SelectedContainer;
            if (selected != BoardInteraction.NoSelection) SetRunLifted(selected, false);

            if (!_viewModel.TryUndo(out var record)) return;

            // Same animator, ends swapped — a move played backwards IS the undo animation. Which
            // also means the undo's DESTINATION is record.From.
            _playMove(record.To, record.From, record.Count, false, record);
        }

        private void SetRunLifted(int tubeIndex, bool lifted)
        {
            var container = _viewModel.Board[tubeIndex];
            if (container.IsEmpty) return;

            _renderer[tubeIndex].AnimateTopRun(container.TopRunLength, lifted);
        }
    }
}
