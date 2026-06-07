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
            vm.MovesText.BindToText(_movesText).AddTo(ref _showDisposables);
            vm.TimeText.BindToText(_timeText).AddTo(ref _showDisposables);

            _playAgainButton.BindButton(vm.OnPlayAgain, ref _showDisposables);
            _mainMenuButton.BindButton(vm.OnMainMenu, ref _showDisposables);
        }
    }
}
