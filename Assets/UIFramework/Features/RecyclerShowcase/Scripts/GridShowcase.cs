using Sinkii09.UIFramework;
using UnityEngine;

namespace RecyclerShowcase
{
    /// <summary>
    /// v3.4 multi-column grid: runtime SetCrossAxisCount (including back to a list), grid + variable
    /// row heights, ScrollToIndex on a grid with a partial last row, and a horizontal grid.
    /// </summary>
    public class GridShowcase : ShowcasePanel
    {
        private const int VerticalCount = 10_000;
        private const int HorizontalCount = 3_000;
        private static readonly int[] ColumnChoices = { 1, 2, 3, 4, 6 };

        [SerializeField] private RecyclerView _vertical;
        [SerializeField] private RecyclerView _horizontal;

        private bool _variableRows;

        public override string Title => "3 · Grid";

        protected override string Hint =>
            "<b>Multi-column grid</b> (v3.4) — flat index, column = i % C. Label: <b>line</b> = i / C, <b>slot</b> = i % C.\n" +
            "• Top grid (vertical): numbers read left->right, then down. Bottom grid (horizontal): #0 TOP-left, #1 directly BELOW it, next line to the right.\n" +
            "• Column buttons drive both grids. After 1 col every tile must be full-width and centred (grid->list), after 6 all equal width.\n" +
            "• <b>Variable rows</b>: a line is as long as its longest item (height on top, width on the bottom grid) — label shows item and line size.\n" +
            "• Jump end at 3 / 6 cols: last line is partial (10,000 % 3 = 1, % 6 = 4), no empty-slot errors.";

        protected override void OnInitialize(ShowcaseToolbar toolbar)
        {
            _vertical.SetCellProvider(index => BindTile(_vertical, VerticalCount, index));
            _vertical.SetItemCount(VerticalCount);
            _horizontal.SetCellProvider(index => BindTile(_horizontal, HorizontalCount, index));
            _horizontal.SetItemCount(HorizontalCount);

            foreach (int columns in ColumnChoices)
                toolbar.AddButton($"{columns} col", () => SetColumns(columns));

            toolbar.AddButton("Variable rows", ToggleVariableRows);
            toolbar.AddButton("Jump middle", () => JumpBoth(0.5f, 0.5f));
            toolbar.AddButton("Jump end", () => JumpBoth(1f, 1f));
            toolbar.AddButton("Top", () => JumpBoth(0f, 0f));
        }

        private static float ItemSize(int index) => 100f + (ShowcaseHash.Of(index) % 5) * 30f;

        private RecyclerCell BindTile(RecyclerView view, int count, int index)
        {
            int columns = view.CrossAxisCount;
            string sizes = "";
            if (_variableRows)
                sizes = $"\nitem {ItemSize(index):0} · line {RowSize(index, columns, count):0}";

            var tile = view.RentCell<ShowcaseCell>(0);
            tile.Bind($"#{index:N0}\n<size=65%>line {index / columns} · slot {index % columns}{sizes}</size>",
                      ShowcaseCell.ColorFor(index));
            return tile;
        }

        // What the view itself declares for a row: the tallest item in it. Shown so a row that is
        // taller than the tile's own size reads as correct, not as a gap.
        private static float RowSize(int index, int columns, int count)
        {
            int first = index / columns * columns;
            int last = Mathf.Min(first + columns, count);
            float max = 0f;
            for (int i = first; i < last; i++) max = Mathf.Max(max, ItemSize(i));
            return max;
        }

        private void SetColumns(int columns)
        {
            _vertical.SetCrossAxisCount(columns);
            _horizontal.SetCrossAxisCount(columns);
        }

        private void ToggleVariableRows()
        {
            _variableRows = !_variableRows;
            _vertical.SetItemSizeProvider(_variableRows ? ItemSize : null);
            _horizontal.SetItemSizeProvider(_variableRows ? ItemSize : null);
        }

        private void JumpBoth(float position, float alignment)
        {
            _vertical.ScrollToIndex(Mathf.RoundToInt((VerticalCount - 1) * position), alignment);
            _horizontal.ScrollToIndex(Mathf.RoundToInt((HorizontalCount - 1) * position), alignment);
        }
    }
}
