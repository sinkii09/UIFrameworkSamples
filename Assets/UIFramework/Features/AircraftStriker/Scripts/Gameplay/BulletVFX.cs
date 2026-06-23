using UnityEngine;

namespace AircraftStriker
{
    // Manages multiple particle system layers on a bullet prefab.
    // Add this component to a child GameObject "[BulletVFX]" under the bullet root.
    // Assign each ParticleSystem layer (Core, Trail, Glow) to _layers in the Inspector.
    // BulletController calls OnPlay/OnStop at pool get/release respectively.
    //
    // Prefab hierarchy:
    //   BulletController (root)
    //     [BulletVFX]   ← this component
    //       Core        ← ParticleSystem
    //       Trail       ← ParticleSystem
    //       Glow        ← ParticleSystem (optional)
    //
    // IMPORTANT: Set Play On Awake = OFF on every particle system in _layers.
    //            This component controls all playback.
    public class BulletVFX : MonoBehaviour
    {
        [SerializeField] private ParticleSystem[] _layers;

        public void OnPlay()
        {
            foreach (var ps in _layers)
            {
                if (ps == null) continue;
                ps.Clear(true);
                ps.Play(true);
            }
        }

        public void OnStop()
        {
            foreach (var ps in _layers)
            {
                if (ps == null) continue;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_layers == null || _layers.Length == 0)
                Debug.LogWarning($"[BulletVFX] No particle layers assigned on '{gameObject.name}'.", this);
        }
#endif
    }
}
