using R3;
using Sinkii09.UIFramework;
using UnityEngine;
using UnityEngine.UI;

namespace MemoryGame
{
    public class SettingsView : UIView<SettingsViewModel>
    {
        [SerializeField] private Toggle _sfxToggle;
        [SerializeField] private Toggle _musicToggle;
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _closeButton;

        public override UILayer Layer => UILayer.Popup;

        protected override void BindViewModel(SettingsViewModel vm)
        {
            _sfxToggle.BindTwoWay(vm.SfxEnabled, ref _showDisposables);
            _musicToggle.BindTwoWay(vm.MusicEnabled, ref _showDisposables);

            _saveButton.BindButton(vm.Save, ref _showDisposables);
            _closeButton.BindButton(vm.RequestClose, ref _showDisposables);
        }
    }
}
