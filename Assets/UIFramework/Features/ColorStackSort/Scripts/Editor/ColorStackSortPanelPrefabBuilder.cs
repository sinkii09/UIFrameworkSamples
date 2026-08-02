using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ColorStackSort.Editor
{
    /// <summary>
    /// Builds the two chrome pieces — the control bar under the board, and the level-complete
    /// panel. Split from <see cref="ColorStackSortPrefabBuilder"/>, which owns the board itself and
    /// had grown past the file-size limit.
    /// </summary>
    internal static class ColorStackSortPanelPrefabBuilder
    {
        /// <summary>
        /// The bar under the board. A child of BoardView rather than its own UIView: the HUD layer
        /// sorts below Screen, so a HUD-layer view would render underneath the board it controls.
        /// </summary>
        internal static BoardControlBar CreateControlBar(RectTransform root)
        {
            var bar = UiPrefabFactory.CreatePoint("ControlBar", root, new Vector2(900f, 120f));
            bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.anchoredPosition = new Vector2(0f, 60f);

            var levelRect = UiPrefabFactory.CreatePoint("LevelLabel", bar, new Vector2(320f, 60f));
            levelRect.anchoredPosition = new Vector2(-280f, 0f);
            var levelLabel = UiPrefabFactory.AddLabel(levelRect, "Level 1", 40f, TextAlignmentOptions.Left);

            var movesRect = UiPrefabFactory.CreatePoint("MovesLabel", bar, new Vector2(200f, 60f));
            var movesLabel = UiPrefabFactory.AddLabel(movesRect, "0", 40f, TextAlignmentOptions.Center);

            var undoRect = UiPrefabFactory.CreatePoint("UndoButton", bar, new Vector2(170f, 90f));
            undoRect.anchoredPosition = new Vector2(240f, 0f);
            var undoButton = UiPrefabFactory.AddButton(undoRect, "Undo", new Color(1f, 1f, 1f, 0.18f));

            var restartRect = UiPrefabFactory.CreatePoint("RestartButton", bar, new Vector2(170f, 90f));
            restartRect.anchoredPosition = new Vector2(420f, 0f);
            var restartButton = UiPrefabFactory.AddButton(restartRect, "Restart", new Color(1f, 1f, 1f, 0.18f));

            ColorStackSortJuicePrefabParts.AddPressPunch(undoButton);
            ColorStackSortJuicePrefabParts.AddPressPunch(restartButton);

            var controlBar = bar.gameObject.AddComponent<BoardControlBar>();
            UiPrefabFactory.SetReference(controlBar, "_levelLabel", levelLabel);
            UiPrefabFactory.SetReference(controlBar, "_movesLabel", movesLabel);
            UiPrefabFactory.SetReference(controlBar, "_undoButton", undoButton);
            UiPrefabFactory.SetReference(controlBar, "_restartButton", restartButton);

            return controlBar;
        }

        /// <summary>
        /// The level-complete panel. Popup layer, so it draws above the board — see
        /// <see cref="ColorStackSortWinView"/>.
        /// </summary>
        internal static GameObject CreateWinView()
        {
            var root = UiPrefabFactory.CreateStretch("ColorStackSortWinView", null);
            root.gameObject.AddComponent<CanvasGroup>();

            // Dims the board and, being a raycast target, stops taps reaching through to it.
            UiPrefabFactory.AddImage(root, null, new Color(0f, 0f, 0f, 0.62f), false);

            var panel = UiPrefabFactory.CreatePoint("Panel", root, new Vector2(760f, 520f));
            UiPrefabFactory.AddImage(
                panel, UiPrefabFactory.BuiltIn(UiPrefabFactory.PanelSprite),
                new Color(0.16f, 0.18f, 0.26f, 0.98f), true);

            var titleRect = UiPrefabFactory.CreatePoint("TitleLabel", panel, new Vector2(680f, 90f));
            titleRect.anchoredPosition = new Vector2(0f, 150f);
            var titleLabel = UiPrefabFactory.AddLabel(
                titleRect, "Level 1 complete", 52f, TextAlignmentOptions.Center);

            var movesRect = UiPrefabFactory.CreatePoint("MovesLabel", panel, new Vector2(680f, 70f));
            movesRect.anchoredPosition = new Vector2(0f, 40f);
            var movesLabel = UiPrefabFactory.AddLabel(movesRect, "0 moves", 38f, TextAlignmentOptions.Center);

            var nextRect = UiPrefabFactory.CreatePoint("NextButton", panel, new Vector2(360f, 110f));
            nextRect.anchoredPosition = new Vector2(0f, -140f);
            var nextButton = UiPrefabFactory.AddButton(nextRect, "Next", new Color(0.30f, 0.62f, 0.95f, 1f));

            ColorStackSortJuicePrefabParts.AddPressPunch(nextButton);

            // Anchored at the top of the panel and never moved, so it uses the fixed-position
            // Burst overload. A child of the view root, so it clears itself via its own OnDisable
            // when the view deactivates — including on a cancelled entrance.
            var confetti = ColorStackSortJuicePrefabParts.CreateBurstEmitter(panel, "Confetti");
            confetti.transform.localPosition = new Vector3(0f, 230f, 0f);

            var view = root.gameObject.AddComponent<ColorStackSortWinView>();
            UiPrefabFactory.SetReference(view, "_panel", panel);
            UiPrefabFactory.SetReference(view, "_titleLabel", titleLabel);
            UiPrefabFactory.SetReference(view, "_movesLabel", movesLabel);
            UiPrefabFactory.SetReference(view, "_nextButton", nextButton);
            UiPrefabFactory.SetReference(view, "_confetti", confetti);

            return root.gameObject;
        }
    }
}
