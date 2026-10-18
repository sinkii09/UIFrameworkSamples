using System.Collections.Generic;
using Sinkii09.UIFramework;
using UnityEngine;

namespace RecyclerShowcase
{
    /// <summary>
    /// v1.5 variable cell size: per-index declared heights, separators of a different prefab AND a
    /// different height, tap-to-expand through RefreshSizes, and switching back to uniform.
    /// </summary>
    public class VariableSizeShowcase : ShowcasePanel
    {
        private const int Count = 5_000;
        private const int SeparatorEvery = 10;
        private const float SeparatorHeight = 40f;
        private const float ExpandBy = 200f;
        private const int RowPrefab = 0;
        private const int SeparatorPrefab = 1;

        [SerializeField] private RecyclerView _list;

        // The provider is the single source of truth for size; expansion lives in our own data and
        // RefreshSizes re-asks — there is deliberately no per-index size setter on the view.
        private readonly HashSet<int> _expanded = new();
        private bool _variable = true;

        public override string Title => "2 · Variable size";

        protected override string Hint =>
            "<b>Variable cell size</b> (v1.5 + RefreshSizes) — heights are DECLARED, never measured.\n" +
            "• Rows are 60–260 px, separators 40 px (a different prefab). No gaps, no overlaps, nothing shifts while scrolling.\n" +
            "• <b>Tap a row</b> to expand +200 px: rows below move down, the list does NOT jump. Tap again to collapse.\n" +
            "• <b>Variable / Uniform</b>: uniform ignores the provider (tap does nothing then). Switch back restores every height.\n" +
            "• Collapse all keeps the offset numerically — content under you changes, that is expected.";

        protected override void OnInitialize(ShowcaseToolbar toolbar)
        {
            _list.SetItemSizeProvider(SizeOf);
            _list.SetCellProvider(Bind);
            _list.SetItemCount(Count);

            toolbar.AddButton("Variable / Uniform", ToggleMode);
            toolbar.AddButton("Collapse all", CollapseAll);
            toolbar.AddButton("Jump #2500 · .5", () => _list.ScrollToIndex(2500, 0.5f));
            toolbar.AddButton("Top", () => _list.ScrollToIndex(0));
        }

        private static bool IsSeparator(int index) => index % SeparatorEvery == 0;

        // Deterministic pseudo-random spread so every run (and every rebind) declares the same height.
        private static float BaseHeight(int index) => 60f + (ShowcaseHash.Of(index) % 9) * 25f;

        private float SizeOf(int index)
        {
            if (IsSeparator(index)) return SeparatorHeight;
            return BaseHeight(index) + (_expanded.Contains(index) ? ExpandBy : 0f);
        }

        private RecyclerCell Bind(int index)
        {
            if (IsSeparator(index))
            {
                var separator = _list.RentCell<ShowcaseCell>(SeparatorPrefab);
                separator.Bind($"— section {index / SeparatorEvery} —", ShowcaseCell.HeaderColor);
                return separator;
            }

            bool expanded = _expanded.Contains(index);
            string height = _variable ? $"{SizeOf(index):0} px" : "uniform";
            string action = !_variable ? "tap does nothing in Uniform mode" : expanded ? "tap to collapse" : "tap to expand";
            var row = _list.RentCell<ShowcaseCell>(RowPrefab);
            row.Bind($"#{index:N0}   declared {height}{(expanded ? "   (expanded)" : "")}\n<size=70%>{action}</size>",
                     ShowcaseCell.ColorFor(index), ToggleExpanded);
            return row;
        }

        private void ToggleExpanded(int index)
        {
            if (!_variable || IsSeparator(index)) return;

            if (!_expanded.Add(index)) _expanded.Remove(index);
            _list.RefreshSizes();
        }

        // SetItemSizeProvider rebinds every shown cell, which also refreshes the mode-dependent label.
        private void ToggleMode()
        {
            _variable = !_variable;
            _list.SetItemSizeProvider(_variable ? SizeOf : null);
        }

        private void CollapseAll()
        {
            _expanded.Clear();

            // RefreshSizes is a no-op without a size provider, so in Uniform mode the labels would keep
            // saying "(expanded)" — a stale label is exactly what this demo teaches you to read as a bug.
            if (_variable) _list.RefreshSizes();
            else _list.RefreshAll();
        }
    }
}
