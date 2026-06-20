using Cysharp.Threading.Tasks;
using R3;
using Sinkii09.UIFramework;
using VContainer;

namespace AircraftStriker
{
    public class AircraftHUDViewModel : ViewModelBase
    {
        private readonly IUINavigator       _navigator;
        private readonly AircraftHUDChannel _channel;

        // Proxy the singleton channel's ReactiveProperties so bindings survive ViewModel re-creation.
        // The channel lives for the whole session; the ViewModel is scoped to the view's lifetime.
        public ReactiveProperty<int>   Score         => _channel.Score;
        public ReactiveProperty<int>   Combo         => _channel.Combo;
        public ReactiveProperty<int>   MaxCombo      => _channel.MaxCombo;
        public ReactiveProperty<int>   Lives         => _channel.Lives;
        public ReactiveProperty<int>   MaxLives      => _channel.MaxLives;
        public ReactiveProperty<int>   ShieldCount   => _channel.ShieldCount;
        public ReactiveProperty<int>   WaveNumber    => _channel.WaveNumber;
        public ReactiveProperty<bool>  IsBossWave    => _channel.IsBossWave;
        public ReactiveProperty<float> GrazeMeter    => _channel.GrazeMeter;
        public ReactiveProperty<int>   GrazeCount    => _channel.GrazeCount;

        [Inject]
        public AircraftHUDViewModel(IUINavigator navigator, AircraftHUDChannel channel)
        {
            _navigator = navigator;
            _channel   = channel;
        }

        public void OnPausePressed() =>
            _navigator.ShowAsync<AircraftPauseView>().Forget();
    }
}
