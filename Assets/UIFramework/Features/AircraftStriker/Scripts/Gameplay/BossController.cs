using System.Collections;
using UnityEngine;

namespace AircraftStriker
{
    public class BossController : PooledObject
    {
        public bool IsAlive { get; private set; }

        private EnemyConfig        _config;
        private WaveConfig         _wave;
        private float              _currentHealth;
        private int                _currentPhase; // 0=P1, 1=P2, 2=P3
        private GameplayController _gameplay;
        private Coroutine          _fireCoroutine;

        private int CurrentPhase
        {
            get
            {
                float ratio = _currentHealth / (float)_config.MaxHealth;
                if (ratio > 0.75f) return 0;
                if (ratio > 0.50f) return 1;
                return 2;
            }
        }

        public void Setup(EnemyConfig config, WaveConfig wave,
                          GameplayController gameplay, AircraftPoolManager pool)
        {
            _config        = config;
            _wave          = wave;
            _currentHealth = config.MaxHealth;
            _currentPhase  = 0;
            _gameplay      = gameplay;
            IsAlive        = true;
            _fireCoroutine = StartCoroutine(FireRoutine());
        }

        public override void OnReturnToPool()
        {
            if (_fireCoroutine != null) { StopCoroutine(_fireCoroutine); _fireCoroutine = null; }
            IsAlive = false;
        }

        private IEnumerator FireRoutine()
        {
            while (IsAlive && (_gameplay?.IsGameActive ?? false))
            {
                BulletPatternConfig[] patterns = CurrentPhase switch
                {
                    0 => _wave.BossPhase1Patterns,
                    1 => _wave.BossPhase2Patterns,
                    _ => _wave.BossPhase3Patterns
                };

                if (patterns != null && patterns.Length > 0)
                {
                    foreach (var p in patterns)
                    {
                        if (!IsAlive) yield break;
                        _gameplay?.ExecuteEnemyPattern(transform.position, p);
                        yield return new WaitForSeconds(p.PatternInterval);
                    }
                }
                else
                {
                    yield return new WaitForSeconds(2f);
                }
            }
        }

        public void TakeDamage(float damage)
        {
            if (!IsAlive) return;
            _currentHealth -= damage;
            _gameplay?.OnBossHealthChanged(_currentHealth, _config.MaxHealth);

            int newPhase = CurrentPhase;
            if (newPhase != _currentPhase)
            {
                _currentPhase = newPhase;
                _gameplay?.OnBossPhaseChanged(_currentPhase);
            }

            if (_currentHealth <= 0f) Die();
        }

        private void Die()
        {
            IsAlive = false;
            _gameplay?.OnBossDied(this, _config);
            ReturnToPool();
        }
    }
}
