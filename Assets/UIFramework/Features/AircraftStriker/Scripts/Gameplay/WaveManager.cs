using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace AircraftStriker
{
    // MonoBehaviour so it can SetActive for rendering; wave logic runs via UniTask (not coroutines)
    // so it is unaffected by the parent hierarchy's active state.
    public class WaveManager : MonoBehaviour
    {
        private WaveDatabase        _database;
        private AircraftPoolManager _pool;
        private GameplayController  _gameplay;
        private int                 _aliveEnemyCount;
        private CancellationTokenSource _waveCts;
        private Camera              _cam;

        private void Awake() => _cam = Camera.main;

        [Inject]
        public void Construct(WaveDatabase database, AircraftPoolManager pool)
        {
            _database = database;
            _pool     = pool;
        }

        public void StartWaves(GameplayController gameplay, int startWaveIndex = 0)
        {
            StopWaves();                          // cancel any prior run before starting a new one
            gameObject.SetActive(true);
            _gameplay = gameplay;
            _waveCts  = new CancellationTokenSource();
            RunAllWavesAsync(startWaveIndex, _waveCts.Token).Forget();
        }

        public void StopWaves()
        {
            _waveCts?.Cancel();
            _waveCts?.Dispose();
            _waveCts         = null;
            _aliveEnemyCount = 0;
            _gameplay        = null;
            gameObject.SetActive(false);
        }

        public void NotifyEnemyDied()
        {
            if (_aliveEnemyCount > 0) _aliveEnemyCount--;
        }

        private async UniTaskVoid RunAllWavesAsync(int startIndex, CancellationToken ct)
        {
            try
            {
                int i = startIndex;
                while (true)
                {
                    for (; i < _database.Waves.Length; i++)
                    {
                        WaveConfig wave = _database.Waves[i];
                        await UniTask.Delay(TimeSpan.FromSeconds(wave.PreWaveDelay), cancellationToken: ct);

                        _gameplay?.OnWaveStarted(i, wave.IsBossWave);
                        _aliveEnemyCount = 0;

                        await RunWaveAsync(wave, ct);
                        await UniTask.WaitUntil(() => _aliveEnemyCount <= 0, cancellationToken: ct);
                    }

                    if (!_database.LoopAfterFinal)
                    {
                        _gameplay?.OnAllWavesComplete();
                        break;
                    }
                    i = 0; // loop back without growing the async chain
                }
            }
            catch (OperationCanceledException) { /* expected on StopWaves() */ }
        }

        private async UniTask RunWaveAsync(WaveConfig wave, CancellationToken ct)
        {
            if (wave.IsBossWave)
            {
                // Only count 1 alive if a boss was actually spawned; 0 lets WaitUntil unblock immediately
                // rather than deadlocking forever waiting for a NotifyEnemyDied that never comes.
                _aliveEnemyCount = SpawnBoss(wave) ? 1 : 0;
                return;
            }

            foreach (var entry in wave.Enemies)
            {
                for (int j = 0; j < entry.Count; j++)
                {
                    ct.ThrowIfCancellationRequested();
                    SpawnEnemy(entry, j, entry.Count);
                    _aliveEnemyCount++;
                    if (entry.SpawnInterval > 0f)
                        await UniTask.Delay(TimeSpan.FromSeconds(entry.SpawnInterval), cancellationToken: ct);
                }
            }
        }

        private void SpawnEnemy(SpawnEntry entry, int index, int total)
        {
            EnemyController enemy = _pool.GetEnemy(entry.EnemyConfig.Type);
            if (enemy == null) return;
            float spread  = 0.8f;
            float xOffset = (index - (total - 1) * 0.5f) * spread;
            enemy.transform.position = new Vector3(xOffset, _cam.orthographicSize + 1.5f, 0f);
            Vector3 holdPos  = ViewportToWorld(entry.HoldPositionViewport);
            holdPos.x       += xOffset;
            enemy.Setup(entry.EnemyConfig, holdPos, _gameplay, _pool);
        }

        private bool SpawnBoss(WaveConfig wave)
        {
            BossController boss = _pool.GetBoss();
            if (boss == null) return false;
            boss.transform.position = new Vector3(0f, _cam.orthographicSize - 1.5f, 0f);
            boss.Setup(wave.BossConfig, wave, _gameplay, _pool);
            return true;
        }

        private Vector3 ViewportToWorld(Vector2 vp) =>
            _cam.ViewportToWorldPoint(new Vector3(vp.x, vp.y, 0f));
    }
}
