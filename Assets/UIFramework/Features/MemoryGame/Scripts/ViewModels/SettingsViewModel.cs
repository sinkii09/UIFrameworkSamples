using Cysharp.Threading.Tasks;
using R3;
using Sinkii09.UIFramework;
using UnityEngine;
using VContainer;

namespace MemoryGame
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly IUINavigator _navigator;
        private readonly ISoundService _sound;
        private readonly SoundConfig _soundConfig;

        public ReactiveProperty<bool> SfxEnabled { get; } = new(true);
        public ReactiveProperty<bool> MusicEnabled { get; } = new(true);

        [Inject]
        public SettingsViewModel(IUINavigator navigator, ISoundService sound, SoundConfig soundConfig)
        {
            _navigator = navigator;
            _sound = sound;
            _soundConfig = soundConfig;
        }

        public override void OnShow()
        {
            SfxEnabled.Value = PlayerPrefs.GetInt("SfxEnabled", 1) == 1;
            MusicEnabled.Value = PlayerPrefs.GetInt("MusicEnabled", 1) == 1;

            // Skip(1) avoids the immediate-emit that fires on subscribe — SoundManager already
            // holds the correct enabled state from Awake()/prior SetMusicEnabled calls.
            // Without Skip(1), opening settings calls SetMusicEnabled(true) and restarts music.
            SfxEnabled.Skip(1).Subscribe(v =>
            {
                PlayerPrefs.SetInt("SfxEnabled", v ? 1 : 0);
                PlayerPrefs.Save();
                _sound.SetSFXEnabled(v);
            }).AddTo(ref _showDisposables);

            MusicEnabled.Skip(1).Subscribe(v =>
            {
                PlayerPrefs.SetInt("MusicEnabled", v ? 1 : 0);
                PlayerPrefs.Save();
                _sound.SetMusicEnabled(v);
            }).AddTo(ref _showDisposables);
        }

        public void RequestClose()
        {
            _sound.PlaySFX(_soundConfig.ButtonClickClip);
            _navigator.PopAsync().Forget();
        }
    }
}
