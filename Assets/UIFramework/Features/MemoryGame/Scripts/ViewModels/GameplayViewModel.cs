using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Sinkii09.UIFramework;
using UnityEngine;
using VContainer;

namespace MemoryGame
{
    public class GameplayViewModel : ViewModelBase
    {
        public ReactiveProperty<int> Moves { get; } = new(0);
        public ReactiveProperty<int> MatchesFound { get; } = new(0);
        public ReactiveProperty<string> TimerText { get; } = new("0:00");

        // VM → View signals; added to _disposables so active R3 subscriptions
        // are released when VContainer disposes the ViewModel's child scope.
        public Subject<int> FlipToFront { get; } = new();
        public Subject<(int Id1, int Id2)> FlipToBack { get; } = new();
        public Subject<(int Id1, int Id2)> MatchConfirmed { get; } = new();
        public Subject<WinArgs> GameWon { get; } = new();

        private readonly ISoundService _sound;
        private readonly SoundConfig _soundConfig;

        private MemoryCardGame _game;
        private float _startTime;
        private CancellationTokenSource _timerCts;

        [Inject]
        public GameplayViewModel(ISoundService sound, SoundConfig soundConfig)
        {
            _sound = sound;
            _soundConfig = soundConfig;
            FlipToFront.AddTo(ref _disposables);
            FlipToBack.AddTo(ref _disposables);
            MatchConfirmed.AddTo(ref _disposables);
            GameWon.AddTo(ref _disposables);
        }

        public override void OnShow()
        {
            _game = new MemoryCardGame();
            _game.Initialize(8);
            _game.OnMatchFound += HandleMatchFound;
            _game.OnMismatch += HandleMismatch;
            _game.OnGameComplete += HandleGameComplete;

            Moves.Value = 0;
            MatchesFound.Value = 0;

            _timerCts = new CancellationTokenSource();
            _startTime = Time.time;
            RunTimerAsync(_timerCts.Token).Forget();

            _sound.PlayMusic(_soundConfig.BackgroundMusicClip);
        }

        public override void OnHide()
        {
            _sound.StopMusic();

            // Cancel timer before nulling _game so DelayedFlipBackAsync's guard fires correctly.
            _timerCts?.Cancel();
            _timerCts?.Dispose();
            _timerCts = null;

            if (_game != null)
            {
                _game.OnMatchFound -= HandleMatchFound;
                _game.OnMismatch -= HandleMismatch;
                _game.OnGameComplete -= HandleGameComplete;
                _game = null;
            }

            base.OnHide();
        }

        public System.Collections.Generic.IReadOnlyList<CardData> GetInitialCards() => _game.Cards;

        public void HandleCardClick(int cardId)
        {
            var result = _game.TryFlip(cardId);
            if (result == FlipResult.NeedSecond ||
                result == FlipResult.Match ||
                result == FlipResult.Mismatch)
            {
                FlipToFront.OnNext(cardId);
                _sound.PlaySFX(_soundConfig.CardFlipClip);
            }
        }

        private void HandleMatchFound(int id1, int id2)
        {
            Moves.Value = _game.Moves;
            MatchesFound.Value = _game.MatchesFound;
            MatchConfirmed.OnNext((id1, id2));
            _sound.PlaySFX(_soundConfig.MatchClip);
        }

        private void HandleMismatch(int id1, int id2)
        {
            Moves.Value = _game.Moves;
            _sound.PlaySFX(_soundConfig.MismatchClip);
            DelayedFlipBackAsync(id1, id2, _timerCts?.Token ?? default).Forget();
        }

        private void HandleGameComplete()
        {
            _timerCts?.Cancel();
            _sound.PlaySFX(_soundConfig.WinClip);
            _sound.StopMusic();
            float elapsed = Time.time - _startTime;
            GameWon.OnNext(new WinArgs { Moves = _game.Moves, ElapsedSeconds = elapsed });
        }

        private async UniTaskVoid DelayedFlipBackAsync(int id1, int id2, CancellationToken ct)
        {
            await UniTask.Delay(800, cancellationToken: ct).SuppressCancellationThrow();
            if (ct.IsCancellationRequested || _game == null) return;
            _game.ResolveMismatch(id1, id2);
            FlipToBack.OnNext((id1, id2));
        }

        private async UniTaskVoid RunTimerAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                float elapsed = Time.time - _startTime;
                int minutes = (int)(elapsed / 60f);
                int seconds = (int)(elapsed % 60f);
                TimerText.Value = $"{minutes}:{seconds:D2}";
                await UniTask.Delay(100, cancellationToken: ct).SuppressCancellationThrow();
            }
        }
    }
}
