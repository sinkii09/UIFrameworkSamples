using Cysharp.Threading.Tasks;
using R3;
using Sinkii09.UIFramework;
using VContainer;

namespace AircraftStriker
{
    public class AircraftVictoryViewModel : ViewModelBase, IViewModel<VictoryArgs>
    {
        private readonly GameLifecycleManager _lifecycle;
        private readonly IProgressionService _progression;

        public ReactiveProperty<int> FinalScore { get; } = new(0);
        public ReactiveProperty<int> BestScore { get; } = new(0);
        public ReactiveProperty<int> CoinsEarned { get; } = new(0);
        public ReactiveProperty<bool> IsNewHighScore { get; } = new(false);

        [Inject]
        public AircraftVictoryViewModel(
            GameLifecycleManager lifecycle,
            IProgressionService progression)
        {
            _lifecycle   = lifecycle;
            _progression = progression;
        }

        public void Initialize(VictoryArgs args)
        {
            FinalScore.Value     = args.FinalScore;
            CoinsEarned.Value    = args.CoinsEarned;
            IsNewHighScore.Value = args.IsNewHighScore;
            // GameplayController already saved before showing this view — load for display only.
            BestScore.Value = _progression.LoadHighScore();
        }

        public void OnPlayAgainPressed() =>
            _lifecycle.RestartCurrentStateAsync().Forget();

        public void OnMenuPressed() =>
            _lifecycle.ChangeStateAsync<AircraftMainMenuState>()
                .Forget(ex => UnityEngine.Debug.LogError($"[AircraftVictoryViewModel] GoToMainMenu failed: {ex}"));
    }
}
