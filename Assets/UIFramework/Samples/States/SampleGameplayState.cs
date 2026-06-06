using System.Threading;
using Cysharp.Threading.Tasks;
using Sinkii09.UIFramework;
using VContainer;

namespace UIFrameworkSamples
{
    // Shows HUDView when gameplay starts; clears all views on exit.
    public class SampleGameplayState : IGameState
    {
        private readonly IUINavigator _navigator;

        [Inject]
        public SampleGameplayState(IUINavigator navigator) => _navigator = navigator;

        public string SceneName => null;
        public bool PausesGameTime => false;

        public async UniTask OnEnterAsync(CancellationToken ct = default)
            => await _navigator.ShowAsync<HUDView>(ct);

        public async UniTask OnExitAsync(CancellationToken ct = default)
            => await _navigator.CloseAllAsync(ct);
    }
}
