using Cysharp.Threading.Tasks;
using R3;
using Sinkii09.UIFramework;
using UnityEngine;
using VContainer;

namespace MemoryGame
{
    public class MainMenuViewModel : ViewModelBase
    {
        private readonly IUINavigator _navigator;
        private readonly ISoundService _sound;
        private readonly SoundConfig _soundConfig;

        public ReactiveProperty<string> Title { get; } = new("Main Menu");

        [Inject]
        public MainMenuViewModel(IUINavigator navigator, ISoundService sound, SoundConfig soundConfig)
        {
            _navigator = navigator;
            _sound = sound;
            _soundConfig = soundConfig;
        }

        public override void OnShow()
        {
            _sound.PlayMusic(_soundConfig.MainMenuMusicClip);
        }

        protected override void OnHide()
        {
            _sound.StopMusic();
        }

        public void RequestPlay()
        {
            _sound.PlaySFX(_soundConfig.ButtonClickClip);
            // UINavigator.ChangeStateAsync clears the stack first (hides MainMenu),
            // then triggers MemoryGameState.OnEnterAsync which shows GameplayView.
            _navigator.ChangeStateAsync<MemoryGameState>().Forget();
        }

        public void RequestSettings()
        {
            _sound.PlaySFX(_soundConfig.ButtonClickClip);
            _navigator.ShowAsync<SettingsView>().Forget();
        }

        public void RequestQuit()
        {
            _sound.PlaySFX(_soundConfig.ButtonClickClip);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
