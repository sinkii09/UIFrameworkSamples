using R3;

namespace AircraftStriker
{
    // Singleton bridge: GameplayController writes, AircraftHUDViewModel reads.
    // Registered as Lifetime.Singleton so GameplayController can inject it directly
    // and UIViewFactory's child scope can resolve it for AircraftHUDViewModel.
    public class AircraftHUDChannel
    {
        public readonly ReactiveProperty<int> Score = new(0);
        public readonly ReactiveProperty<int> Combo = new(0);
        public readonly ReactiveProperty<int> MaxCombo = new(0);
        public readonly ReactiveProperty<int> Lives = new(3);
        public readonly ReactiveProperty<int> MaxLives = new(3);
        public readonly ReactiveProperty<int> ShieldCount = new(0);
        public readonly ReactiveProperty<int> WaveNumber = new(1);
        public readonly ReactiveProperty<bool> IsBossWave = new(false);
        public readonly ReactiveProperty<float> GrazeMeter = new(0f);
        public readonly ReactiveProperty<int> GrazeCount = new(0);

        public void Push(PlayerData data, int waveNumber, bool isBossWave)
        {
            Score.Value         = data.Score;
            Combo.Value         = data.Combo;
            MaxCombo.Value      = data.MaxCombo;
            Lives.Value         = data.Lives;
            MaxLives.Value      = data.MaxLives;
            ShieldCount.Value   = data.ShieldCount;
            WaveNumber.Value    = waveNumber;
            IsBossWave.Value    = isBossWave;
            GrazeMeter.Value    = data.GrazeMeter;
            GrazeCount.Value    = data.GrazeCount;
        }
    }
}
