using Sinkii09.UIFramework;
using UnityEngine;

namespace RecyclerShowcase
{
    /// <summary>
    /// All four ScrollDirections side by side. Direction is init-only on RecyclerView (no setter),
    /// hence four instances rather than one switched view.
    /// </summary>
    public class DirectionsShowcase : ShowcasePanel
    {
        private const int Count = 200;

        [Tooltip("Order: TopToBottom, BottomToTop, LeftToRight, RightToLeft.")]
        [SerializeField] private RecyclerView[] _lists;

        public override string Title => "4 · Directions";

        protected override string Hint =>
            "<b>Scroll directions</b> — the same 200 items, four layouts.\n" +
            "• #0 must sit at: TopToBottom -> <b>top</b>, BottomToTop -> <b>bottom</b>, LeftToRight -> <b>left</b>, RightToLeft -> <b>right</b>.\n" +
            "• Numbers increase AWAY from that edge; scrolling reveals higher numbers.\n" +
            "• Jump #150 puts #150 at each list's leading edge (the edge #0 starts at); Jump end puts #199 at the far edge.";

        protected override void OnInitialize(ShowcaseToolbar toolbar)
        {
            foreach (RecyclerView list in _lists)
            {
                RecyclerView view = list;
                view.SetCellProvider(index => Bind(view, index));
                view.SetItemCount(Count);
            }

            toolbar.AddButton("Jump #150 (all)", () => JumpAll(150, 0f));
            toolbar.AddButton("Jump #0 (all)", () => JumpAll(0, 0f));
            toolbar.AddButton("Jump end (all)", () => JumpAll(Count - 1, 1f));
        }

        private static RecyclerCell Bind(RecyclerView view, int index)
        {
            var cell = view.RentCell<ShowcaseCell>(0);
            cell.Bind($"#{index}", ShowcaseCell.ColorFor(index));
            return cell;
        }

        private void JumpAll(int index, float alignment)
        {
            foreach (RecyclerView list in _lists) list.ScrollToIndex(index, alignment);
        }
    }
}
