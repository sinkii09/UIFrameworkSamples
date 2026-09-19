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
        private readonly GameLifecycleManager _lifecycle;
        private readonly ISoundService _sound;
        private readonly SoundConfig _soundConfig;

        [Inject]
        public MainMenuViewModel(IUINavigator navigator, GameLifecycleManager lifecycle, ISoundService sound, SoundConfig soundConfig)
        {
            _navigator = navigator;
            _lifecycle = lifecycle;
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
            // GameLifecycleManager.ChangeStateAsync clears the stack first (hides MainMenu),
            // then triggers MemoryGameState.OnEnterAsync which shows GameplayView. Routes through
            // GameLifecycleManager, not UINavigator directly — IUINavigator.ChangeStateAsync was
            // removed in UIFramework v1.2.0 (GameLifecycleManager is now the sole sanctioned
            // state-transition entry point; see the framework CHANGELOG's BREAKING block).
            _lifecycle.ChangeStateAsync<MemoryGameState>().Forget();
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
