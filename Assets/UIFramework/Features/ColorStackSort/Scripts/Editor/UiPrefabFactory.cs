using UnityEngine;
using UnityEngine.UI;

namespace ColorStackSort.Editor
{
    /// <summary>Small builders shared by <see cref="ColorStackSortPrefabBuilder"/>.</summary>
    internal static class UiPrefabFactory
    {
        /// <summary>Unity's built-in round sprite — gives real balls without shipping any art.</summary>
        internal const string KnobSprite = "UI/Skin/Knob.psd";

        /// <summary>Unity's built-in rounded panel sprite, used for the tube body.</summary>
        internal const string PanelSprite = "UI/Skin/Background.psd";

        internal static Sprite BuiltIn(string path) =>
            UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>(path);

        /// <summary>
        /// Creates a RectTransform GameObject with POINT anchors (centre/centre).
        /// Point anchors are load-bearing for balls: with stretch anchors the rect is re-derived
        /// from whatever parent it currently has, so a ball would visibly resize the moment it is
        /// reparented onto the travel overlay mid-move.
        /// </summary>
        internal static RectTransform CreatePoint(string name, Transform parent, Vector2 size)
        {
            var rect = Create(name, parent);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;

            return rect;
        }

        /// <summary>Creates a RectTransform GameObject stretched to fill its parent.</summary>
        internal static RectTransform CreateStretch(string name, Transform parent)
        {
            var rect = Create(name, parent);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return rect;
        }

        /// <summary>
        /// A TextMeshPro label. The font is left unassigned on purpose — TMP resolves the project
        /// default when the component is added, and hard-coding an asset path here would break on
        /// any project that renames it.
        /// </summary>
        internal static TMPro.TMP_Text AddLabel(
            RectTransform rect, string text, float size, TMPro.TextAlignmentOptions alignment)
        {
            var label = rect.gameObject.AddComponent<TMPro.TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = false;

            return label;
        }

        /// <summary>A labelled button: image background, Button component, centred caption child.</summary>
        internal static Button AddButton(RectTransform rect, string caption, Color tint)
        {
            var background = AddImage(rect, BuiltIn(PanelSprite), tint, true);

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;

            var labelRect = CreateStretch("Label", rect);
            AddLabel(labelRect, caption, 34f, TMPro.TextAlignmentOptions.Center);

            return button;
        }

        internal static Image AddImage(RectTransform rect, Sprite sprite, Color color, bool sliced)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            if (sliced && sprite != null) image.type = Image.Type.Sliced;

            return image;
        }

        /// <summary>
        /// Writes a private <c>[SerializeField]</c> via SerializedObject — the only way to reach one
        /// from outside the class.
        /// </summary>
        internal static void SetReference(Object target, string fieldName, Object value)
        {
            var serialized = new UnityEditor.SerializedObject(target);
            var property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                Debug.LogError($"[ColorStackSort] {target.GetType().Name} has no field '{fieldName}'.");
                return;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RectTransform Create(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one;

            return (RectTransform)go.transform;
        }
    }
}
