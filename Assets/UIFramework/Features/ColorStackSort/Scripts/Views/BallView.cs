using ColorStackSort.Logic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ColorStackSort
{
    /// <summary>
    /// One ball. Deliberately dumb — it holds colour and identity, and <see cref="BoardView"/> drives
    /// every tween.
    /// <para>
    /// It must never attach an <c>OnComplete</c> handler: <c>TweenExtensions.AwaitAsync</c> sets
    /// <c>OnComplete</c> itself, and DOTween's <c>OnComplete</c> <b>replaces</b> rather than appends,
    /// so anything set here would be silently discarded the moment the tween is awaited.
    /// </para>
    /// <para>
    /// The prefab must use point anchors (<c>anchorMin == anchorMax == pivot == (0.5, 0.5)</c>).
    /// With stretch anchors the ball's rect is derived from its parent, so it would visibly resize
    /// when reparented onto the travel overlay mid-move.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class BallView : MonoBehaviour
    {
        // Squash on impact: wider and shorter, springing back to unit scale.
        private static readonly Vector3 ImpactPunch = new Vector3(0.22f, -0.22f, 0f);
        private const float ImpactDuration = 0.24f;
        private const int ImpactVibrato = 6;
        private const float ImpactElasticity = 0.6f;

        [SerializeField] private Image _image;

        private RectTransform _rect;

        public RectTransform Rect => _rect != null ? _rect : _rect = (RectTransform)transform;
        public ColorId BallColor { get; private set; }

        public void Setup(ColorId ballColor, Color tint)
        {
            BallColor = ballColor;
            if (_image != null) _image.color = tint;
        }

        /// <summary>
        /// Stops every tween on this ball and returns it to unit scale. The single place that is
        /// allowed to kill a ball's tweens.
        /// </summary>
        /// <remarks>
        /// <c>DOKill(complete: false)</c> stops a tween where it stands and does <b>not</b> restore
        /// what it was writing. Once anything scales a ball, a bare <c>DOKill</c> therefore strands
        /// it at whatever scale the tween had reached — permanently, since nothing else writes
        /// <c>localScale</c>. Killing and restoring are one operation for that reason, and every
        /// caller goes through here rather than reproducing the pair correctly by hand.
        /// <para>
        /// Position is deliberately not restored: every caller writes its own target immediately
        /// afterwards, whereas scale has no such owner.
        /// </para>
        /// </remarks>
        public void ResetTransformTweens()
        {
            Rect.DOKill();
            Rect.localScale = Vector3.one;
        }

        public void SnapTo(Vector2 anchoredPosition)
        {
            ResetTransformTweens();
            Rect.anchoredPosition = anchoredPosition;
        }

        /// <summary>
        /// Squash-and-stretch as the ball lands. Fire-and-forget; nothing awaits it.
        /// </summary>
        /// <param name="delay">
        /// Staggers a landing run so the balls pop in the order they arrived rather than as one
        /// block. It is a cascade after the fact, not a match to each ball's own arrival time — the
        /// travel tweens are staggered but <see cref="TubeView.Attach"/> runs for the whole run at
        /// once, after the sequence completes, so no per-ball landing moment exists to sync to.
        /// </param>
        public void PlayLandingImpact(float delay = 0f)
        {
            // Kill-then-punch, like every other tween start in this feature: two landings inside one
            // impact duration would otherwise stack two scale tweens on one transform.
            ResetTransformTweens();

            Rect.DOPunchScale(ImpactPunch, ImpactDuration, ImpactVibrato, ImpactElasticity)
                .SetDelay(delay)
                .SetLink(gameObject);
        }

        private void OnDestroy() => transform.DOKill();
    }
}
