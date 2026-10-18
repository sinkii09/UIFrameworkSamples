using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RecyclerShowcase.Editor
{
    /// <summary>Small uGUI building blocks shared by the prefab and scene builders.</summary>
    internal static class ShowcaseUiFactory
    {
        internal static readonly Color Ink = new(0.10f, 0.11f, 0.14f);
        internal static readonly Color Panel = new(0.13f, 0.14f, 0.19f);
        internal static readonly Color ButtonColor = new(0.25f, 0.28f, 0.38f);

        internal static RectTransform Create(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        /// <summary>Child spanning anchor fractions, inset by pixel offsets (min = left/bottom, max = right/top).</summary>
        internal static RectTransform Area(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
                                           Vector2 offsetMin = default, Vector2 offsetMax = default)
        {
            RectTransform rect = Create(name, parent);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        internal static RectTransform Stretch(string name, Transform parent, float inset = 0f) =>
            Area(name, parent, Vector2.zero, Vector2.one, new Vector2(inset, inset), new Vector2(-inset, -inset));

        /// <summary>A band hugging the top edge of its parent.</summary>
        internal static RectTransform TopBand(string name, Transform parent, float fromTop, float height, float sideInset) =>
            Area(name, parent, new Vector2(0f, 1f), Vector2.one,
                 new Vector2(sideInset, -fromTop - height), new Vector2(-sideInset, -fromTop));

        internal static Image Image(RectTransform rect, Color color, bool raycast = false)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        // Font left unassigned on purpose: TMP resolves the project default on AddComponent.
        internal static TMP_Text Label(RectTransform rect, string text, float size, TextAlignmentOptions alignment, Color color)
        {
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = color;
            label.richText = true;
            label.raycastTarget = false;
            return label;
        }

        internal static Button Button(RectTransform rect, string caption, float fontSize)
        {
            Image background = Image(rect, ButtonColor, true);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;

            Label(Stretch("Label", rect, 6f), caption, fontSize, TextAlignmentOptions.Center, Color.white);
            return button;
        }
    }
}
