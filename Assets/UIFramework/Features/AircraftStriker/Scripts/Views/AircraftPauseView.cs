using System.Threading;
using Cysharp.Threading.Tasks;
using Sinkii09.UIFramework;
using UnityEngine;
using UnityEngine.UI;

namespace AircraftStriker
{
    [UIViewKey("AircraftStriker/AircraftPauseView")]
    public class AircraftPauseView : UIView<AircraftPauseViewModel>
    {
        public override UILayer Layer => UILayer.Overlay;

        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _menuButton;

        protected override void BindViewModel(AircraftPauseViewModel vm)
        {
            _resumeButton.BindButton(vm.OnResumePressed, ref _showDisposables);
            _restartButton.BindButton(vm.OnRestartPressed, ref _showDisposables);
            _menuButton.BindButton(vm.OnMenuPressed, ref _showDisposables);
        }

        // Freeze game time AFTER the entrance animation so the slide-in plays at normal speed.
        // Time is restored by AircraftPauseViewModel before any exit action so hide animations
        // run at normal speed regardless of which button (resume/restart/menu) the player presses.
        protected override UniTask OnShowAsync(CancellationToken ct)
        {
            Time.timeScale = 0f;
            return UniTask.CompletedTask;
        }
    }
}
