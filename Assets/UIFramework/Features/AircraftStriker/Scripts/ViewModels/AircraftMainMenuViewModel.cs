using Cysharp.Threading.Tasks;
using R3;
using Sinkii09.UIFramework;
using VContainer;

namespace AircraftStriker
{
    public class AircraftMainMenuViewModel : ViewModelBase
    {
        private readonly IUINavigator _navigator;
        private readonly GameLifecycleManager _lifecycle;
        private readonly IProgressionService _progression;
        private readonly IAircraftSoundService _sound;

        public ReactiveProperty<int> BestScore { get; } = new(0);

        [Inject]
        public AircraftMainMenuViewModel(
            IUINavigator navigator,
            GameLifecycleManager lifecycle,
            IProgressionService progression,
            IAircraftSoundService sound)
        {
            _navigator = navigator;
            _lifecycle = lifecycle;
            _progression = progression;
            _sound = sound;
        }

        public override void OnShow()
        {
            BestScore.Value = _progression.LoadHighScore();
        }

        public void OnPlayPressed()
        {
            _sound.PlaySFX(SFXType.UIClick);
            _lifecycle.ChangeStateAsync<AircraftGameplayState>().Forget();
        }

        public void OnShopPressed()
        {
            _sound.PlaySFX(SFXType.UIClick);
            _navigator.ShowAsync<ShopView>().Forget();
        }
    }
}
