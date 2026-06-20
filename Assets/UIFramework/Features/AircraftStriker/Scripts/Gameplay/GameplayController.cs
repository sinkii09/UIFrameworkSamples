using System;
using System.Threading;
using Random = UnityEngine.Random;
using Cysharp.Threading.Tasks;
using Sinkii09.UIFramework;
using UnityEngine;
using VContainer;

namespace AircraftStriker
{
    public class GameplayController : IDisposable
    {
        private readonly IUINavigator        _navigator;
        private readonly AircraftPoolManager  _pool;
        private readonly IProgressionService  _progression;
        private readonly WaveManager          _waveManager;
        private readonly BulletPatternExecutor _patternExecutor;
        private readonly CheckpointManager    _checkpoint;
        private readonly AircraftHUDChannel   _hud;

        private Transform  _playerTransform;
        private PlayerData _playerData;
        private int        _currentWave;
        private bool       _isBossWave;
        private bool       _isGameActive;
        private float      _invincibleUntil;
        private CancellationTokenSource _sessionCts;
        private CancellationTokenSource _gameplayCts;

        private const float InvincibleDuration = 1.5f;

        public bool IsGameActive  => _isGameActive;
        public bool IsInvincible  => Time.time < _invincibleUntil;

        [Inject]
        public GameplayController(IUINavigator navigator, AircraftPoolManager pool,
            IProgressionService progression, WaveManager waveManager,
            BulletPatternExecutor executor, CheckpointManager checkpoint,
            AircraftHUDChannel hud)
        {
            _navigator       = navigator;
            _pool            = pool;
            _progression     = progression;
            _waveManager     = waveManager;
            _patternExecutor = executor;
            _checkpoint      = checkpoint;
            _hud             = hud;
        }

        public void StartGame()
        {
            _sessionCts?.Cancel();
            _sessionCts?.Dispose();
            _sessionCts   = new CancellationTokenSource();
            _gameplayCts?.Cancel();
            _gameplayCts?.Dispose();
            _gameplayCts  = new CancellationTokenSource();
            _isGameActive    = true;
            _invincibleUntil = 0f;

            int bonus      = _progression.LoadBonusLives();
            WeaponLevel wep = _progression.LoadStartingWeapon();
            _playerData    = new PlayerData();

            if (_checkpoint.IsPendingRestore && _checkpoint.HasCheckpoint)
            {
                CheckpointState saved = _checkpoint.ConsumeRestore();
                _playerData.Reset(3 + bonus, wep);
                _playerData.RestoreFrom(saved);
                _currentWave = saved.WaveIndex;
                _isBossWave  = false;
                _waveManager.StartWaves(this, saved.WaveIndex);
            }
            else
            {
                _checkpoint.Clear();
                _playerData.Reset(3 + bonus, wep);
                _currentWave = 0;
                _isBossWave  = false;
                _waveManager.StartWaves(this, 0);
            }

            PushHUDUpdate();
        }

        public void StopGame()
        {
            _isGameActive = false;
            _sessionCts?.Cancel();
            _sessionCts?.Dispose();
            _sessionCts  = null;
            _gameplayCts?.Cancel();
            _gameplayCts?.Dispose();
            _gameplayCts = null;
            _waveManager.StopWaves();
            _pool.ReturnAll();
            _invincibleUntil = 0f;
        }

        public void Dispose()
        {
            _sessionCts?.Dispose();
            _sessionCts  = null;
            _gameplayCts?.Dispose();
            _gameplayCts = null;
        }

        public void SetPlayerTransform(Transform t) => _playerTransform = t;
        public Vector3 GetPlayerPosition()          => _playerTransform != null ? _playerTransform.position : Vector3.zero;

        public void ExecuteEnemyPattern(Vector3 origin, BulletPatternConfig config)
        {
            if (!_isGameActive) return;
            _patternExecutor.Fire(origin, config, GetPlayerPosition, _pool, this,
                _gameplayCts?.Token ?? CancellationToken.None).Forget();
        }

        public void FirePlayerBullets(Vector3 origin)
        {
            if (!_isGameActive) return;
            _patternExecutor.FirePlayer(origin, _playerData.CurrentWeapon, _pool, this);
        }

        public void OnPlayerHit()
        {
            if (!_isGameActive || IsInvincible) return;
            _playerData.TakeDamage();
            PushHUDUpdate();
            if (!_playerData.IsAlive)
            {
                _isGameActive = false;
                _gameplayCts?.Cancel();
                _waveManager.StopWaves();
                HandleGameOver().Forget();
                return;
            }
            // Grant i-frames so a burst/spread shot can only consume one life per window.
            _invincibleUntil = Time.time + InvincibleDuration;
        }

        public void OnEnemyHit(EnemyController e, float dmg) => e.TakeDamage(dmg);
        public void OnBossHit(BossController b,   float dmg) => b.TakeDamage(dmg);

        public void OnGraze()
        {
            if (!_isGameActive) return;
            _playerData.AddGraze();
            if (_playerData.TryConsumeFullGrazeMeter()) _playerData.AddScore(500);
            PushHUDUpdate();
        }

        public void OnEnemyDied(EnemyController enemy, EnemyConfig config)
        {
            _waveManager.NotifyEnemyDied();
            _playerData.AddScore(config.ScoreValue);
            _playerData.AddCoins(GameScore.ScoreToCoins(config.ScoreValue));
            PushHUDUpdate();
            TryDropPickup(enemy.transform.position, config.PickupDropChance);
        }

        public void OnBossDied(BossController boss, EnemyConfig config)
        {
            _waveManager.NotifyEnemyDied();
            _checkpoint.Clear();
            _playerData.AddScore(config.ScoreValue * 10);
            _playerData.AddCoins(GameScore.ScoreToCoins(config.ScoreValue * 10));
            PushHUDUpdate();
        }

        public void OnBossHealthChanged(float cur, float max) { }
        public void OnBossPhaseChanged(int phase) { }

        public void OnPickupCollected(PickupType type)
        {
            switch (type)
            {
                case PickupType.Health:     _playerData.RestoreLife(1);   break;
                case PickupType.Shield:     _playerData.AddShield();      break;
                case PickupType.ScoreBoost: _playerData.AddScore(200);    break;
            }
            PushHUDUpdate();
        }

        public void OnWaveStarted(int waveIndex, bool isBossWave)
        {
            _currentWave = waveIndex;
            _isBossWave  = isBossWave;
            if (isBossWave) _checkpoint.Save(waveIndex, _playerData);
            PushHUDUpdate();
        }

        public void OnAllWavesComplete()
        {
            _isGameActive = false;
            _gameplayCts?.Cancel();
            _waveManager.StopWaves();
            _checkpoint.Clear();
            ShowVictoryAsync().Forget();
        }

        private async UniTaskVoid ShowVictoryAsync()
        {
            if (_playerData == null) return;
            var ct   = _sessionCts?.Token ?? CancellationToken.None;
            int hs   = _progression.LoadHighScore();
            bool isNew = _playerData.Score > hs;
            if (isNew) _progression.SaveHighScore(_playerData.Score);
            _progression.SaveCoins(_progression.LoadCoins() + _playerData.CoinsEarned);
            await _navigator.ShowAsync<AircraftVictoryView, VictoryArgs>(new VictoryArgs
            {
                FinalScore     = _playerData.Score,
                CoinsEarned    = _playerData.CoinsEarned,
                IsNewHighScore = isNew
            }, ct);
        }

        private async UniTaskVoid HandleGameOver()
        {
            if (_playerData == null) return;
            var ct   = _sessionCts?.Token ?? CancellationToken.None;
            int hs   = _progression.LoadHighScore();
            bool isNew = _playerData.Score > hs;
            if (isNew) _progression.SaveHighScore(_playerData.Score);
            _progression.SaveCoins(_progression.LoadCoins() + _playerData.CoinsEarned);
            await _navigator.ShowAsync<AircraftGameOverView, GameOverArgs>(new GameOverArgs
            {
                FinalScore     = _playerData.Score,
                WavesReached   = _currentWave + 1,
                CoinsEarned    = _playerData.CoinsEarned,
                GrazeCount     = _playerData.GrazeCount,
                MaxCombo       = _playerData.MaxCombo,
                IsNewHighScore = isNew,
                HasCheckpoint  = _checkpoint.HasCheckpoint
            }, ct);
        }

        private void TryDropPickup(Vector3 pos, float chance)
        {
            if (Random.value > chance) return;
            PickupType type = Random.value < 0.6f ? PickupType.Health : PickupType.Shield;
            PickupController pickup = _pool.GetPickup(type);
            if (pickup == null) return;
            pickup.transform.position = pos;
            pickup.Setup(type, this);
        }

        private void PushHUDUpdate() =>
            _hud.Push(_playerData, _currentWave + 1, _isBossWave);
    }
}
