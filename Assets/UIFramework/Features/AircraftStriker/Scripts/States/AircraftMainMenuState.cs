using System.Threading;
using Cysharp.Threading.Tasks;
using Sinkii09.UIFramework;
using VContainer;

namespace AircraftStriker
{
    public class AircraftMainMenuState : IGameState
    {
        private readonly IUINavigator _navigator;

        [Inject]
        public AircraftMainMenuState(IUINavigator navigator) => _navigator = navigator;

        // SceneName null = no scene load; PausesGameTime false = time runs normally.
        public string SceneName    => null;
        public bool PausesGameTime => false;

        // No CloseAllAsync here: UINavigator.ChangeStateAsync clears the stack before entering a state,
        // and a CloseAllAsync issued inside that transition is refused with a warning anyway.
        public async UniTask OnEnterAsync(CancellationToken ct = default)
            => await _navigator.ShowAsync<AircraftMainMenuView>(ct);

        public UniTask OnExitAsync(CancellationToken ct = default) => UniTask.CompletedTask;
    }
}
