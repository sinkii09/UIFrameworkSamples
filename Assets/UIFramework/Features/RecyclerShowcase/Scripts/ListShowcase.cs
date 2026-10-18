using System.Collections.Generic;
using Sinkii09.UIFramework;
using UnityEngine;

namespace RecyclerShowcase
{
    /// <summary>
    /// v1.4 virtualized list: 100k uniform items, two prefab ids, ScrollToIndex alignment,
    /// RefreshIndex and SetItemCount on a scrolled list.
    /// </summary>
    public class ListShowcase : ShowcasePanel
    {
        private const int InitialCount = 100_000;
        private const int HeaderEvery = 25;
        private const int RowPrefab = 0;
        private const int HeaderPrefab = 1;

        [SerializeField] private RecyclerView _list;

        // Header membership is DATA, not a fixed rule, so "flip" can turn a visible row into a
        // header: RefreshIndex then has to re-rent from the OTHER prefab pool.
        private readonly HashSet<int> _flipped = new();
        private readonly System.Random _random = new(1337);
        private int _count = InitialCount;

        public override string Title => "1 · List 100k";

        protected override string Hint =>
            "<b>Virtualized list</b> (v1.4) — 100,000 items, only enough real cells to cover the viewport.\n" +
            "• Fling / wheel anywhere: <b>instances</b> stays flat. <b>shown</b> rises after the first scroll, then plateaus (hysteresis, not a leak).\n" +
            "• Dark SECTION rows only at multiples of 25 (prefab id 1). <b>Flip a visible type</b> swaps one row/header in place.\n" +
            "• Top / Middle·.5 / End·1: the target lands at leading edge / centre / trailing edge.\n" +
            "• +1000 / -1000 keep the scroll offset numerically (content may jump if you were near the end). Label colour must always match its #.";

        protected override void OnInitialize(ShowcaseToolbar toolbar)
        {
            _list.SetCellProvider(Bind);
            _list.SetItemCount(_count);

            toolbar.AddButton("Top", () => _list.ScrollToIndex(0));
            toolbar.AddButton("Middle · .5", () => _list.ScrollToIndex(_count / 2, 0.5f));
            toolbar.AddButton("End · 1", () => _list.ScrollToIndex(_count - 1, 1f));
            toolbar.AddButton("Random", () => _list.ScrollToIndex(_random.Next(_count), 0.5f));
            toolbar.AddButton("+1000", () => SetCount(_count + 1000));
            toolbar.AddButton("-1000", () => SetCount(Mathf.Max(0, _count - 1000)));
            toolbar.AddButton("Flip a visible type", FlipRandomVisible);
            toolbar.AddButton("Refresh all", _list.RefreshAll);
        }

        private bool IsHeader(int index) => (index % HeaderEvery == 0) ^ _flipped.Contains(index);

        private RecyclerCell Bind(int index)
        {
            if (IsHeader(index))
            {
                var header = _list.RentCell<ShowcaseCell>(HeaderPrefab);
                header.Bind($"SECTION · #{index:N0}", ShowcaseCell.HeaderColor);
                return header;
            }

            var row = _list.RentCell<ShowcaseCell>(RowPrefab);
            row.Bind($"#{index:N0}", ShowcaseCell.ColorFor(index));
            return row;
        }

        private void SetCount(int count)
        {
            _count = count;
            _list.SetItemCount(_count);
        }

        private void FlipRandomVisible()
        {
            IReadOnlyList<int> shown = _list.ShownIndices;
            if (shown.Count == 0) return;

            // The middle of the window: ShownIndices also holds the buffered cells just off-screen,
            // and flipping one of those would change nothing you can see.
            int index = shown[shown.Count / 2];
            if (!_flipped.Add(index)) _flipped.Remove(index);
            _list.RefreshIndex(index);
        }
    }
}
