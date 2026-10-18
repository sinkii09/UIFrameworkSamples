using Sinkii09.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RecyclerShowcase.Editor
{
    /// <summary>
    /// Builds the five showcase panels and wires each to its RecyclerView(s). Areas are anchor
    /// fractions of the panel's content rect (portrait-first): vertical lists get tall areas,
    /// horizontal lists wide, short ones.
    /// </summary>
    internal static class ShowcasePanelBuilder
    {
        private const float StatusHeight = 88f;   // two lines: correctness, then prefetch

        internal static ShowcasePanel[] BuildAll(Transform canvas, Button template, RecyclerCell row, RecyclerCell header,
                                                 RecyclerCell picture)
        {
            return new ShowcasePanel[]
            {
                List(canvas, template, row, header),
                VariableSize(canvas, template, row, header),
                Grid(canvas, template, row),
                Directions(canvas, template, row),
                AsyncPictures(canvas, template, picture),
            };
        }

        private static ShowcasePanel AsyncPictures(Transform canvas, Button template, RecyclerCell picture)
        {
            RectTransform content = ShowcasePanelLayout.Create(canvas, "Panel · Async", template, out AsyncPicturesShowcase panel);

            RectTransform statusRect = ShowcaseUiFactory.TopBand("Status", content, 0f, StatusHeight, 0f);
            ShowcaseUiFactory.Image(statusRect, ShowcaseUiFactory.Panel);
            TMP_Text status = ShowcaseUiFactory.Label(ShowcaseUiFactory.Stretch("Text", statusRect, 10f), "", 24f,
                                                      TextAlignmentOptions.MidlineLeft, Color.white);

            RecyclerView view = ShowcaseRecyclerFactory.Build(
                content, "TopToBottom · 120 px · loads 50–600 ms · prefetch 600 px", Vector2.zero, Vector2.one,
                Vector2.zero, new Vector2(0f, -(StatusHeight + 8f)),
                new RecyclerSpec(ScrollDirection.TopToBottom, 120f, 4f, prefetchDistance: 600f), new[] { picture });

            ShowcaseWiring.Ref(panel, "_list", view);
            ShowcaseWiring.Ref(panel, "_status", status);
            return panel;
        }

        private static ShowcasePanel List(Transform canvas, Button template, RecyclerCell row, RecyclerCell header)
        {
            RectTransform content = ShowcasePanelLayout.Create(canvas, "Panel · List", template, out ListShowcase panel);
            RecyclerView view = Area(content, "TopToBottom · uniform 80 px · ids 0+1", 0f, 0f, 1f, 1f,
                                     new RecyclerSpec(ScrollDirection.TopToBottom, 80f, 4f), new[] { row, header });
            ShowcaseWiring.Ref(panel, "_list", view);
            return panel;
        }

        private static ShowcasePanel VariableSize(Transform canvas, Button template, RecyclerCell row, RecyclerCell header)
        {
            RectTransform content = ShowcasePanelLayout.Create(canvas, "Panel · Variable", template, out VariableSizeShowcase panel);
            RecyclerView view = Area(content, "TopToBottom · declared sizes · uniform fallback 100 px", 0f, 0f, 1f, 1f,
                                     new RecyclerSpec(ScrollDirection.TopToBottom, 100f, 4f), new[] { row, header });
            ShowcaseWiring.Ref(panel, "_list", view);
            return panel;
        }

        private static ShowcasePanel Grid(Transform canvas, Button template, RecyclerCell row)
        {
            RectTransform content = ShowcasePanelLayout.Create(canvas, "Panel · Grid", template, out GridShowcase panel);
            RecyclerView vertical = Area(content, "Vertical grid · TopToBottom", 0f, 0.37f, 1f, 1f,
                                         new RecyclerSpec(ScrollDirection.TopToBottom, 160f, 8f, 4, 8f), new[] { row });
            RecyclerView horizontal = Area(content, "Horizontal grid · LeftToRight", 0f, 0f, 1f, 0.34f,
                                           new RecyclerSpec(ScrollDirection.LeftToRight, 200f, 8f, 3, 8f), new[] { row });
            ShowcaseWiring.Ref(panel, "_vertical", vertical);
            ShowcaseWiring.Ref(panel, "_horizontal", horizontal);
            return panel;
        }

        private static ShowcasePanel Directions(Transform canvas, Button template, RecyclerCell row)
        {
            RectTransform content = ShowcasePanelLayout.Create(canvas, "Panel · Directions", template, out DirectionsShowcase panel);
            var cells = new[] { row };
            RecyclerView[] lists =
            {
                Area(content, "TopToBottom", 0.00f, 0.44f, 0.48f, 1.00f, Spec(ScrollDirection.TopToBottom, 70f), cells, 15f),
                Area(content, "BottomToTop", 0.52f, 0.44f, 1.00f, 1.00f, Spec(ScrollDirection.BottomToTop, 70f), cells, 15f),
                Area(content, "LeftToRight", 0.00f, 0.22f, 1.00f, 0.41f, Spec(ScrollDirection.LeftToRight, 120f), cells, 15f),
                Area(content, "RightToLeft", 0.00f, 0.00f, 1.00f, 0.19f, Spec(ScrollDirection.RightToLeft, 120f), cells, 15f),
            };
            ShowcaseWiring.Refs(panel, "_lists", lists);
            return panel;
        }

        private static RecyclerSpec Spec(ScrollDirection direction, float cellSize) => new(direction, cellSize, 4f);

        private static RecyclerView Area(Transform content, string caption, float xMin, float yMin, float xMax, float yMax,
                                         RecyclerSpec spec, RecyclerCell[] cells, float statsFontSize = 18f)
        {
            return ShowcaseRecyclerFactory.Build(
                content, caption, new Vector2(xMin, yMin), new Vector2(xMax, yMax), Vector2.zero, Vector2.zero,
                spec, cells, statsFontSize);
        }
    }
}
