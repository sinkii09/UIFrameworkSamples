using System.Collections.Generic;
using Sinkii09.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RecyclerShowcase.Editor
{
    /// <summary>
    /// A RecyclerView with a live stats line above it. Same scaffold as the package's
    /// "GameObject/UI/UIFramework/Recycler View" menu item (ScrollRect + RectMask2D viewport + content),
    /// saved ACTIVE: RecyclerView has no [ExecuteAlways], so nothing initialises in edit mode, and a
    /// view saved inactive would reach the panel's Initialize before its own Awake.
    /// </summary>
    internal static class ShowcaseRecyclerFactory
    {
        private const float StatsHeight = 44f;

        internal static RecyclerView Build(Transform parent, string caption, Vector2 anchorMin, Vector2 anchorMax,
                                           Vector2 offsetMin, Vector2 offsetMax, RecyclerSpec spec,
                                           IReadOnlyList<RecyclerCell> prefabs, float statsFontSize = 18f)
        {
            RectTransform area = ShowcaseUiFactory.Area(caption, parent, anchorMin, anchorMax, offsetMin, offsetMax);

            RectTransform root = ShowcaseUiFactory.Area("Recycler View", area, Vector2.zero, Vector2.one,
                                                        Vector2.zero, new Vector2(0f, -StatsHeight - 4f));
            ShowcaseUiFactory.Image(root, ShowcaseUiFactory.Ink);

            var scrollRect = root.gameObject.AddComponent<ScrollRect>();
            RectTransform viewport = ShowcaseUiFactory.Stretch("Viewport", root, 4f);
            viewport.gameObject.AddComponent<RectMask2D>();
            // Transparent but raycastable, so a drag that starts between cells still scrolls.
            ShowcaseUiFactory.Image(viewport, Color.clear, raycast: true);
            RectTransform content = ShowcaseUiFactory.Create("Content", viewport);

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.scrollSensitivity = 40f;

            // Added after the ScrollRect is wired so its RequireComponent finds it configured.
            var view = root.gameObject.AddComponent<RecyclerView>();
            ShowcaseWiring.Write(view, s =>
            {
                ShowcaseWiring.Prop(s, "_direction").intValue = (int)spec.Direction;
                ShowcaseWiring.Prop(s, "_settings._cellSize").floatValue = spec.CellSize;
                ShowcaseWiring.Prop(s, "_settings._spacing").floatValue = spec.Spacing;
                ShowcaseWiring.Prop(s, "_settings._crossAxisCount").intValue = spec.CrossAxisCount;
                ShowcaseWiring.Prop(s, "_settings._crossSpacing").floatValue = spec.CrossSpacing;
            });
            ShowcaseWiring.Refs(view, "_cellPrefabs", prefabs);

            BuildStats(area, view, caption, statsFontSize);
            return view;
        }

        private static void BuildStats(RectTransform area, RecyclerView view, string caption, float fontSize)
        {
            RectTransform rect = ShowcaseUiFactory.TopBand("Stats", area, 0f, StatsHeight, 0f);
            TMP_Text label = ShowcaseUiFactory.Label(rect, caption, fontSize, TextAlignmentOptions.BottomLeft,
                                                     new Color(0.82f, 0.85f, 0.92f));

            var stats = rect.gameObject.AddComponent<RecyclerStatsLabel>();
            ShowcaseWiring.Write(stats, s =>
            {
                ShowcaseWiring.Prop(s, "_target").objectReferenceValue = view;
                ShowcaseWiring.Prop(s, "_label").objectReferenceValue = label;
                ShowcaseWiring.Prop(s, "_caption").stringValue = caption;
            });
        }
    }
}
