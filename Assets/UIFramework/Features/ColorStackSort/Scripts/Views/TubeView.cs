using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ColorStackSort
{
    /// <summary>
    /// One tube. Owns its ball column, its slot geometry, and its own self-contained feedback
    /// animations (lift and reject-shake). Cross-tube travel belongs to <see cref="BoardView"/>.
    /// <para>
    /// <b>Never add a LayoutGroup to this prefab.</b> A <c>LayoutGroup</c> rewrites
    /// <c>anchoredPosition</c> every frame and silently defeats every <c>DOAnchorPos</c> here —
    /// balls would simply not animate. Slot positions are computed instead; that is the whole point.
    /// </para>
    /// </summary>
    public sealed class TubeView : MonoBehaviour
    {
        private const float LiftDuration = 0.14f;
        private const float ShakeDuration = 0.3f;

        [SerializeField] private RectTransform _slotRoot;
        [SerializeField] private Button _button;
        [SerializeField] private TubeFeedback _feedback;
        [SerializeField] private float _slotHeight = 88f;
        [SerializeField] private float _bottomOffset = 14f;
        [SerializeField] private float _liftHeight = 74f;

        private readonly List<BallView> _balls = new();

        public int Index { get; private set; }
        public int BallCount => _balls.Count;

        /// <summary>Raised with this tube's index when tapped.</summary>
        public event Action<int> Tapped;

        private void Awake()
        {
            if (_slotRoot == null)
            {
                // Falling back to the tube's own transform is NOT equivalent: the tube root's pivot
                // is (0.5, 0.5) — center — while Slots is deliberately a bottom-pivoted, zero-height
                // rect (see ColorStackSortPrefabBuilder.CreateTube). Silently substituting it would
                // reintroduce the exact "balls land near the tube's center" bug that fix corrected.
                Debug.LogError($"[TubeView {name}] _slotRoot not assigned — falling back to the tube " +
                                "root, which will misplace every ball. Re-run Tools/ColorStackSort/Build Prefabs.", this);
                _slotRoot = (RectTransform)transform;
            }

            if (_button != null) _button.onClick.AddListener(() => Tapped?.Invoke(Index));
        }

        public void Initialize(int index) => Index = index;

        /// <summary>
        /// Local position of a slot. Balls use point anchors, so this doubles as their
        /// <c>anchoredPosition</c> while parented here.
        /// </summary>
        public Vector2 SlotPosition(int slot) => new Vector2(0f, _bottomOffset + slot * _slotHeight);

        public Vector3 SlotWorldPosition(int slot) => _slotRoot.TransformPoint(SlotPosition(slot));

        /// <summary>Parents a ball into a slot and snaps it there. Used to land a completed move.</summary>
        public void Attach(BallView ball, int slot)
        {
            if (ball == null)
            {
                // Silently skipping would shift every later Add down one slot while SnapTo kept
                // using the original index — a permanent, undiagnosable view/model divergence.
                Debug.LogError($"[TubeView {Index}] Attach called with a null ball for slot {slot}.", this);
                return;
            }

            // worldPositionStays: false — the ball is being placed, not carried, so local space
            // should reset before SnapTo writes the slot position.
            ball.transform.SetParent(_slotRoot, false);

            // SnapTo resets scale via ResetTransformTweens. This used to set localScale itself and
            // was correct only by accident of the two statements' ordering; the invariant belongs in
            // one place, so it is not duplicated back here.
            ball.SnapTo(SlotPosition(slot));

            if (slot >= _balls.Count) _balls.Add(ball);
            else _balls[slot] = ball;
        }

        /// <summary>
        /// Removes the top <paramref name="count"/> balls and returns them BOTTOM-TO-TOP.
        /// Order matters: the caller reparents in this order so the topmost ball draws last.
        /// Does not reparent — the caller owns that.
        /// </summary>
        public List<BallView> PopTop(int count)
        {
            // Clamping quietly would hand back fewer balls than the model just moved, leaving the
            // view permanently short. The model is the authority, so this can only be a bug.
            if (count > _balls.Count)
                Debug.LogError(
                    $"[TubeView {Index}] Asked for {count} balls but only {_balls.Count} present — " +
                    "view has desynced from the board.", this);

            var taken = new List<BallView>(count);
            var first = Mathf.Max(0, _balls.Count - count);

            for (var i = first; i < _balls.Count; i++) taken.Add(_balls[i]);
            _balls.RemoveRange(first, _balls.Count - first);

            return taken;
        }

        /// <summary>Raises or lowers the top <paramref name="runLength"/> balls to show selection.</summary>
        public void AnimateTopRun(int runLength, bool lifted)
        {
            var first = Mathf.Max(0, _balls.Count - runLength);

            for (var i = first; i < _balls.Count; i++)
            {
                var ball = _balls[i];
                if (ball == null) continue;

                var target = SlotPosition(i) + (lifted ? new Vector2(0f, _liftHeight) : Vector2.zero);

                // ResetTransformTweens, not a bare DOKill: this path tweens position only, so a
                // landing-impact scale punch killed here would leave the ball squashed with nothing
                // left to restore it. Selecting a run that is still landing is enough to hit it.
                ball.ResetTransformTweens();
                ball.Rect.DOAnchorPos(target, LiftDuration).SetEase(Ease.OutQuad).SetLink(ball.gameObject);
            }
        }

        /// <summary>
        /// The only feedback an illegal tap produces. Fire-and-forget; nothing awaits it.
        /// <para>
        /// Shakes <c>_slotRoot</c>, not this transform. The tube row is arranged by a LayoutGroup,
        /// which rewrites the tube's own <c>anchoredPosition</c> every frame and would erase the
        /// shake entirely. <c>_slotRoot</c> is a child and therefore unmanaged — and shaking it
        /// carries the balls along, which reads better anyway.
        /// </para>
        /// </summary>
        public void Shake()
        {
            if (_slotRoot == null) return;

            // Cannot fight the shake below: different targets. The flash writes the body Image, the
            // shake writes _slotRoot's RectTransform, and DOKill on one never touches the other.
            if (_feedback != null) _feedback.PlayRejected();

            // DOKill FIRST, then capture. Reading anchoredPosition before the kill would sample a
            // shake already in flight — an offset of up to the shake amplitude — and this shake's
            // OnKill would later write that stale value back, leaving the column permanently
            // off-centre. Two rejected taps inside the shake duration is enough to trigger it.
            _slotRoot.DOKill(true);
            var origin = _slotRoot.anchoredPosition;
            _slotRoot.DOShakeAnchorPos(ShakeDuration, new Vector2(14f, 0f), 18, 90f, false, true)
                .SetLink(gameObject)
                // Restore explicitly: a shake killed mid-flight would otherwise strand the column
                // off-centre for the rest of the level.
                .OnKill(() => { if (_slotRoot != null) _slotRoot.anchoredPosition = origin; });
        }

        /// <summary>
        /// Celebrates this tube being completed. Delegated rather than implemented here so the
        /// UIEffect dependency stays inside <see cref="TubeFeedback"/>.
        /// </summary>
        public void PlayCompleteFeedback(Color tint)
        {
            if (_feedback != null) _feedback.PlayComplete(tint);
        }

        public void ClearBalls()
        {
            foreach (var ball in _balls)
                if (ball != null) Destroy(ball.gameObject);

            _balls.Clear();
        }

        private void OnDestroy()
        {
            transform.DOKill();
            if (_slotRoot != null) _slotRoot.DOKill();
        }
    }
}
