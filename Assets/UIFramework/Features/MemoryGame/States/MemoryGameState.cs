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

        public UniTask OnEnterAsync(CancellationToken ct = default)
            => _navigator.ShowAsync<GameplayView>(ct);

        public UniTask OnExitAsync(CancellationToken ct = default)
            => _navigator.CloseAllAsync(ct);
    }
}
