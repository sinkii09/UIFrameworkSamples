using Cysharp.Threading.Tasks;
using Sinkii09.UIFramework;
using UnityEngine;
using VContainer;

namespace AircraftStriker
{
    public class AircraftPauseViewModel : ViewModelBase
    {
        private readonly IUINavigator _navigator;
        private readonly GameLifecycleManager _lifecycle;

        [Inject]
        public AircraftPauseViewModel(IUINavigator navigator, GameLifecycleManager lifecycle)
        {
            _navigator = navigator;
            _lifecycle = lifecycle;
        }

        // Restore timeScale BEFORE the async call so the hide animation plays at normal speed.
        // AircraftPauseView.OnShowAsync sets timeScale=0 after the entrance animation.
        public void OnResumePressed()
        {
            Time.timeScale = 1f;
            _navigator.PopAsync()
                .Forget(ex => Debug.LogError($"[AircraftPauseViewModel] Resume failed: {ex}"));
        }

        public void OnRestartPressed()
        {
            Time.timeScale = 1f;
            _lifecycle.RestartCurrentStateAsync()
                .Forget(ex => Debug.LogError($"[AircraftPauseViewModel] Restart failed: {ex}"));
        }

        public void OnMenuPressed() =>
            GoToMainMenuAsync()
                .Forget(ex => Debug.LogError($"[AircraftPauseViewModel] GoToMainMenu failed: {ex}"));

        private async UniTask GoToMainMenuAsync()
        {
            Time.timeScale = 1f;
            await _lifecycle.ChangeStateAsync<AircraftMainMenuState>();
        }
    }
}
