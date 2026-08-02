using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sinkii09.UIFramework;
using UnityEngine;

namespace ColorStackSort
{
    /// <summary>
    /// Carries a run of balls from one tube to another. Split out of <see cref="BoardView"/> because
    /// the cross-tube choreography is a separate concern from board structure and input routing.
    /// </summary>
    internal sealed class BoardMoveAnimator
    {
        private const float TravelDuration = 0.26f;
        private const float TravelStagger = 0.05f;

        private readonly RectTransform _overlay;
        private readonly GameObject _linkTarget;

        internal BoardMoveAnimator(RectTransform overlay, GameObject linkTarget)
        {
            _overlay = overlay;
            _linkTarget = linkTarget;
        }

        /// <summary>
        /// Moves the top <paramref name="count"/> balls from <paramref name="source"/> onto
        /// <paramref name="destination"/>. Throws <see cref="System.OperationCanceledException"/> if
        /// the tween is killed — callers must release their input guard in a <c>finally</c>.
        /// </summary>
        internal async UniTask PlayAsync(
            TubeView source, TubeView destination, int count, CancellationToken ct)
        {
            // Bottom-to-top, so the topmost ball reparents last and therefore draws last.
            var moving = source.PopTop(count);
            var firstSlot = destination.BallCount;

            Lift(moving);

            var sequence = DOTween.Sequence().SetLink(_linkTarget);
            for (var i = 0; i < moving.Count; i++)
            {
                var target = ToOverlaySpace(destination.SlotWorldPosition(firstSlot + i));
                sequence.Insert(
                    i * TravelStagger,
                    moving[i].Rect.DOAnchorPos(target, TravelDuration).SetEase(Ease.OutQuad));
            }

            await sequence.AwaitAsync(ct);

            // Snapped AFTER the await, never via OnComplete — AwaitAsync sets its own OnComplete and
            // DOTween replaces rather than appends, so a callback here would be silently discarded.
            for (var i = 0; i < moving.Count; i++)
            {
                destination.Attach(moving[i], firstSlot + i);

                // After Attach, which resets scale — a punch started before it would be killed by
                // the very SnapTo that lands the ball.
                moving[i].PlayLandingImpact(i * TravelStagger);
            }
        }

        /// <summary>
        /// Reparents the run onto the overlay so it draws above every tube it passes over.
        /// </summary>
        private void Lift(List<BallView> moving)
        {
            foreach (var ball in moving)
            {
                // Kill the selection-lift tween BEFORE reparenting. It is fire-and-forget and runs
                // for 0.14s, so tapping a destination quickly leaves it alive — and once the ball is
                // on the overlay its target (a tube-local slot position) means somewhere else
                // entirely. Being the older tween it also writes first each frame, so the travel
                // tweens would capture that corrupted value as their start and the balls would warp.
                ball.ResetTransformTweens();

                // worldPositionStays: true — the balls are mid-flight and must not jump. It preserves
                // world position/rotation/scale only; the point anchors on the ball prefab are what
                // stop the rect being re-derived against the overlay and visibly resizing.
                ball.transform.SetParent(_overlay, true);

                // Preserving world POSITION is the point of that flag; preserving world SCALE is not.
                // SetParent recomputes localScale from the world matrix, so any lossyScale difference
                // between the tube's slot root and the overlay leaves the ball non-unit here — before
                // any tween is involved at all. Resetting before the reparent cannot cover this.
                ball.Rect.localScale = Vector3.one;
            }
        }

        private Vector2 ToOverlaySpace(Vector3 worldPosition)
        {
            var local = _overlay.InverseTransformPoint(worldPosition);
            return new Vector2(local.x, local.y);
        }
    }
}
