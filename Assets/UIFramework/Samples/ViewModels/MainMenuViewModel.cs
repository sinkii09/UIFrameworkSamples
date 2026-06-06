using Cysharp.Threading.Tasks;
using R3;
using Sinkii09.UIFramework;
using UnityEngine;
using VContainer;

namespace UIFrameworkSamples
{
    public class MainMenuViewModel : ViewModelBase
    {
        private readonly IUINavigator _navigator;
        private readonly GameLifecycleManager _lifecycle;
        private readonly ILoadingContext _loadingContext;

        public ReactiveProperty<string> Title { get; } = new("Main Menu");

        [Inject]
        public MainMenuViewModel(IUINavigator navigator, GameLifecycleManager lifecycle, ILoadingContext loadingContext)
        {
            _navigator = navigator;
            _lifecycle = lifecycle;
            _loadingContext = loadingContext;
        }

        public void RequestPlay()
        {
            // Set destination before entering LoadingState — LoadingState reads context on enter.
            _loadingContext.Set("GameplayScene", ct => _lifecycle.ChangeStateAsync<SampleGameplayState>(ct));
            _lifecycle.ChangeStateAsync<LoadingState>().Forget();
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
