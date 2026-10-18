using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RecyclerShowcase.Editor
{
    /// <summary>
    /// The skeleton every panel shares: a wrapping toolbar, an on-screen hint box, and a content area
    /// below. Reference resolution is 1080×1920 portrait (this project's Game view); everything is
    /// anchored by fraction, so a landscape window still lays out, just with wider lists.
    /// </summary>
    internal static class ShowcasePanelLayout
    {
        internal const float TabBarHeight = 110f;
        private const float ToolbarTop = 14f;
        private const float ToolbarHeight = 130f;   // two rows of 60 px buttons
        private const float HintTop = ToolbarTop + ToolbarHeight + 10f;
        private const float HintHeight = 250f;
        private const float Gutter = 20f;

        internal static RectTransform Create<T>(Transform canvas, string name, Button buttonTemplate, out T panel)
            where T : ShowcasePanel
        {
            RectTransform root = ShowcaseUiFactory.Area(name, canvas, Vector2.zero, Vector2.one,
                                                        Vector2.zero, new Vector2(0f, -TabBarHeight));

            RectTransform toolbarRect = ShowcaseUiFactory.TopBand("Toolbar", root, ToolbarTop, ToolbarHeight, Gutter);
            // A grid, not a horizontal row: it wraps, so a 9-button panel fits a portrait screen.
            var layout = toolbarRect.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(196f, 60f);
            layout.spacing = new Vector2(10f, 10f);
            layout.childAlignment = TextAnchor.UpperLeft;
            var toolbar = toolbarRect.gameObject.AddComponent<ShowcaseToolbar>();
            ShowcaseWiring.Ref(toolbar, "_buttonTemplate", buttonTemplate);

            RectTransform hintRect = ShowcaseUiFactory.TopBand("Hint", root, HintTop, HintHeight, Gutter);
            ShowcaseUiFactory.Image(hintRect, ShowcaseUiFactory.Panel);
            TMP_Text hint = ShowcaseUiFactory.Label(ShowcaseUiFactory.Stretch("Text", hintRect, 14f), "", 21f,
                                                    TextAlignmentOptions.TopLeft, new Color(0.9f, 0.92f, 0.97f));
            hint.enableAutoSizing = true;   // long hints shrink instead of spilling onto the list
            hint.fontSizeMin = 14f;
            hint.fontSizeMax = 21f;

            RectTransform content = ShowcaseUiFactory.Area("Content", root, Vector2.zero, Vector2.one,
                                                           new Vector2(Gutter, Gutter),
                                                           new Vector2(-Gutter, -(HintTop + HintHeight + 12f)));

            panel = root.gameObject.AddComponent<T>();
            ShowcaseWiring.Ref(panel, "_toolbar", toolbar);
            ShowcaseWiring.Ref(panel, "_hint", hint);
            return content;
        }
    }
}
