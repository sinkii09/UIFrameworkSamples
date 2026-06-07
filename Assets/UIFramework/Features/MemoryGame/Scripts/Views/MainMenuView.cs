using R3;
using Sinkii09.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MemoryGame
{
    public class MainMenuView : UIView<MainMenuViewModel>
    {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;

        public override UILayer Layer => UILayer.Screen;

        protected override void BindViewModel(MainMenuViewModel vm)
        {
            vm.Title.BindToText(_titleText).AddTo(ref _showDisposables);

            _playButton.BindButton(vm.RequestPlay, ref _showDisposables);
            _settingsButton.BindButton(vm.RequestSettings, ref _showDisposables);
            _quitButton.BindButton(vm.RequestQuit, ref _showDisposables);
        }
    }
}
