using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Sinkii09.UIFramework;
using UnityEngine;
using VContainer;

namespace ColorStackSort
{
    /// <summary>
    /// Renders the board and turns taps into moves. Owns every cross-tube tween.
    /// </summary>
    /// <remarks>
    /// The key is namespaced rather than defaulted to the class name: UIViewRegistry scans every
    /// assembly and drops a second view sharing a key, so an unqualified name would silently
    /// collide with another feature's view. [Preserve] is load-bearing on IL2CPP (Android/iOS) —
    /// both the key lookup and view discovery run on reflection, and a stripped type or attribute
    /// sends the key back to "BoardView", failing the Resources load with nothing shown.
    /// </remarks>
    // Fully qualified: VContainer ships its own PreserveAttribute, so the short name is ambiguous
    // in any file that also uses VContainer.
    [UnityEngine.Scripting.Preserve]
    [UIViewKey("ColorStackSort/BoardView")]
    public sealed class BoardView : UIView<BoardViewModel>
    {
        private const float CelebrationBeatDelaySeconds = 0.4f;

        [SerializeField] private TubeView _tubePrefab;
        [SerializeField] private BallView _ballPrefab;
        [SerializeField] private RectTransform _tubeRow;

        [Tooltip("Must be the LAST sibling under the board root — uGUI draws in hierarchy order, " +
                 "so anything after it would cover travelling balls.")]
        [SerializeField] private RectTransform _travelOverlay;

        [SerializeField] private BallPalette _palette = new();

        [SerializeField] private BoardControlBar _controlBar;

        [Tooltip("Optional. One emitter shared by every tube's completion burst.")]
        [SerializeField] private JuiceBurstEmitter _burstEmitter;

        public override UILayer Layer => UILayer.Screen;

        private BoardRenderer _renderer;
        private BoardInputRouter _router;
        private BoardMoveAnimator _animator;
        private BoardAnimationScope _scope;
        private bool _isAnimating;
        private bool _isSolved;
        private IUINavigator _navigator;

        // Method injection: UIViewBase already claims the [Inject] hook for its animator.
        [Inject]
        private void Construct(IUINavigator navigator) => _navigator = navigator;

        protected override void BindViewModel(BoardViewModel vm)
        {
            // Locks the board the moment the puzzle is won; the win panel is raised later, from
            // RunAnimationAsync, once the winning move has finished animating.
            vm.Solved.Subscribe(_ => _isSolved = true).AddTo(ref _showDisposables);

            if (_controlBar == null)
            {
                Debug.LogError("[BoardView] Control bar not assigned — undo and restart are dead. " +
                               "Delete the BoardView prefab and re-run Tools/ColorStackSort/Build Prefabs.", this);
                return;
            }

            _controlBar.Bind(vm, HandleUndo, ref _showDisposables);
        }

        // Runs after vm.OnShow(), so the board exists by now.
        protected override UniTask OnShowAsync(CancellationToken ct)
        {
            _scope ??= new BoardAnimationScope(destroyCancellationToken);
            _scope.Renew();
            _isSolved = false;
            _isAnimating = false;

            _renderer ??= new BoardRenderer(
                _tubePrefab, _ballPrefab, _tubeRow, _travelOverlay, _palette, HandleTap, _burstEmitter);

            // Built before Build(), because a tap becomes possible the moment tubes exist. Rebuilt
            // every show rather than cached: it captures this show's ViewModel, and the framework is
            // free to hand out a different instance next time.
            _router = new BoardInputRouter(
                ViewModel, _renderer, () => _isAnimating || _isSolved, PlayMove);

            _renderer.Build(ViewModel.Board, this);

            // Guarantees travelling balls draw above every tube, regardless of prefab authoring.
            if (_travelOverlay != null) _travelOverlay.SetAsLastSibling();
            _animator = new BoardMoveAnimator(_travelOverlay, gameObject);

            return UniTask.CompletedTask;
        }

        protected override UniTask OnHideAsync(CancellationToken ct)
        {
            _renderer?.Clear();

            // Dropped with the board it routed for. Left alive, it would keep answering presses
            // using the ViewModel of a show that has ended.
            _router = null;
            return UniTask.CompletedTask;
        }

        // See BoardAnimationScope.IsStale for why a re-enable outside the Show path must renew.
        private void OnEnable()
        {
            _scope ??= new BoardAnimationScope(destroyCancellationToken);
            if (_scope.IsStale) _scope.Renew();
        }

        private void OnDisable()
        {
            _scope?.Cancel();
            _isAnimating = false;
        }

        private void OnDestroy()
        {
            _scope?.Dispose();
            _scope = null;
        }

        // Input entry points, forwarding through the _router FIELD rather than being handed out as
        // _router.HandleX directly: BoardRenderer caches its tap callback for its whole lifetime and
        // BoardControlBar binds undo before OnShowAsync runs, so passing the router's own method
        // group would pin whichever router existed at bind time — a stale ViewModel forever after.
        //
        // Null-conditional because the field genuinely is null outside a show: BindViewModel wires
        // the undo button BEFORE OnShowAsync builds the router, and OnHideAsync drops it again. A
        // press in either window is meant to do nothing, not to reach a router holding a dead
        // ViewModel. (A plain class, so ?. is the right test here — not a Unity object.)
        private void HandleTap(int index) => _router?.HandleTap(index);

        private void HandleUndo() => _router?.HandleUndo();

        private void PlayMove(
            int fromIndex, int toIndex, int count, bool isForwardMove, object description)
            => RunAnimationAsync(fromIndex, toIndex, count, isForwardMove, description).Forget();

        /// <summary>
        /// The one animation path. Every caller shares the busy flag and its <c>finally</c>: a
        /// killed tween throws straight past the success path, so clearing the flag anywhere else
        /// would deadlock input permanently.
        /// </summary>
        private async UniTaskVoid RunAnimationAsync(
            int fromIndex, int toIndex, int count, bool isForwardMove, object description)
        {
            _isAnimating = true;
            SetControlsInteractable(false);
            try
            {
                await _animator.PlayAsync(
                    _renderer[fromIndex], _renderer[toIndex], count, _scope.Token);

                // Forward moves only. An undo can perfectly well leave its destination full and
                // monochrome, and celebrating a move being taken back reads as a bug.
                // Board goes null once the view hides (OnHide drops the interaction) and this runs
                // after an await, so a teardown that did not cancel must not log a spurious error.
                var board = ViewModel.Board;
                if (isForwardMove && board != null)
                    _renderer.CelebrateIfComplete(board[toIndex], toIndex);

                // Raised here, not from vm.Solved, which fires inside Tap() before the winning move
                // has landed. Cancellation reaches the catch below — v1.2.0 ShowAsync throws it.
                if (isForwardMove && _isSolved)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(CelebrationBeatDelaySeconds),
                        DelayType.UnscaledDeltaTime, cancellationToken: _scope.Token);
                    var args = new ColorStackSortWinArgs(ViewModel.Level.Value, ViewModel.MoveCount.Value);
                    await _navigator.ShowAsync<ColorStackSortWinView, ColorStackSortWinArgs>(
                        args, _scope.Token);
                }
            }
            // View hidden or destroyed mid-animation; Clear() removes anything left on the overlay.
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogError($"[BoardView] {description} failed: {ex}", this);
            }
            finally
            {
                _isAnimating = false;
                SetControlsInteractable(true);
            }
        }

        private void SetControlsInteractable(bool interactable)
        {
            if (_controlBar == null) return;

            _controlBar.SetInteractable(interactable && !_isSolved, ViewModel.CanUndo.Value);
        }
    }
}
