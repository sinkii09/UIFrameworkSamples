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

        public ReactiveProperty<bool> SfxEnabled { get; } = new(true);
        public ReactiveProperty<bool> MusicEnabled { get; } = new(true);

        [Inject]
        public SettingsViewModel(IUINavigator navigator) => _navigator = navigator;

        public override void OnShow()
        {
            SfxEnabled.Value = PlayerPrefs.GetInt("SfxEnabled", 1) == 1;
            MusicEnabled.Value = PlayerPrefs.GetInt("MusicEnabled", 1) == 1;
        }

        public void Save()
        {
            PlayerPrefs.SetInt("SfxEnabled", SfxEnabled.Value ? 1 : 0);
            PlayerPrefs.SetInt("MusicEnabled", MusicEnabled.Value ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log($"[Sample] Settings saved — SFX: {SfxEnabled.Value}, Music: {MusicEnabled.Value}");
        }

        public void RequestClose() => _navigator.PopAsync().Forget();
    }
}
