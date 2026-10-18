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
    /// <see cref="RecyclerCell.BindCancellationToken"/>. v3.8 adds prefetch (Correct mode only): the
    /// list announces upcoming rows so their pictures are cached before they bind.
    /// </summary>
    public partial class AsyncPicturesShowcase : ShowcasePanel
    {
        private const float SampleSeconds = 0.2f;

        [SerializeField] private RecyclerView _list;
        [SerializeField] private TMP_Text _status;

        private readonly FakePictureLoader _loader = new();
        private readonly System.Random _random = new(42);
        private PicturePrefetcher _prefetch;
        private CancellationTokenSource _naiveLoads = new();
        private Action<RecyclerCell> _countWrong;
        private int _count = 2_000;
        private bool _naive;
        private bool _prefetchWanted;
        private bool _throwNextBind;
        private int _wrongNow;
        private int _wrongWorst;
        private int _placeholders;
        private float _nextSample;

        public override string Title => "5 · Async";

        /// <summary>Rows whose picture belongs to another row, at the last sample. QA reads this.</summary>
        public int WrongNow => _wrongNow;

        public bool Naive => _naive;

        /// <summary>Correct-mode binds that missed the cache and showed "loading...". QA reads this.</summary>
        public int Placeholders => _placeholders;

        public bool PrefetchOn => _prefetch != null && _prefetch.Enabled;

        public FakePictureLoader Loader => _loader;

        protected override string Hint =>
            "<b>Async pictures</b> (v3.7, prefetch v3.8) — every row loads its picture in 50–600 ms, out of order, like Addressables or a web image.\n" +
            "• The row's <b>#N</b> must match its picture's <b>img #N</b>; <i>wrong now</i> counts rows where they differ.\n" +
            "• <b>Naive</b> (no clear, no cancel, no check): fling — recycled rows keep the old picture, slow loads land on the wrong row, <i>worst</i> climbs.\n" +
            "• <b>Correct</b> (placeholder + BindCancellationToken + cache): only 'loading...' or the right picture. Scroll back: cached rows appear instantly.\n" +
            "• <b>Prefetch</b> (Correct only): the list announces the next ~5 rows before they bind, so a steady scroll stops adding <i>placeholders</i>. <i>hinted</i> = announced, not yet bound or cancelled; off cancels them all.\n" +
            "• <b>Provider throws once</b>: a bind fails after RentCell — <b>instances</b> on the stats line must not grow.";

        protected override void OnInitialize(ShowcaseToolbar toolbar)
        {
            _countWrong = CountWrong;
            _prefetch = new PicturePrefetcher(_list, _loader);
            _list.SetCellProvider(Bind);
            _list.SetItemCount(_count);

            toolbar.AddButton("Naive / Correct", ToggleMode);
            toolbar.AddButton("Prefetch on/off", TogglePrefetch);
            toolbar.AddButton("Clear cache", ClearCache);
            toolbar.AddButton("Jump random", () => _list.ScrollToIndex(_random.Next(_count), 0.5f));
            toolbar.AddButton("Append 1", () => _list.SetItemCount(++_count));
            toolbar.AddButton("Provider throws once", ThrowOnce);
            toolbar.AddButton("Reset counters", () => { _wrongWorst = 0; _placeholders = 0; });
        }

        private RecyclerCell Bind(int index)
        {
            // First, whatever happens next: the view counts a provider call as the end of the hint.
            _loader.Claim(index);
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

        private void OnDestroy() => _naiveLoads.Dispose();

        private void ToggleMode()
        {
            _naiveLoads.Cancel();
            _naiveLoads.Dispose();
            _naiveLoads = new CancellationTokenSource();

            _naive = !_naive;
            _loader.ClearCache(); // both modes start cold — a warm cache would hide the race
            if (_naive) _prefetch.SetEnabled(false); // before the re-bind: Naive never prefetches
            _wrongWorst = 0;
            _list.RefreshAll();
            if (!_naive) _prefetch.SetEnabled(_prefetchWanted); // after it: announce against the new window
        }

        private void TogglePrefetch()
        {
            _prefetchWanted = !_prefetchWanted;
            _prefetch.SetEnabled(_prefetchWanted && !_naive);
        }

        private void ClearCache()
        {
            _loader.ClearCache();

            // The clear also stopped the loads behind outstanding hints: re-issue them.
            if (_prefetch.Enabled) _prefetch.SetEnabled(true);
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
            string prefetch = PrefetchOn ? "<color=#8AF0A8>ON</color>" : _prefetchWanted ? "on (Naive: idle)" : "OFF";
            _status.text = $"mode <b>{mode}</b>   wrong now <b>{_wrongNow}</b>   worst <b>{_wrongWorst}</b>   " +
                           $"cached <b>{_loader.CachedCount}</b>\n" +
                           $"prefetch <b>{prefetch}</b>   hinted <b>{_loader.Prefetching}</b>   " +
                           $"placeholders <b>{_placeholders}</b>   v <b>{_list.ScrollVelocity:F0}</b>";
        }

        private void CountWrong(RecyclerCell cell)
        {
            var row = (AsyncPictureCell)cell;
            if (row.ShownPictureKey >= 0 && row.ShownPictureKey != row.Index) _wrongNow++;
        }
    }
}
