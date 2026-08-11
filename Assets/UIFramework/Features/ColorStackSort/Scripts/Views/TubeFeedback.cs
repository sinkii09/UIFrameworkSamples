using Coffee.UIEffects;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ColorStackSort
{
    /// <summary>
    /// Presentation-only feedback for one tube: the shiny sweep when it is completed, and the red
    /// flash when a tap onto it is rejected.
    /// <para>
    /// Split out of <see cref="TubeView"/> rather than added to it — that class is at the file-size
    /// limit and owns the board-facing contract (slots, pop, attach). Keeping the juice packages'
    /// types out of it stops UIEffect leaking into the class the board logic talks to.
    /// </para>
    /// </summary>
    public sealed class TubeFeedback : MonoBehaviour
    {
        private const float FlashDuration = 0.12f;
        private const float SweepBodyAlpha = 0.7f;
        private static readonly Color FlashTint = new Color(1f, 0.35f, 0.35f, 0.5f);

        [SerializeField] private Image _body;
        [SerializeField] private UIEffectTweener _sweep;

        // Not a serialized field: UIEffect and TubeFeedback are always added to the same
        // GameObject by ColorStackSortJuicePrefabParts.AddTubeFeedback, so fetching it is simpler
        // than another builder-wired reference. Component add order doesn't matter — only the
        // reference is captured here, nothing on it is read or written until PlayComplete runs.
        private UIEffect _effect;

        /// <summary>
        /// Captured once, at Awake. Reading the current colour at flash time would sample a flash
        /// already in flight and write that half-faded value back as the "base" — permanently. This
        /// is the same trap <see cref="TubeView.Shake"/> documents for its own restore.
        /// </summary>
        /// <remarks>
        /// Safe to capture this early because nothing re-tints a tube body per level:
        /// <c>BoardRenderer.Build</c> only calls <c>Initialize</c> and tints the balls.
        /// </remarks>
        private Color _bodyBaseTint;

        private void Awake()
        {
            if (_body != null) _bodyBaseTint = _body.color;
            _effect = GetComponent<UIEffect>();
        }

        /// <summary>
        /// Sweeps the tube once, colour-matched to <paramref name="tint"/>. Fire-and-forget;
        /// nothing awaits it.
        /// </summary>
        /// <remarks>
        /// Also flashes the body panel's alpha up for the sweep's duration. Without this the sweep
        /// is rendered through the body's resting 13% alpha and is functionally invisible — the
        /// panel needs something opaque enough to actually show a highlight on.
        /// </remarks>
        public void PlayComplete(Color tint)
        {
            if (_effect != null) _effect.transitionColor = tint;

            if (_body != null)
            {
                // Kill and restore together, same idiom as PlayRejected below: two completions
                // inside one flash would otherwise stack tweens on this Image.
                RestoreBody();

                var boosted = _bodyBaseTint;
                boosted.a = SweepBodyAlpha;
                var sweepDuration = _sweep != null ? _sweep.duration : FlashDuration;
                _body.DOColor(boosted, sweepDuration / 2f)
                    .SetLoops(2, LoopType.Yoyo)
                    .SetLink(gameObject);
            }

            // resetTime: true — a tube completed twice in one level must sweep from the start again,
            // not resume from wherever the previous sweep was left.
            if (_sweep != null) _sweep.PlayForward(true);
        }

        public void PlayRejected()
        {
            if (_body == null) return;

            // Kill and restore together, never kill alone: two rejected taps inside one flash would
            // otherwise stack two colour tweens, and the second would capture the first's mid-flash
            // colour as its start.
            RestoreBody();

            _body.DOColor(FlashTint, FlashDuration)
                .SetLoops(2, LoopType.Yoyo)
                .SetLink(gameObject);
        }

        // SetLink defaults to KillOnDestroy, NOT kill-on-disable, so a hidden-and-reshown board would
        // otherwise resume a flash against a recycled tube.
        private void OnDisable()
        {
            if (_sweep != null) _sweep.Stop();
            RestoreBody();
        }

        private void RestoreBody()
        {
            if (_body == null) return;

            _body.DOKill();
            _body.color = _bodyBaseTint;
        }
    }
}
