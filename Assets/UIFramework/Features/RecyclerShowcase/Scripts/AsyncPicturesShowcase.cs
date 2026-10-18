using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sinkii09.UIFramework;
using TMPro;
using UnityEngine;

namespace RecyclerShowcase
{
    /// <summary>
    /// v3.7 async cells: the "recycled row shows the previous item's picture" bug beside its fix.
    /// Naive binding never clears, never cancels, never checks. Correct binding shows a cache hit
    /// synchronously, otherwise clears to a placeholder and loads under the cell's
    /// <see cref="RecyclerCell.BindCancellationToken"/>.
    /// </summary>
    public class AsyncPicturesShowcase : ShowcasePanel
    {
        private const float SampleSeconds = 0.2f;

        [SerializeField] private RecyclerView _list;
        [SerializeField] private TMP_Text _status;

        private readonly FakePictureLoader _loader = new();
        private readonly System.Random _random = new(42);
        private CancellationTokenSource _naiveLoads = new();
        private Action<RecyclerCell> _countWrong;
        private int _count = 2_000;
        private bool _naive;
        private bool _throwNextBind;
        private int _wrongNow;
        private int _wrongWorst;
        private float _nextSample;

        public override string Title => "5 · Async";

        /// <summary>Rows whose picture belongs to another row, at the last sample. QA reads this.</summary>
        public int WrongNow => _wrongNow;

        public bool Naive => _naive;

        protected override string Hint =>
            "<b>Async pictures</b> (v3.7) — every row loads its picture in 50–600 ms, out of order, like Addressables or a web image.\n" +
            "• The row's <b>#N</b> must match its picture's <b>img #N</b>; the status line counts rows where they differ.\n" +
            "• <b>Naive</b> (no clear, no cancel, no check): fling — recycled rows keep the old picture, slow loads land on the wrong row, <i>worst</i> climbs.\n" +
            "• <b>Correct</b> (placeholder + BindCancellationToken + cache): only 'loading...' or the right picture, <i>wrong now</i> stays 0. Scroll back: cached rows appear instantly.\n" +
            "• <b>Provider throws once</b>: a bind fails after RentCell — <b>instances</b> on the stats line must not grow.";

        protected override void OnInitialize(ShowcaseToolbar toolbar)
        {
            _countWrong = CountWrong;
            _list.SetCellProvider(Bind);
            _list.SetItemCount(_count);

            toolbar.AddButton("Naive / Correct", ToggleMode);
            toolbar.AddButton("Clear cache", _loader.ClearCache);
            toolbar.AddButton("Jump random", () => _list.ScrollToIndex(_random.Next(_count), 0.5f));
            toolbar.AddButton("Append 1", () => _list.SetItemCount(++_count));
            toolbar.AddButton("Provider throws once", ThrowOnce);
            toolbar.AddButton("Reset worst", () => _wrongWorst = 0);
        }

        private RecyclerCell Bind(int index)
        {
            var cell = _list.RentCell<AsyncPictureCell>(0);

            if (_throwNextBind)
            {
                // After RentCell, the way a real provider fails on bad data. The view has to hand the
                // rented cell back itself — before v3.7 it stayed on screen, tracked by nothing.
                _throwNextBind = false;
                throw new InvalidOperationException($"provider failed on purpose for #{index}");
            }

            cell.SetRow(index);
            if (_naive) NaiveLoadAsync(cell, index).Forget();
            else BindCorrect(cell, index);
            return cell;
        }

        // The bug, on purpose: nothing cleared, so a recycled row keeps the last item's picture until
        // this lands; nothing cancelled or checked, so a slow load for a previous item lands last and
        // wins. The panel-wide token is demo hygiene only — it stops naive loads still in flight from
        // landing after a switch to Correct and being blamed on it.
        private async UniTaskVoid NaiveLoadAsync(AsyncPictureCell cell, int key)
        {
            Color picture;
            try
            {
                picture = await _loader.LoadAsync(key, _naiveLoads.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (cell != null) cell.ShowPicture(key, picture); // null once play mode has ended
        }

        private void BindCorrect(AsyncPictureCell cell, int key)
        {
            // Synchronous on a hit, so a known picture never flashes the placeholder for a frame.
            if (_loader.TryGetCached(key, out Color cached))
            {
                cell.ShowPicture(key, cached);
                return;
            }

            // Cleared now, not when the load lands: a cell re-rented this frame is still on screen
            // showing the previous item's picture.
            cell.ShowPlaceholder();
            CorrectLoadAsync(cell, key, cell.BindCancellationToken).Forget();
        }

        private async UniTaskVoid CorrectLoadAsync(AsyncPictureCell cell, int key, CancellationToken bindToken)
        {
            Color picture;
            try
            {
                picture = await _loader.LoadAsync(key, bindToken);
            }
            catch (OperationCanceledException)
            {
                return; // the cell moved on to another item: expected, not an error
            }

            // Not cancelled means not recycled, so the cell still represents this key.
            if (bindToken.IsCancellationRequested) return;
            cell.ShowPicture(key, picture);
        }

        private void OnDestroy() => _naiveLoads.Dispose();

        private void ToggleMode()
        {
            _naiveLoads.Cancel();
            _naiveLoads.Dispose();
            _naiveLoads = new CancellationTokenSource();

            _naive = !_naive;
            _loader.ClearCache(); // both modes start cold — a warm cache would hide the race
            _wrongWorst = 0;
            _list.RefreshAll();
        }

        private void ThrowOnce()
        {
            IReadOnlyList<int> shown = _list.ShownIndices;
            if (shown.Count == 0) return;

            _throwNextBind = true;
            try
            {
                _list.RefreshIndex(shown[shown.Count / 2]);
            }
            catch (InvalidOperationException exception)
            {
                Debug.Log($"[RecyclerShowcase] {exception.Message} — 'instances' must not grow.");
            }
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextSample) return;
            _nextSample = Time.unscaledTime + SampleSeconds;

            _wrongNow = 0;
            _list.ForEachShownCell(_countWrong);
            if (_wrongNow > _wrongWorst) _wrongWorst = _wrongNow;

            string mode = _naive ? "<color=#FF8A8A>NAIVE</color>" : "<color=#8AF0A8>CORRECT</color>";
            _status.text = $"mode <b>{mode}</b>   wrong now <b>{_wrongNow}</b>   worst <b>{_wrongWorst}</b>   " +
                           $"cached <b>{_loader.CachedCount}</b>";
        }

        private void CountWrong(RecyclerCell cell)
        {
            var row = (AsyncPictureCell)cell;
            if (row.ShownPictureKey >= 0 && row.ShownPictureKey != row.Index) _wrongNow++;
        }
    }
}
