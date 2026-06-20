using Cysharp.Threading.Tasks;
using R3;
using Sinkii09.UIFramework;
using VContainer;

namespace AircraftStriker
{
    public class AircraftGameOverViewModel : ViewModelBase, IViewModel<GameOverArgs>
    {
        private readonly GameLifecycleManager _lifecycle;
        private readonly IProgressionService  _progression;
        private readonly CheckpointManager    _checkpoint;

        public ReactiveProperty<int>  FinalScore     { get; } = new(0);
        public ReactiveProperty<int>  BestScore      { get; } = new(0);
        public ReactiveProperty<int>  WavesReached   { get; } = new(0);
        public ReactiveProperty<int>  CoinsEarned    { get; } = new(0);
        public ReactiveProperty<int>  GrazeCount     { get; } = new(0);
        public ReactiveProperty<int>  MaxCombo       { get; } = new(0);
        public ReactiveProperty<bool> IsNewHighScore { get; } = new(false);
        public ReactiveProperty<bool> HasCheckpoint  { get; } = new(false);

        [Inject]
        public AircraftGameOverViewModel(
            GameLifecycleManager lifecycle,
            IProgressionService progression,
            CheckpointManager checkpoint)
        {
            _lifecycle   = lifecycle;
            _progression = progression;
            _checkpoint  = checkpoint;
        }

        public void Initialize(GameOverArgs args)
        {
            FinalScore.Value     = args.FinalScore;
            WavesReached.Value   = args.WavesReached;
            CoinsEarned.Value    = args.CoinsEarned;
            GrazeCount.Value     = args.GrazeCount;
            MaxCombo.Value       = args.MaxCombo;
            IsNewHighScore.Value = args.IsNewHighScore;
            HasCheckpoint.Value  = args.HasCheckpoint;
            // GameplayController already saved before showing this view — load for display only.
            BestScore.Value = _progression.LoadHighScore();
        }

        public void OnRetryPressed()
        {
            if (HasCheckpoint.Value)
                _checkpoint.RequestRestore();
            else
                _checkpoint.Clear();
            _lifecycle.RestartCurrentStateAsync().Forget();
        }

        public void OnMenuPressed() =>
            GoToMainMenuAsync().Forget(ex => UnityEngine.Debug.LogError($"[AircraftGameOverViewModel] GoToMainMenu failed: {ex}"));

        private async UniTask GoToMainMenuAsync()
        {
            _checkpoint.Clear();
            await _lifecycle.ChangeStateAsync<AircraftMainMenuState>();
        }
    }
}
