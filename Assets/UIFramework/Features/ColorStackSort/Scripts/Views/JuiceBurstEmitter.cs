using Coffee.UIExtensions;
using UnityEngine;

namespace ColorStackSort
{
    /// <summary>
    /// One shared particle burst for the whole board, moved to wherever it is needed.
    /// <para>
    /// A plain <c>ParticleSystem</c> cannot be used here: the board's canvas is
    /// <c>ScreenSpaceOverlay</c>, which is composited after everything else, so a normal particle
    /// system would always draw <b>behind</b> the entire UI. <see cref="UIParticle"/> bakes the
    /// particle mesh through a <c>CanvasRenderer</c>, so it sorts and masks like any other graphic.
    /// </para>
    /// </summary>
    /// <remarks>
    /// The particle system must use <b>World</b> simulation space. Bursts overlap in time — one
    /// destination per move bounds how many emit at once, not how many are still alive — and in
    /// Local space, moving this emitter to the next tube would drag every live particle across the
    /// screen with it.
    /// </remarks>
    [RequireComponent(typeof(RectTransform))]
    public sealed class JuiceBurstEmitter : MonoBehaviour
    {
        [SerializeField] private UIParticle _uiParticle;
        [SerializeField] private ParticleSystem _particles;
        [SerializeField] private int _particlesPerBurst = 18;

        private void Awake()
        {
            if (_uiParticle == null) _uiParticle = GetComponent<UIParticle>();
            if (_particles == null) _particles = GetComponent<ParticleSystem>();
        }

        /// <summary>Emits one burst at a world position, tinted to match what triggered it.</summary>
        public void Burst(Vector3 worldPosition, Color tint)
        {
            if (_particles == null) return;

            // Safe precisely because the system simulates in world space: already-live particles
            // ignore the move and finish where they were emitted.
            transform.position = worldPosition;
            Emit(tint);
        }

        /// <summary>
        /// Emits without moving, for an effect authored at a fixed spot in its prefab — the win
        /// panel's confetti, rather than a burst that has to chase a tube.
        /// </summary>
        public void Burst(Color tint)
        {
            if (_particles == null) return;

            Emit(tint);
        }

        private void Emit(Color tint)
        {
            // Emit() rather than UIParticle.Play(): Play() calls Simulate(0, false, restart: true),
            // which WIPES particles still alive from an earlier burst. Emit() adds to the system and
            // leaves them to finish. EmitParams also carries the tint without mutating the shared
            // system's main module.
            var emitParams = new ParticleSystem.EmitParams
            {
                startColor = tint,
                applyShapeToPosition = true
            };
            _particles.Emit(emitParams, _particlesPerBurst);
        }

        private void OnEnable()
        {
            // A Stopped ParticleSystem is not simulated, so particles pushed in with Emit() would
            // spawn and then never move. Play() puts it into the Playing state Emit() needs and
            // spawns nothing itself — rate is 0 and there are no bursts configured.
            //
            // Note this is ParticleSystem.Play(), NOT UIParticle.Play(): the latter calls
            // Simulate(0, false, restart: true), which wipes live particles. Harmless here (nothing
            // is alive yet on enable) but it is not the call that does what is needed.
            if (_particles != null && !_particles.isPlaying) _particles.Play();

            // Clear() and Stop() both leave UIParticle.isPaused true and nothing resets it, so a
            // cleared emitter stays frozen for the rest of its life without this pairing.
            if (_uiParticle != null) _uiParticle.Resume();
        }

        private void OnDisable()
        {
            if (_uiParticle != null) _uiParticle.Clear();
        }
    }
}
