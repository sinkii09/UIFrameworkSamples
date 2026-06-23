using UnityEngine;

namespace AircraftStriker
{
    public class BulletController : PooledObject
    {
        [SerializeField] private BulletConfig _config;
        [SerializeField] private BulletVFX    _vfx;   // optional — assign after adding [BulletVFX] child

        public BulletType   BulletType { get; private set; }
        public BulletOwner  Owner      { get; private set; }

        private float              _currentSpeed;
        private float              _elapsed;
        private GameplayController _gameplay;
        private Camera             _cam;

        public void Setup(BulletOwner owner, Vector2 direction, GameplayController gameplay)
        {
            BulletType     = _config.BulletType;
            Owner          = owner;
            _gameplay      = gameplay;
            _currentSpeed  = _config.BaseSpeed;
            _elapsed       = 0f;
            transform.up   = direction;
            _cam           = Camera.main;
        }

        public override void OnGetFromPool()
        {
            _elapsed = 0f;
            _vfx?.OnPlay();
        }

        public override void OnReturnToPool() => _vfx?.OnStop();

        private void Update()
        {
            if (_config == null) return;

            _elapsed += Time.deltaTime;
            if (_elapsed >= _config.Lifetime) { ReturnToPool(); return; }

            float  halfH = _cam.orthographicSize + 2f;
            float  halfW = _cam.aspect * _cam.orthographicSize + 2f;
            Vector3 p   = transform.position;
            if (p.y < -halfH || p.y > halfH || p.x < -halfW || p.x > halfW)
            {
                ReturnToPool();
                return;
            }

            if (_config.Acceleration != 0f)
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, _config.MaxSpeed,
                                                   _config.Acceleration * Time.deltaTime);

            if (_config.AngularVelocity != 0f)
                transform.Rotate(0f, 0f, _config.AngularVelocity * Time.deltaTime);

            transform.position += transform.up * _currentSpeed * Time.deltaTime;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_config == null || _gameplay == null) return;
            Vector2 hitPos = transform.position;

            if (Owner == BulletOwner.Enemy && other.CompareTag("PlayerHitbox"))
            {
                _gameplay.OnPlayerHit(hitPos);
                ReturnToPool();
                return;
            }

            if (Owner == BulletOwner.Player)
            {
                var enemy = other.GetComponentInParent<EnemyController>();
                if (enemy != null) { _gameplay.OnEnemyHit(enemy, _config.Damage, hitPos); ReturnToPool(); return; }

                var boss = other.GetComponentInParent<BossController>();
                if (boss != null) { _gameplay.OnBossHit(boss, _config.Damage, hitPos); ReturnToPool(); }
            }
        }
    }
}
