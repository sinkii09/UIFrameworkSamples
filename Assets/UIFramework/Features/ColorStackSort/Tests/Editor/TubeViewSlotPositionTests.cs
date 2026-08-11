using DG.Tweening;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ColorStackSort.Tests
{
    /// <summary>
    /// Regression guard for a RectTransform anchor-fraction bug: a point-anchored ball resolves its
    /// Y position against its parent's RECT BOUNDS, not the parent's local origin — so a parent rect
    /// built at Tube-height with a bottom pivot silently stranded children near the tube's vertical
    /// CENTER instead of its bottom. Fixed by zeroing Slots' rect height in
    /// <c>ColorStackSortPrefabBuilder.CreateTube</c>; this test fails against the pre-fix builder
    /// output and passes against the fixed one.
    /// </summary>
    public class TubeViewSlotPositionTests
    {
        private const string TubePrefabPath =
            "Assets/UIFramework/Features/ColorStackSort/Prefabs/Tube.prefab";

        // Mirrors TubeView's own default _slotHeight — not read via reflection, just the same
        // "well within one slot" tolerance the plan called for. A ball stranded by the bug lands
        // roughly 200 units up, comfortably outside this.
        private const float OneSlotHeight = 88f;

        private GameObject _tubeInstance;
        private GameObject _ballInstance;

        [SetUp]
        public void SetUp()
        {
            // Attach -> SnapTo -> ResetTransformTweens calls DOKill(); idempotent, and this is the
            // first EditMode test in this feature to touch a tween-bearing path outside Play Mode.
            DOTween.Init();

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TubePrefabPath);
            Assert.IsNotNull(prefab,
                $"Tube prefab missing at {TubePrefabPath} — run Tools/ColorStackSort/Build Prefabs.");
            _tubeInstance = Object.Instantiate(prefab);

            // A bare BallView rather than Ball.prefab: only its RectTransform anchors matter here
            // (per BallView's own doc, point anchors == anchorMin == anchorMax == pivot), and this
            // keeps the test decoupled from Ball.prefab's own contents.
            _ballInstance = new GameObject("TestBall", typeof(RectTransform));
            var rect = (RectTransform)_ballInstance.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            _ballInstance.AddComponent<BallView>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_tubeInstance != null) Object.DestroyImmediate(_tubeInstance);
            if (_ballInstance != null) Object.DestroyImmediate(_ballInstance);
        }

        [Test]
        public void Attach_FirstSlot_LandsNearTubeBottomNotCenter()
        {
            var tubeRect = (RectTransform)_tubeInstance.transform;
            var ball = _ballInstance.GetComponent<BallView>();
            var tube = _tubeInstance.GetComponent<TubeView>();

            tube.Attach(ball, 0);

            // Measuring against the TUBE'S OWN TRANSFORM (localY == 0) is NOT the same as measuring
            // against its bottom edge: Tube's own pivot is (0.5, 0.5) — its transform sits at its
            // CENTER, not its bottom. A ball merely "close to 0" is close to the tube's center,
            // which is the exact bug this test exists to catch. The true bottom edge, in the tube's
            // own local space, is tubeRect.rect.yMin (= -height/2, since pivot.y = 0.5).
            var localY = tube.transform.InverseTransformPoint(ball.transform.position).y;
            var distanceFromBottomEdge = localY - tubeRect.rect.yMin;

            Assert.Less(distanceFromBottomEdge, OneSlotHeight,
                "Ball landed far above the tube's true bottom edge — the Slots anchor-fraction bug is back.");
        }
    }
}
