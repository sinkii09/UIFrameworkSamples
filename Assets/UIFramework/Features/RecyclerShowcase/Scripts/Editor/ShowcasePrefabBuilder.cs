using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RecyclerShowcase.Editor
{
    /// <summary>
    /// The two cell prefabs: id 0 "row" and id 1 "header/separator". Both carry the same
    /// <see cref="ShowcaseCell"/>. Root anchors/pivot/size are irrelevant — the RecyclerView overwrites
    /// them per bind — so only the label child needs stretch anchors. No LayoutGroup or
    /// ContentSizeFitter: sizes are declared by the view, a self-sizing cell is reported as an error.
    /// </summary>
    internal static class ShowcasePrefabBuilder
    {
        internal const string Folder = "Assets/UIFramework/Features/RecyclerShowcase/Prefabs";
        internal const string RowPath = Folder + "/ShowcaseRowCell.prefab";
        internal const string HeaderPath = Folder + "/ShowcaseHeaderCell.prefab";
        internal const string PicturePath = Folder + "/AsyncPictureCell.prefab";

        internal static void BuildAll()
        {
            Save(BuildCell("ShowcaseRowCell", Color.white, ShowcaseUiFactory.Ink, 24f,
                           TextAlignmentOptions.MidlineLeft, FontStyles.Normal), RowPath);
            Save(BuildCell("ShowcaseHeaderCell", ShowcaseCell.HeaderColor, Color.white, 20f,
                           TextAlignmentOptions.Center, FontStyles.Bold), HeaderPath);
            Save(BuildPictureCell(), PicturePath);
        }

        /// <summary>
        /// Row label on the left, "picture" on the right with its own key printed on it — so a row
        /// showing another row's picture reads as "#120 … img #87" without any tooling.
        /// </summary>
        private static GameObject BuildPictureCell()
        {
            RectTransform root = ShowcaseUiFactory.Create("AsyncPictureCell", null);
            root.sizeDelta = new Vector2(400f, 120f);
            ShowcaseUiFactory.Image(root, ShowcaseUiFactory.Panel, raycast: true);

            RectTransform rowRect = ShowcaseUiFactory.Area("Row", root, Vector2.zero, new Vector2(0.3f, 1f),
                                                           new Vector2(18f, 2f), new Vector2(-6f, -2f));
            TMP_Text row = ShowcaseUiFactory.Label(rowRect, "#0", 30f, TextAlignmentOptions.MidlineLeft, Color.white);
            row.fontStyle = FontStyles.Bold;

            RectTransform pictureRect = ShowcaseUiFactory.Area("Picture", root, new Vector2(0.3f, 0.08f),
                                                               new Vector2(1f, 0.92f), Vector2.zero, new Vector2(-12f, 0f));
            Image picture = ShowcaseUiFactory.Image(pictureRect, Color.gray);
            TMP_Text pictureLabel = ShowcaseUiFactory.Label(ShowcaseUiFactory.Stretch("Key", pictureRect, 4f), "img #0", 28f,
                                                            TextAlignmentOptions.Center, ShowcaseUiFactory.Ink);

            var cell = root.gameObject.AddComponent<AsyncPictureCell>();
            ShowcaseWiring.Write(cell, s =>
            {
                ShowcaseWiring.Prop(s, "_picture").objectReferenceValue = picture;
                ShowcaseWiring.Prop(s, "_pictureLabel").objectReferenceValue = pictureLabel;
                ShowcaseWiring.Prop(s, "_rowLabel").objectReferenceValue = row;
            });

            return root.gameObject;
        }

        private static GameObject BuildCell(string name, Color background, Color textColor, float fontSize,
                                            TextAlignmentOptions alignment, FontStyles style)
        {
            RectTransform root = ShowcaseUiFactory.Create(name, null);
            root.sizeDelta = new Vector2(400f, 80f);

            Image image = ShowcaseUiFactory.Image(root, background, raycast: true);

            // Transition None: the background colour IS the per-index data colour; a tint would hide it.
            var button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.targetGraphic = image;

            RectTransform labelRect = ShowcaseUiFactory.Area("Label", root, Vector2.zero, Vector2.one,
                                                             new Vector2(18f, 2f), new Vector2(-18f, -2f));
            TMP_Text label = ShowcaseUiFactory.Label(labelRect, "#0", fontSize, alignment, textColor);
            label.fontStyle = style;
            label.overflowMode = TextOverflowModes.Ellipsis;

            var cell = root.gameObject.AddComponent<ShowcaseCell>();
            ShowcaseWiring.Write(cell, s =>
            {
                ShowcaseWiring.Prop(s, "_background").objectReferenceValue = image;
                ShowcaseWiring.Prop(s, "_label").objectReferenceValue = label;
                ShowcaseWiring.Prop(s, "_button").objectReferenceValue = button;
            });

            return root.gameObject;
        }

        // Overwrites in place: the prefab keeps its GUID, and the scene is always rebuilt in the same
        // run, so no stale fileID survives in it.
        private static void Save(GameObject go, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(go, path, out bool saved);
            Object.DestroyImmediate(go);
            if (!saved) throw new InvalidOperationException($"[RecyclerShowcase] Could not save prefab '{path}'.");
        }
    }
}
