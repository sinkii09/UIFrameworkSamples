using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AircraftStriker
{
    // Pooled single-burst hit VFX. Placed as child of AircraftPoolManager.
    // Call Initialize(pos, color) after pool.Get() to position, tint, and play.
    // Auto-returns to pool after particle lifetime elapses.
    public class HitEffect : PooledObject
    {
        [SerializeField] private ParticleSystem _ps;

        private void Awake()
        {
            Debug.Assert(_ps != null, $"[HitEffect] _ps is null on '{name}'. Run 'Build Hit Effect Prefab' from the AircraftStriker menu.", this);
        }

        public void Initialize(Vector3 worldPos, Color color)
        {
            transform.position = worldPos;
            var main = _ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(color);
            _ps.Play();
            AutoReturnAsync().Forget();
        }

        public override void OnReturnToPool()
        {
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private async UniTaskVoid AutoReturnAsync()
        {
            var main = _ps.main;
            // constantMax is valid only when mode == TwoConstants (set by HitEffectBuilder).
            // If the particle config is changed to a curve mode, update this to use Evaluate(1f).
            Debug.Assert(main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants,
                         "[HitEffect] startLifetime must be TwoConstants mode for constantMax delay to be correct.");
            float delay = main.duration + main.startLifetime.constantMax;
            await UniTask.Delay(TimeSpan.FromSeconds(delay),
                                cancellationToken: destroyCancellationToken);
            // Guard: ReturnAll() may have already returned us (SetActive(false)).
            if (gameObject.activeSelf) ReturnToPool();
        }
    }
}
