using Cysharp.Threading.Tasks;
using Sinkii09.UIFramework;
using VContainer;
using VContainer.Unity;

namespace MemoryGame
{
    // IInitializable runs before IAsyncStartable — registers game states and kicks off initial UI.
    // Parent scope's BootState is a no-op, so we trigger the first view here instead.
    public class SampleGameBootstrap : IInitializable
    {
        private readonly GameLifecycleManager _lifecycle;
        private readonly SampleGameplayState _gameplay;
        private readonly MemoryGameState _memoryGame;
        private readonly IUINavigator _navigator;

        [Inject]
        public SampleGameBootstrap(
            GameLifecycleManager lifecycle,
            SampleGameplayState gameplay,
            MemoryGameState memoryGame,
            IUINavigator navigator)
        {
            _lifecycle = lifecycle;
            _gameplay = gameplay;
            _memoryGame = memoryGame;
            _navigator = navigator;
        }

        public void Initialize()
        {
            // Register before GameLifecycleManager.StartAsync() runs.
            _lifecycle.RegisterState(_gameplay);
            _lifecycle.RegisterState(_memoryGame);

            // WinView uses IViewModel<WinArgs> so auto-registration can't handle it — register manually.
            _navigator.Register<WinView, WinViewModel, WinArgs>();

            // Parent BootState is a no-op — show the first screen here.
            // Forget() is safe: Initialize() is synchronous, the async show completes on the next frame.
            _navigator.ShowAsync<MainMenuView>().Forget();
        }
    }
}
