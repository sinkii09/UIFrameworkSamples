using System;
using System.Collections.Generic;
using ColorStackSort.Logic;
using UnityEngine;

namespace ColorStackSort
{
    /// <summary>
    /// Owns the tube and ball GameObjects: builds them from a <see cref="BoardState"/> and destroys
    /// them again. Extracted from <see cref="BoardView"/>, which had grown past the file-size limit
    /// and was doing two unrelated jobs — framework view lifecycle plus scene-graph construction.
    /// <para>
    /// A plain class rather than a MonoBehaviour: it has no lifecycle of its own, and making it a
    /// component would add a second Awake/OnDestroy ordering to reason about for no gain.
    /// </para>
    /// </summary>
    // internal, matching BallPalette — a public constructor taking an internal type is a
    // CS0051 accessibility error, and nothing outside the feature constructs this.
    internal sealed class BoardRenderer
    {
        private readonly TubeView _tubePrefab;
        private readonly BallView _ballPrefab;
        private readonly RectTransform _tubeRow;
        private readonly RectTransform _travelOverlay;
        private readonly BallPalette _palette;
        private readonly Action<int> _onTubeTapped;
        private readonly JuiceBurstEmitter _burstEmitter;

        private readonly List<TubeView> _tubes = new();

        /// <param name="onTubeTapped">
        /// Subscribed on build and unsubscribed on clear, so the pairing lives in one place rather
        /// than being split across the view's show and hide paths.
        /// </param>
        /// <param name="burstEmitter">
        /// Optional. One emitter is shared by every tube — see <see cref="JuiceBurstEmitter"/> for
        /// why that is safe despite bursts overlapping in time.
        /// </param>
        internal BoardRenderer(
            TubeView tubePrefab,
            BallView ballPrefab,
            RectTransform tubeRow,
            RectTransform travelOverlay,
            BallPalette palette,
            Action<int> onTubeTapped,
            JuiceBurstEmitter burstEmitter)
        {
            _tubePrefab = tubePrefab;
            _ballPrefab = ballPrefab;
            _tubeRow = tubeRow;
            _travelOverlay = travelOverlay;
            _palette = palette;
            _onTubeTapped = onTubeTapped;
            _burstEmitter = burstEmitter;
        }

        public int TubeCount => _tubes.Count;

        public TubeView this[int index] => _tubes[index];

        public void Build(BoardState board, UnityEngine.Object context = null)
        {
            Clear();

            if (board == null)
            {
                Debug.LogError("[BoardRenderer] No board to render — was OnShow skipped?", context);
                return;
            }

            if (_tubePrefab == null || _ballPrefab == null)
            {
                Debug.LogError("[BoardRenderer] Tube or ball prefab not assigned.", context);
                return;
            }

            for (var i = 0; i < board.ContainerCount; i++)
            {
                var tube = UnityEngine.Object.Instantiate(_tubePrefab, _tubeRow);
                tube.Initialize(i);
                tube.Tapped += _onTubeTapped;
                _tubes.Add(tube);

                var container = board[i];
                for (var slot = 0; slot < container.Count; slot++)
                {
                    // Parented at spawn so it never touches the scene root, even briefly.
                    var ball = UnityEngine.Object.Instantiate(_ballPrefab, tube.transform);
                    ball.Setup(container[slot], _palette.TintFor(container[slot]));
                    tube.Attach(ball, slot);
                }
            }
        }

        /// <summary>
        /// Fires the completion celebration if <paramref name="container"/> is now full and all one
        /// colour. Lives here rather than in <see cref="BoardView"/> because this class already owns
        /// the two things it needs — the tubes and the palette.
        /// </summary>
        /// <remarks>
        /// Callers must gate this on the move being a FORWARD move. An undo can perfectly well leave
        /// its destination full and monochrome, and celebrating a move being taken back reads as a
        /// bug to the player.
        /// <para>
        /// It cannot double-fire within one landing — <c>PushRange</c> throws when the amount
        /// exceeds free space, so a move onto an already-full tube is impossible. It can fire again
        /// across moves, if a completed tube is emptied and refilled; that is correct.
        /// </para>
        /// </remarks>
        internal void CelebrateIfComplete(StackContainer container, int tubeIndex)
        {
            if (!container.IsFull || !container.IsMonochrome) return;
            if (tubeIndex < 0 || tubeIndex >= _tubes.Count) return;

            var tube = _tubes[tubeIndex];
            if (tube == null) return;

            // Computed once, shared by the burst and the sweep, so the two celebrations for the
            // same completion always agree on colour.
            var tint = _palette.TintFor(container[0]);
            tube.PlayCompleteFeedback(tint);

            if (_burstEmitter == null) return;

            // Burst at the top of the column, where the last ball just landed — not at the tube's
            // origin, which is its base.
            _burstEmitter.Burst(tube.SlotWorldPosition(container.Count - 1), tint);
        }

        public void Clear()
        {
            foreach (var tube in _tubes)
            {
                if (tube == null) continue;

                tube.Tapped -= _onTubeTapped;
                tube.ClearBalls();
                UnityEngine.Object.Destroy(tube.gameObject);
            }

            _tubes.Clear();

            // Balls stranded on the overlay by a cancelled move are not owned by any tube.
            if (_travelOverlay == null) return;
            for (var i = _travelOverlay.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(_travelOverlay.GetChild(i).gameObject);
        }
    }
}
