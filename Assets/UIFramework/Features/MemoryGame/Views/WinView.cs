using R3;
using Sinkii09.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MemoryGame
{
    public class WinView : UIView<WinViewModel>
    {
        [SerializeField] private TMP_Text _movesText;
        [SerializeField] private TMP_Text _timeText;
        [SerializeField] private Button _playAgainButton;
        [SerializeField] private Button _mainMenuButton;

        public override UILayer Layer => UILayer.Popup;

        // BindViewModel is called once per view instance. Initialize(WinArgs) has already run
        // so ReactiveProperties carry correct values at subscription time.
        protected override void BindViewModel(WinViewModel vm)
        {
            vm.MovesText.Subscribe(v => _movesText.SetText(v)).AddTo(ref _showDisposables);
            vm.TimeText.Subscribe(v => _timeText.SetText(v)).AddTo(ref _showDisposables);

            _playAgainButton.onClick.AddListener(vm.OnPlayAgain);
            _mainMenuButton.onClick.AddListener(vm.OnMainMenu);
            // Track removal in _showDisposables so listeners are cleaned up on hide,
            // preventing stale delegates from invoking a disposed ViewModel if the view is pooled.
            R3.Disposable.Create(() =>
            {
                _playAgainButton.onClick.RemoveListener(vm.OnPlayAgain);
                _mainMenuButton.onClick.RemoveListener(vm.OnMainMenu);
            }).AddTo(ref _showDisposables);
        }
    }
}
