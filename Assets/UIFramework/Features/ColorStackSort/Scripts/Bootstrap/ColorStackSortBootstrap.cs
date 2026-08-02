using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sinkii09.UIFramework;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ColorStackSort
{
    /// <summary>
    /// Registers the feature's state and view bindings, then enters gameplay.
    /// <para>
    /// Split across both entry-point interfaces on purpose: <see cref="Initialize"/> must complete
    /// its registrations before anything can transition, and VContainer runs every
    /// <c>IInitializable</c> before any <c>IAsyncStartable</c>.
    /// </para>
    /// <remarks>
    /// [Preserve]: only VContainer's reflection ever constructs this, so IL2CPP's linker has no
    /// static call site to keep it alive.
    /// </remarks>
    // Fully qualified: VContainer ships its own PreserveAttribute, so the short name is ambiguous
    // in any file that also uses VContainer.
    [UnityEngine.Scripting.Preserve]
    public sealed class ColorStackSortBootstrap : IInitializable, IAsyncStartable
    {
        /// <summary>How long boot may take before we assume it faulted and proceed regardless.</summary>
        private const double BootTimeoutSeconds = 5d;

        private readonly GameLifecycleManager _lifecycle;

        // Concrete type, never IGameState. UIStateMachine keys its registry on typeof(T) and
        // GameLifecycleManager.RegisterState<T> forwards T unchanged, so an interface-typed field
        // would register this under typeof(IGameState) and every
        // ChangeStateAsync<ColorStackSortGameplayState> would throw "State not registered".
        private readonly ColorStackSortGameplayState _gameplayState;

        private readonly IUINavigator _navigator;
        private readonly IUIStateMachine _stateMachine;
        private readonly LevelProgressService _progress;

        [Inject]
        public ColorStackSortBootstrap(
            GameLifecycleManager lifecycle,
            ColorStackSortGameplayState gameplayState,
            IUINavigator navigator,
            IUIStateMachine stateMachine,
            LevelProgressService progress)
        {
            _lifecycle = lifecycle;
            _gameplayState = gameplayState;
            _navigator = navigator;
            _stateMachine = stateMachine;
            _progress = progress;
        }

        public void Initialize()
        {
            _lifecycle.RegisterState(_gameplayState);

            // Both ViewModels implement IViewModel<TArgs>. UIViewRegistry.AutoRegister only builds
            // the key -> (view, viewModel) map; the args-carrying navigator overload has to be
            // registered by hand or ShowAsync<TView, TArgs> cannot resolve it.
            _navigator.Register<BoardView, BoardViewModel, BoardArgs>();
            _navigator.Register<ColorStackSortWinView, ColorStackSortWinViewModel, ColorStackSortWinArgs>();
        }

        public async UniTask StartAsync(CancellationToken ct)
        {
            try
            {
                await WaitForBootAsync(ct);

                // Before the state change, not after: ColorStackSortGameplayState.OnEnterAsync reads
                // the current level to build its board, so loading later would show level 1 for one
                // board and then jump. LoadAsync never throws anything but cancellation, which is
                // load-bearing here — this catch only handles OperationCanceledException, so any
                // other escaping exception would skip the state change and leave a blank screen.
                await _progress.LoadAsync(ct);

                await _lifecycle.ChangeStateAsync<ColorStackSortGameplayState>(ct);
            }
            catch (OperationCanceledException)
            {
                // Scope torn down mid-boot (play-mode exit, domain reload). Nothing to clean up.
            }
        }

        /// <summary>
        /// Waits until the boot transition has finished.
        /// <see cref="GameLifecycleManager.ChangeStateAsync{T}"/> silently drops any call made
        /// while a transition is in flight, so firing blind would mean the board sometimes never
        /// appears at all.
        /// <para>
        /// Today this is a no-op: <c>BootState.OnEnterAsync</c> returns a completed task, so
        /// <c>StartAsync</c> finishes synchronously before this ever runs. It is here for the day
        /// boot becomes genuinely async — a splash screen, a remote config fetch, a catalog load.
        /// <b>Do not delete it as dead code.</b>
        /// </para>
        /// </summary>
        private async UniTask WaitForBootAsync(CancellationToken ct)
        {
            var deadline = Time.realtimeSinceStartupAsDouble + BootTimeoutSeconds;

            // CurrentState stays null until the first transition succeeds, and is assigned with no
            // await before _isTransitioning clears — so this can never observe a half-done boot.
            while (_stateMachine.CurrentState == null)
            {
                if (Time.realtimeSinceStartupAsDouble >= deadline)
                {
                    // A faulted boot leaves CurrentState null forever, so waiting longer can only
                    // produce a permanent blank screen. Push on instead: UIStateMachine handles a
                    // null previous state fine, so the board still has a chance to appear.
                    Debug.LogError(
                        $"[ColorStackSortBootstrap] Boot state not entered after {BootTimeoutSeconds:0}s — " +
                        "the boot transition most likely threw. Entering gameplay anyway.");
                    return;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }
    }
}
