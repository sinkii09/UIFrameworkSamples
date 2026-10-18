using System;
using Sinkii09.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RecyclerShowcase
{
    /// <summary>
    /// The one cell type every showcase uses. Prefab id 0 is a "row" look, id 1 a "header" look —
    /// same class, so a provider can rent either with <c>RentCell&lt;ShowcaseCell&gt;(id)</c>.
    /// The background colour is derived from the index, so a cell bound to the wrong index shows
    /// up as a colour that does not match its label.
    /// </summary>
    public class ShowcaseCell : RecyclerCell
    {
        /// <summary>Background of the id-1 prefab (headers, separators) — fixed, unlike row colours.</summary>
        public static readonly Color HeaderColor = new(0.16f, 0.18f, 0.26f);

        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Button _button;

        private Action<int> _onClick;

        private void Awake() => _button.onClick.AddListener(HandleClick);

        public void Bind(string text, Color background, Action<int> onClick = null)
        {
            _label.text = text;
            _background.color = background;
            _onClick = onClick;
        }

        // A recycled cell is rebound to another index; a handler surviving here would fire for the
        // wrong item on the next tap.
        public override void OnRecycled() => _onClick = null;

        private void HandleClick() => _onClick?.Invoke(Index);

        /// <summary>Golden-ratio hue walk: neighbours always differ, and the colour is stable per index.</summary>
        public static Color ColorFor(int index)
        {
            float hue = (index * 0.618034f) % 1f;
            return Color.HSVToRGB(hue, 0.35f, 0.95f);
        }
    }
}
