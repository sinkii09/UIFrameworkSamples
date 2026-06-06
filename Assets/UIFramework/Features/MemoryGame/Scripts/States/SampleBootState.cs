using System.Threading;
using Cysharp.Threading.Tasks;
using Sinkii09.UIFramework;
using VContainer;

namespace MemoryGame
{
    // Overrides the framework no-op BootState to show MainMenuView at startup.
    public class SampleBootState : BootState
    {
        private readonly IUINavigator _navigator;

        [Inject]
        public SampleBootState(IUINavigator navigator) => _navigator = navigator;

        public override async UniTask OnEnterAsync(CancellationToken ct = default)
        {
            await _navigator.ShowAsync<MainMenuView>(ct);
        }
    }
}
