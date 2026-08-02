using System;
using ColorStackSort.Logic;
using Cysharp.Threading.Tasks;
using R3;
using Sinkii09.UIFramework;
using UnityEngine;
using VContainer;

namespace ColorStackSort
{
    /// <summary>
    /// Thin reactive wrapper over <see cref="BoardInteraction"/>. Deliberately holds no rules of its
    /// own: <see cref="BoardInteraction"/> owns the board's only write path, and duplicating any of
    /// it here would create a second one that eventually disagrees.
    /// </summary>
    /// <remarks>
    /// [Preserve] guards against IL2CPP link-time stripping — this type is only ever reached
    /// reflectively, via UIViewRegistry's assembly scan and VContainer's child-scope registration.
    /// </remarks>
    // Fully qualified: VContainer ships its own PreserveAttribute, so the short name is ambiguous
    // in any file that also uses VContainer.
    [UnityEngine.Scripting.Preserve]
    public sealed class BoardViewModel : ViewModelBase, IViewModel<BoardArgs>
    {
        /// <summary>Used when the view is shown without args, so Phase 3 can wire up incrementally.</summary>
        private static readonly LevelParams FallbackParams = new LevelParams(4, 4, 2, 60);

        public ReactiveProperty<int> MoveCount { get; } = new(0);
        public ReactiveProperty<int> Level { get; } = new(DifficultyCurve.FirstLevel);

        /// <summary>
        /// Drives the undo button's interactability. Kept as its own property rather than derived
        /// from <see cref="MoveCount"/> — they diverge the moment a move is rejected or the board
        /// is solved.
        /// </summary>
        public ReactiveProperty<bool> CanUndo { get; } = new(false);

        public Subject<Unit> Solved { get; } = new();

        private BoardInteraction _interaction;
        private LevelParams _params = FallbackParams;
        private int _seed;
        private int _level = DifficultyCurve.FirstLevel;
        private bool _initialized;

        private readonly GameLifecycleManager _lifecycle;

        [Inject]
        public BoardViewModel(GameLifecycleManager lifecycle)
        {
            _lifecycle = lifecycle;

            MoveCount.AddTo(ref _disposables);
            Level.AddTo(ref _disposables);
            CanUndo.AddTo(ref _disposables);
            Solved.AddTo(ref _disposables);
        }

        /// <summary>Runs before <c>BindViewModel</c> and before <see cref="OnShow"/>.</summary>
        public void Initialize(BoardArgs args)
        {
            if (args == null) return;

            _params = args.Params;
            _seed = args.Seed;
            _level = args.Level;
            _initialized = true;
        }

        public BoardState Board => _interaction?.Board;
        public int ContainerCount => _interaction?.Board.ContainerCount ?? 0;
        public int SelectedContainer => _interaction?.SelectedContainer ?? BoardInteraction.NoSelection;
        public bool IsSolved => _interaction != null && _interaction.IsSolved;

        public override void OnShow()
        {
            if (!_initialized)
            {
                // Warn rather than fail: a missing Initialize is a wiring bug, and a silent
                // fallback would hide it until someone wondered why every level looked the same.
                Debug.LogWarning($"[BoardViewModel] Shown without BoardArgs — falling back to " +
                                 $"[{FallbackParams}] seed {_seed}. Pass args via ShowAsync<BoardView, BoardArgs>.");
            }

            MoveCount.Value = 0;
            Level.Value = _level;
            CanUndo.Value = false;
            _interaction = new BoardInteraction(LevelGenerator.Generate(_params, _seed).Board);
        }

        protected override void OnHide()
        {
            _interaction = null;
        }

        /// <summary>
        /// Routes a tap. The returned result is what the view animates from — it carries the run
        /// length and colour captured before the board mutated.
        /// </summary>
        public TapResult Tap(int container)
        {
            if (_interaction == null) return TapResult.None();

            var result = _interaction.Tap(container);
            if (result.Outcome != TapOutcome.Moved) return result;

            MoveCount.Value++;

            // Solving ends the level, so there is nothing left to undo into.
            CanUndo.Value = _interaction.CanUndo && !_interaction.IsSolved;

            // Fires before the view has finished animating: the board is already solved in state,
            // and any listener wanting to wait for the animation should hook the view, not this.
            if (_interaction.IsSolved) Solved.OnNext(Unit.Default);

            return result;
        }

        /// <summary>
        /// Reverses the last move and returns what was reversed, so the view can animate it.
        /// <para>
        /// There is deliberately no <c>Restart()</c> counterpart. Regenerating the board here would
        /// leave the view rendering the old one and permanently input-locked — the view rebuilds
        /// only in <c>OnShowAsync</c>. Restart is a state re-entry, not a ViewModel operation.
        /// </para>
        /// </summary>
        public bool TryUndo(out MoveRecord record)
        {
            record = default;
            if (_interaction == null || !_interaction.TryUndo(out record)) return false;

            MoveCount.Value--;
            CanUndo.Value = _interaction.CanUndo;
            return true;
        }

        /// <summary>
        /// Rebuilds the current level from scratch.
        /// <para>
        /// Note what this does NOT do: regenerate the board here. The view builds its tubes only in
        /// <c>OnShowAsync</c>, so a local rebuild would leave the old board on screen and — because
        /// the view's solved latch also only clears there — permanently unable to accept a tap.
        /// Re-entering the state is what makes the view rebuild.
        /// </para>
        /// <para>
        /// No cancellation token: <c>RestartCurrentStateAsync</c> hides this view partway through
        /// its own await, so a view-scoped token would cancel the restart halfway and leave the
        /// stack cleared with nothing shown. Shutdown is still covered — the lifecycle manager
        /// links <c>Application.exitCancellationToken</c> itself.
        /// </para>
        /// </summary>
        public void OnRestartPressed() =>
            _lifecycle.RestartCurrentStateAsync().Forget(LogUnlessCancelled);

        /// <summary>
        /// Ignores cancellation. As of UIFramework v1.2.0 a cancelled show throws rather than
        /// returning quietly, so a bare handler would log an error on every shutdown mid-transition.
        /// </summary>
        private static void LogUnlessCancelled(Exception ex)
        {
            if (ex is OperationCanceledException) return;

            Debug.LogError($"[BoardViewModel] Restart failed: {ex}");
        }
    }
}
