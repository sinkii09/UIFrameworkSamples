using Sinkii09.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RecyclerShowcase
{
    /// <summary>
    /// A row whose "picture" arrives asynchronously. The picture carries its own key printed on it,
    /// beside the row's index, so a stale or wrong picture is readable at a glance: "#120" next to
    /// "img #87" is the bug.
    /// </summary>
    public class AsyncPictureCell : RecyclerCell
    {
        private static readonly Color PlaceholderColor = new(0.55f, 0.57f, 0.62f);

        [SerializeField] private Image _picture;
        [SerializeField] private TMP_Text _pictureLabel;
        [SerializeField] private TMP_Text _rowLabel;

        /// <summary>Key of the picture on screen; -1 while the placeholder shows.</summary>
        public int ShownPictureKey { get; private set; } = -1;

        public void SetRow(int index) => _rowLabel.text = $"#{index:N0}";

        public void ShowPlaceholder()
        {
            _picture.color = PlaceholderColor;
            _pictureLabel.text = "loading...";
            ShownPictureKey = -1;
        }

        public void ShowPicture(int key, Color picture)
        {
            _picture.color = picture;
            _pictureLabel.text = $"img #{key:N0}";
            ShownPictureKey = key;
        }
    }
}
