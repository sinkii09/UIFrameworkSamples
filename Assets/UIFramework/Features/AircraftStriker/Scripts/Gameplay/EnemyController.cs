using System.Collections;
using UnityEngine;

namespace AircraftStriker
{
    public class EnemyController : PooledObject
    {
        public EnemyType EnemyType { get; private set; }
        public bool      IsAlive   { get; private set; }

        private EnemyConfig       _config;
        private float             _currentHealth;
        private Vector3           _holdPosition;
        private GameplayController _gameplay;
        private AircraftPoolManager _pool;
        private bool              _isHolding;
        private Coroutine         _fireCoroutine;

        public void Setup(EnemyConfig config, Vector3 holdPosition,
                          GameplayController gameplay, AircraftPoolManager pool)
        {
            _config        = config;
            EnemyType      = config.Type;
            _currentHealth = config.MaxHealth;
            _holdPosition  = holdPosition;
            _gameplay      = gameplay;
            _pool          = pool;
            IsAlive        = true;
            _isHolding     = false;
        }

        public override void OnReturnToPool()
        {
            if (_fireCoroutine != null) { StopCoroutine(_fireCoroutine); _fireCoroutine = null; }
            IsAlive = false;
        }

        private void Update()
        {
            if (!IsAlive || _config == null || (_gameplay != null && !_gameplay.IsGameActive)) return;

            if (_isHolding) return;

            transform.position = Vector3.MoveTowards(
                transform.position, _holdPosition, _config.MoveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, _holdPosition) < 0.05f)
            {
                transform.position = _holdPosition;
                _isHolding         = true;
                if (_config.FirePattern != null)
                    _fireCoroutine = StartCoroutine(FireRoutine());
            }
        }

        private IEnumerator FireRoutine()
        {
            while (IsAlive && (_gameplay?.IsGameActive ?? false))
            {
                yield return new WaitForSeconds(_config.FirePattern.PatternInterval);
                if (IsAlive)
                    _gameplay?.ExecuteEnemyPattern(transform.position, _config.FirePattern);
            }
        }

        public void TakeDamage(float damage)
        {
            if (!IsAlive) return;
            _currentHealth -= damage;
            if (_currentHealth <= 0f) Die();
        }

        private void Die()
        {
            IsAlive = false;
            _gameplay?.OnEnemyDied(this, _config);
            ReturnToPool();
        }
    }
}
