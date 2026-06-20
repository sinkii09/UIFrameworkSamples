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

        public async UniTask OnEnterAsync(CancellationToken ct = default)
        {
            await _navigator.CloseAllAsync(ct);
            await _navigator.ShowAsync<AircraftMainMenuView>(ct);
        }

        // OnExitAsync of the outgoing state (e.g. AircraftGameplayState) typically calls CloseAllAsync,
        // but we repeat it here to guard against entry paths where the predecessor does not clean up.
        public UniTask OnExitAsync(CancellationToken ct = default) => UniTask.CompletedTask;
    }
}
