using Cysharp.Threading.Tasks;
using R3;
using Sinkii09.UIFramework;
using VContainer;

namespace MemoryGame
{
    public class WinViewModel : ViewModelBase, IViewModel<WinArgs>
    {
        private readonly IUINavigator _navigator;
        private readonly GameLifecycleManager _lifecycle;
        private readonly ISoundService _sound;
        private readonly SoundConfig _soundConfig;

        public ReactiveProperty<string> MovesText { get; } = new();
        public ReactiveProperty<string> TimeText { get; } = new();

        [Inject]
        public WinViewModel(IUINavigator navigator, GameLifecycleManager lifecycle, ISoundService sound, SoundConfig soundConfig)
        {
            _navigator = navigator;
            _lifecycle = lifecycle;
            _sound = sound;
            _soundConfig = soundConfig;
        }

        // Called by UIViewFactory before BindViewModel — properties are populated
        // before the view subscribes, so the view gets correct values immediately.
        public void Initialize(WinArgs args)
        {
            MovesText.Value = $"Moves: {args.Moves}";
            TimeText.Value = $"Time: {UIFormatUtils.FormatTime(args.ElapsedSeconds)}";
        }

        public void OnPlayAgain()
        {
            _sound.PlaySFX(_soundConfig.ButtonClickClip);
            if (_navigator.IsTransitioning) return;
            // Same-state re-entry: the current state already IS MemoryGameState, so this must use
            // RestartCurrentStateAsync (exit -> enter same state) rather than ChangeStateAsync,
            // which would hit the state machine's same-state guard and silently no-op.
            // IUINavigator.ChangeStateAsync was removed in UIFramework v1.2.0.
            _lifecycle.RestartCurrentStateAsync().Forget();
        }

        public void OnMainMenu()
        {
            _sound.PlaySFX(_soundConfig.ButtonClickClip);
            OnMainMenuAsync().Forget();
        }

        private async UniTaskVoid OnMainMenuAsync()
        {
            // CloseAllAsync is silently dropped when _isTransitioning is true.
            // Guard here so we don't push MainMenuView onto a stack that wasn't cleared.
            if (_navigator.IsTransitioning) return;
            await _navigator.CloseAllAsync();
            _navigator.ResetState();
            await _navigator.ShowAsync<MainMenuView>();
        }
    }
}
