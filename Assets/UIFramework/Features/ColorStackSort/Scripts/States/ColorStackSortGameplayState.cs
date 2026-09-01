using System.Threading;
using Cysharp.Threading.Tasks;
using Sinkii09.UIFramework;
using VContainer;

namespace ColorStackSort
{
    /// <summary>
    /// Builds the current level and shows the board. Re-entering this state is the feature's only
    /// board-rebuild path — both Restart and Next go through
    /// <see cref="GameLifecycleManager.RestartCurrentStateAsync"/>, which exits and re-enters here.
    /// </summary>
    /// <remarks>
    /// [Preserve]: only VContainer's reflection ever constructs this, so IL2CPP's linker has no
    /// static call site to keep it alive.
    /// </remarks>
    // Fully qualified: VContainer ships its own PreserveAttribute, so the short name is ambiguous
    // in any file that also uses VContainer.
    [UnityEngine.Scripting.Preserve]
    public sealed class ColorStackSortGameplayState : IGameState
    {
        private readonly IUINavigator _navigator;
        private readonly ColorStackSortSettings _settings;
        private readonly LevelProgressService _progress;

        [Inject]
        public ColorStackSortGameplayState(
            IUINavigator navigator, ColorStackSortSettings settings, LevelProgressService progress)
        {
            _navigator = navigator;
            _settings = settings;
            _progress = progress;
        }

        /// <summary>Single scene — nothing to load additively.</summary>
        public string SceneName => null;

        /// <summary>Board motion is tween-driven, so nothing here depends on Time.timeScale.</summary>
        public bool PausesGameTime => false;

        public async UniTask OnEnterAsync(CancellationToken ct = default)
        {
            // Read per entry, not per construction: Next advances the service and then re-enters,
            // so this is what makes the new level take effect.
            var level = _progress.Current;
            var args = new BoardArgs(_settings.ParamsForLevel(level), _settings.SeedForLevel(level), level);

            // Deliberately unguarded. A failure here is a real one — a missing prefab or a bad
            // settings asset — and swallowing it would leave a blank screen with no explanation.
            // Only decorative work (overlays, sound) earns a catch in a state's enter path.
            await _navigator.ShowAsync<BoardView, BoardArgs>(args, ct);
        }

        /// <summary>
        /// Nothing to clean up. There is deliberately no <c>CloseAllAsync</c> here: as of
        /// UIFramework v1.2.0 the stack is already cleared on both paths that reach this method —
        /// <c>UINavigator.ChangeStateAsync</c> clears before entering the state machine, and
        /// <c>GameLifecycleManager.RestartCurrentStateAsync</c> calls <c>CloseAllAsync</c> itself
        /// immediately after this returns. On the first of those it would additionally be a no-op
        /// that logs a warning, since the navigator still holds <c>_isTransitioning</c>.
        /// </summary>
        public UniTask OnExitAsync(CancellationToken ct = default) => UniTask.CompletedTask;
    }
}
