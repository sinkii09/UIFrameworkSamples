using System.Collections.Generic;
using Sinkii09.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RecyclerShowcase
{
    /// <summary>
    /// One live line per RecyclerView — the proof that virtualization works. "instances" counts every
    /// cell GameObject under the content (shown + pooled); it must stay flat no matter how far the
    /// list is scrolled. "shown" grows after the first scroll and then plateaus: that is the
    /// create/recycle hysteresis, not a leak.
    /// </summary>
    public class RecyclerStatsLabel : MonoBehaviour
    {
        private const float RefreshSeconds = 0.2f;

        [SerializeField] private RecyclerView _target;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private string _caption;

        private RectTransform _content;
        private float _nextRefresh;

        private void Update()
        {
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + RefreshSeconds;

            if (_content == null) _content = _target.GetComponent<ScrollRect>().content;
            _label.text = $"<b><color=#8FB4FF>{_caption}</color></b>   {Describe(_target, _content)}";
        }

        private static string Describe(RecyclerView view, RectTransform content)
        {
            IReadOnlyList<int> shown = view.ShownIndices;
            string range = shown.Count == 0 ? "—" : $"{shown[0]}–{shown[shown.Count - 1]}";

            int active = 0;
            for (int i = 0; i < content.childCount; i++)
                if (content.GetChild(i).gameObject.activeSelf) active++;

            return $"items <b>{view.ItemCount:N0}</b>   shown <b>{shown.Count}</b> [{range}]   " +
                   $"instances <b>{content.childCount}</b> ({active} active, {content.childCount - active} pooled)   " +
                   $"cols <b>{view.CrossAxisCount}</b>";
        }
    }
}
