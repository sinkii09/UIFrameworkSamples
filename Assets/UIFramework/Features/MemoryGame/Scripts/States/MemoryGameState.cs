using System.Threading;
using Cysharp.Threading.Tasks;
using Sinkii09.UIFramework;
using VContainer;

namespace MemoryGame
{
    public class MemoryGameState : IGameState
    {
        private readonly IUINavigator _navigator;

        [Inject]
        public MemoryGameState(IUINavigator navigator) => _navigator = navigator;

        public string SceneName => null;
        public bool PausesGameTime => false;

        // Awaited rather than returned directly: the navigator now reports an outcome
        // (UniTask<NavigationResult>), which does not convert to the UniTask this interface returns.
        public async UniTask OnEnterAsync(CancellationToken ct = default)
            => await _navigator.ShowAsync<GameplayView>(ct);

        public async UniTask OnExitAsync(CancellationToken ct = default)
            => await _navigator.CloseAllAsync(ct);
    }
}
