using UnityEngine;

namespace AircraftStriker
{
    public class PickupController : PooledObject
    {
        private const float DriftSpeed = 2.5f;

        public PickupType PickupType { get; private set; }

        private GameplayController _gameplay;

        public void Setup(PickupType type, GameplayController gameplay)
        {
            PickupType = type;
            _gameplay  = gameplay;
        }

        private void Update()
        {
            transform.position += Vector3.down * DriftSpeed * Time.deltaTime;
            if (transform.position.y < -(Camera.main.orthographicSize + 2f))
                ReturnToPool();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_gameplay == null || !_gameplay.IsGameActive) return;
            if (other.CompareTag("PlayerHitbox"))
            {
                _gameplay.OnPickupCollected(PickupType);
                ReturnToPool();
            }
        }
    }
}
