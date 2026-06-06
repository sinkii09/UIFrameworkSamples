using Cysharp.Threading.Tasks;
using R3;
using Sinkii09.UIFramework;
using UnityEngine;
using VContainer;

namespace MemoryGame
{
    public class MainMenuViewModel : ViewModelBase
    {
        private readonly IUINavigator _navigator;

        public ReactiveProperty<string> Title { get; } = new("Main Menu");

        [Inject]
        public MainMenuViewModel(IUINavigator navigator) => _navigator = navigator;

        public void RequestPlay()
        {
            // UINavigator.ChangeStateAsync clears the stack first (hides MainMenu),
            // then triggers MemoryGameState.OnEnterAsync which shows GameplayView.
            _navigator.ChangeStateAsync<MemoryGameState>().Forget();
        }

        public void RequestSettings()
        {
            _navigator.ShowAsync<SettingsView>().Forget();
        }

        public void RequestQuit()
        {
            Debug.Log("[Sample] Quit requested");
            Application.Quit();
        }
    }
}
