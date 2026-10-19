using System;
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

            // Queued, not a raw navigator show: Initialize runs before GameLifecycleManager.StartAsync,
            // and a show here would hold the navigator and get the boot transition refused, leaving the
            // menu outside the state machine. The queue waits for boot, then enters the menu as a state.
            _lifecycle.EnqueueStateChange<AircraftMainMenuState>();
        }

        public void Dispose()
        {
            _viewFactory.ResetScopeContainer(_resolver);
        }
    }
}
