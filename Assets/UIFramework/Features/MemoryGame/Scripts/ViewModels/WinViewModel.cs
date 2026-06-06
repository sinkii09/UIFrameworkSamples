using Cysharp.Threading.Tasks;
using R3;
using Sinkii09.UIFramework;
using VContainer;

namespace MemoryGame
{
    public class WinViewModel : ViewModelBase, IViewModel<WinArgs>
    {
        private readonly IUINavigator _navigator;
        private readonly ISoundService _sound;
        private readonly SoundConfig _soundConfig;

        public ReactiveProperty<string> MovesText { get; } = new();
        public ReactiveProperty<string> TimeText { get; } = new();

        [Inject]
        public WinViewModel(IUINavigator navigator, ISoundService sound, SoundConfig soundConfig)
        {
            _navigator = navigator;
            _sound = sound;
            _soundConfig = soundConfig;
        }

        // Called by UIViewFactory before BindViewModel — properties are populated
        // before the view subscribes, so the view gets correct values immediately.
        public void Initialize(WinArgs args)
        {
            int m = (int)(args.ElapsedSeconds / 60f);
            int s = (int)(args.ElapsedSeconds % 60f);
            MovesText.Value = $"Moves: {args.Moves}";
            TimeText.Value = $"Time: {m}:{s:D2}";
        }

        // UINavigator.ChangeStateAsync clears the stack first (removes WinView),
        // then MemoryGameState.OnEnterAsync shows GameplayView fresh.
        public void OnPlayAgain()
        {
            _sound.PlaySFX(_soundConfig.ButtonClickClip);
            _navigator.ChangeStateAsync<MemoryGameState>().Forget();
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
            await _navigator.ShowAsync<MainMenuView>();
        }
    }
}
