using System;
using ColorStackSort.Logic;
using UnityEngine;

namespace ColorStackSort
{
    /// <summary>
    /// Maps a <see cref="ColorId"/> to a display colour. Serialized as a field on
    /// <see cref="BoardView"/> so the palette can be retuned per level pack without touching code.
    /// </summary>
    [Serializable]
    internal sealed class BallPalette
    {
        [SerializeField]
        private Color[] _colors =
        {
            new Color(0.91f, 0.30f, 0.34f), new Color(0.25f, 0.60f, 0.92f),
            new Color(0.36f, 0.78f, 0.42f), new Color(0.98f, 0.76f, 0.24f),
            new Color(0.65f, 0.40f, 0.85f), new Color(0.98f, 0.55f, 0.24f),
            new Color(0.30f, 0.80f, 0.79f), new Color(0.93f, 0.47f, 0.71f)
        };

        /// <summary>
        /// Wraps when a level uses more colours than the palette defines, so an over-provisioned
        /// level renders (with repeats) rather than throwing mid-game. <c>ColorId.Value</c> is a
        /// byte, so the index is never negative.
        /// </summary>
        internal Color TintFor(ColorId color) =>
            _colors != null && _colors.Length > 0 ? _colors[color.Value % _colors.Length] : Color.white;
    }
}
