using UnityEngine;

namespace AircraftStriker
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float          _speed        = 8f;
        [SerializeField] private float          _fireInterval = 0.15f;
        [SerializeField] private float          _halfW        = 2.5f;
        [SerializeField] private float          _halfH        = 5f;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        private AircraftInputHandler _input;
        private GameplayController   _gameplay;
        private GrazeDetector        _grazeDetector;
        private float                _fireTimer;

        public void Initialize(AircraftInputHandler input, GameplayController gameplay)
        {
            _input         = input;
            _gameplay      = gameplay;
            _grazeDetector = new GrazeDetector();
            _fireTimer     = 0f;
        }

        private void Update()
        {
            // Blink sprite during i-frames (10 Hz). Runs outside IsGameActive guard so the
            // sprite resets to visible when the game stops.
            if (_spriteRenderer != null && _gameplay != null)
                _spriteRenderer.enabled = !_gameplay.IsInvincible || (Time.time % 0.2f < 0.1f);

            if (_input == null || _gameplay == null || !_gameplay.IsGameActive) return;

            if (_input.IsDragging)
            {
                Vector3 pos = transform.position + (Vector3)_input.WorldDeltaThisFrame;
                pos.x = Mathf.Clamp(pos.x, -_halfW, _halfW);
                pos.y = Mathf.Clamp(pos.y, -_halfH, _halfH);
                transform.position = pos;
            }

            _fireTimer -= Time.deltaTime;
            if (_fireTimer <= 0f)
            {
                _fireTimer = _fireInterval;
                _gameplay.FirePlayerBullets(transform.position);
            }
        }

        // Graze trigger: large trigger collider (r≈0.4) on this object or a child.
        // Kill hitbox (tag="PlayerHitbox") is a separate smaller trigger handled by BulletController.
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_gameplay == null || _grazeDetector == null || !_gameplay.IsGameActive) return;
            var bullet = other.GetComponent<BulletController>();
            if (bullet != null && bullet.Owner == BulletOwner.Enemy)
            {
                if (_grazeDetector.TryGraze(bullet.GetEntityId()))
                    _gameplay.OnGraze();
            }
        }
    }
}
