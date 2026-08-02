using Cysharp.Threading.Tasks;
using R3;
using Sinkii09.UIFramework;
using UnityEngine;
using VContainer;

namespace ColorStackSort
{
    /// <summary>
    /// The win panel's only job: advance the level and rebuild the board.
    /// </summary>
    /// <remarks>
    /// [Preserve]: reached only reflectively, via UIViewRegistry's assembly scan and VContainer's
    /// child-scope registration.
    /// </remarks>
    [UnityEngine.Scripting.Preserve]
    public sealed class ColorStackSortWinViewModel : ViewModelBase, IViewModel<ColorStackSortWinArgs>
    {
        private readonly GameLifecycleManager _lifecycle;
        private readonly LevelProgressService _progress;
        private readonly IUINavigator _navigator;

        /// <summary>Whether this show has already banked its level. See <see cref="OnNextPressed"/>.</summary>
        private bool _advanced;

        public ReactiveProperty<int> Level { get; } = new(0);
        public ReactiveProperty<int> MoveCount { get; } = new(0);

        /// <summary>
        /// Gates the Next button. Starts <b>false</b> and is raised by the view once its entrance
        /// animation has finished — see <see cref="MarkReadyForInput"/> — then drops permanently on
        /// the first press.
        /// </summary>
        public ReactiveProperty<bool> CanAdvance { get; } = new(false);

        [Inject]
        public ColorStackSortWinViewModel(
            GameLifecycleManager lifecycle, LevelProgressService progress, IUINavigator navigator)
        {
            _lifecycle = lifecycle;
            _progress = progress;
            _navigator = navigator;

            Level.AddTo(ref _disposables);
            MoveCount.AddTo(ref _disposables);
            CanAdvance.AddTo(ref _disposables);
        }

        public void Initialize(ColorStackSortWinArgs args)
        {
            if (args == null) return;

            Level.Value = args.Level;
            MoveCount.Value = args.MoveCount;
        }

        /// <summary>
        /// Deliberately leaves <see cref="CanAdvance"/> false. <c>OnShow</c> runs before the view's
        /// entrance animation, and the popup is already interactable during it — enabling here
        /// would arm the trap <see cref="MarkReadyForInput"/> exists to close.
        /// </summary>
        public override void OnShow()
        {
            CanAdvance.Value = false;
            _advanced = false;
        }

        /// <summary>
        /// Called by the view once its entrance animation has completed.
        /// <para>
        /// Until then the navigator is still inside the <c>ShowAsync</c> that is presenting this
        /// very popup, so <c>_isTransitioning</c> is held — and a Next press in that window would
        /// advance the level while both the <c>CloseAllAsync</c> and the <c>ShowAsync</c> inside
        /// <c>RestartCurrentStateAsync</c> were silently dropped by that same guard. The level would
        /// advance, the board would never rebuild, and this popup would be left over a board whose
        /// controls are already disabled: an unrecoverable soft-lock that logs two warnings and no
        /// error.
        /// </para>
        /// </summary>
        public void MarkReadyForInput() => CanAdvance.Value = true;

        /// <summary>
        /// Advances one level and re-enters the gameplay state, which regenerates the board.
        /// <para>
        /// One-shot on purpose. <c>GameLifecycleManager</c>'s re-entrancy guard swallows a second
        /// <c>RestartCurrentStateAsync</c>, but nothing swallows a second <see cref="Advance"/> — a
        /// double tap would leave the service on level N+2 while only one rebuild ran, silently
        /// skipping a level. Advancing inside the state's <c>OnEnterAsync</c> instead is not an
        /// option: Restart re-enters the same state and must not advance.
        /// </para>
        /// <para>
        /// No cancellation token, deliberately. <c>RestartCurrentStateAsync</c> hides this very view
        /// (via <c>CloseAllAsync</c>) partway through its own await, so a token tied to this view's
        /// lifetime would cancel the remainder — clearing the stack and never running
        /// <c>OnEnterAsync</c>, leaving a permanently blank screen. Application shutdown is still
        /// covered: the lifecycle manager links <c>Application.exitCancellationToken</c> internally.
        /// </para>
        /// </summary>
        public void OnNextPressed()
        {
            if (!CanAdvance.Value) return;

            // Second, independent gate. MarkReadyForInput already keeps the button dark until the
            // entrance finishes; this covers the residual window between that animation completing
            // and the navigator clearing its flag, where a transition-time press would be dropped
            // by UINavigator and leave the level advanced with no rebuild.
            if (_navigator != null && _navigator.IsTransitioning) return;

            CanAdvance.Value = false;

            // Advance at most once per show, but retry the rebuild as often as needed. The two are
            // separated because the level is banked to disk the instant Advance() runs: if the
            // rebuild then fails and the player presses Next again, advancing a second time would
            // silently skip a level — the exact failure this method's one-shot rule guards against.
            if (!_advanced)
            {
                _progress.Advance();
                _advanced = true;
            }

            _lifecycle.RestartCurrentStateAsync().Forget(OnRestartFailed);
        }

        /// <summary>
        /// A Forget handler that ignores cancellation. As of UIFramework v1.2.0 a cancelled show
        /// throws rather than returning quietly, so a bare handler would log an error every time
        /// the game shut down mid-transition.
        /// <para>
        /// On a genuine failure it re-arms the button. Without this the popup is dead: the board was
        /// never rebuilt, <see cref="CanAdvance"/> is already false, and only a fresh show raises it
        /// again — which cannot happen, because showing is what just failed.
        /// </para>
        /// </summary>
        private void OnRestartFailed(System.Exception ex)
        {
            if (ex is System.OperationCanceledException) return;

            Debug.LogError($"[ColorStackSortWinViewModel] Next level failed: {ex}");
            CanAdvance.Value = true;
        }
    }
}
