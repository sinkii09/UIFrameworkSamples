using System;
using Cysharp.Threading.Tasks;
using Sinkii09.UIFramework;
using VContainer;
using VContainer.Unity;

namespace AircraftStriker
{
    // IDisposable is automatically honoured by RegisterEntryPoint — Dispose() fires when the scope is destroyed.
    public class AircraftGameBootstrap : IInitializable, IDisposable
    {
        private readonly GameLifecycleManager _lifecycle;
        private readonly AircraftGameplayState _gameplayState;
        private readonly AircraftMainMenuState _mainMenuState;
        private readonly IUINavigator _navigator;
        private readonly IUIViewFactory _viewFactory;
        // VContainer resolves IObjectResolver to the scope that owns this registration (AircraftLifetimeScope).
        private readonly IObjectResolver _resolver;

        [Inject]
        public AircraftGameBootstrap(
            GameLifecycleManager lifecycle,
            AircraftGameplayState gameplayState,
            AircraftMainMenuState mainMenuState,
            IUINavigator navigator,
            IUIViewFactory viewFactory,
            IObjectResolver resolver)
        {
            _lifecycle = lifecycle;
            _gameplayState = gameplayState;
            _mainMenuState = mainMenuState;
            _navigator = navigator;
            _viewFactory = viewFactory;
            _resolver = resolver;
        }

        public void Initialize()
        {
            // Push AircraftLifetimeScope's resolver into UIViewFactory so per-view child scopes
            // are created as children of this scope — making IProgressionService etc. visible
            // to ViewModels. Called here (not in Awake) because IInitializable fires after all
            // scopes in the hierarchy are fully built; Awake() is too early if parent scope is still initializing.
            _viewFactory.SetScopeContainer(_resolver);

            _lifecycle.RegisterState(_gameplayState);
            _lifecycle.RegisterState(_mainMenuState);

            _navigator.Register<AircraftGameOverView, AircraftGameOverViewModel, GameOverArgs>();
            _navigator.Register<AircraftVictoryView, AircraftVictoryViewModel, VictoryArgs>();
            _navigator.ShowAsync<AircraftMainMenuView>()
                .Forget(ex => UnityEngine.Debug.LogError($"[AircraftGameBootstrap] ShowAsync<AircraftMainMenuView> failed: {ex}"));
        }

        public void Dispose()
        {
            _viewFactory.ResetScopeContainer(_resolver);
        }
    }
}
