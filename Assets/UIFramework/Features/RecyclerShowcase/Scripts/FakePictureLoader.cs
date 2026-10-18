using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RecyclerShowcase
{
    /// <summary>
    /// Stand-in for an Addressables or web image loader. Every load takes a random 50–600 ms, so
    /// loads finish out of order — the condition that turns "slow" into "wrong". A picture is just a
    /// colour derived from its key; what matters here is when it arrives, not what it looks like.
    ///
    /// <para><b>Prefetch (v3.8).</b> <see cref="Prefetch"/> starts ONE shared load per key, which a bind
    /// later joins instead of starting its own. A bind <see cref="Claim"/>s the key first — the view's
    /// rule is that calling the provider ends the hint — so a later cancel can never abort a load a
    /// cell is waiting on; <see cref="CancelPrefetch"/> only stops loads nobody claimed.</para>
    /// </summary>
    public sealed class FakePictureLoader
    {
        /// <summary>
        /// One shared load. Its result goes through a completion source because a row re-bound while
        /// the load runs (RefreshAll, "Append 1", scrolling back) is a second waiter on it: a plain
        /// UniTask may be awaited once, and <c>Preserve()</c> only lifts that AFTER completion — a
        /// second waiter while it runs throws "Already continuation registered" and the row stays on
        /// the placeholder. A recycled cell's <c>AttachExternalCancellation</c> keeps its slot, too.
        /// </summary>
        private sealed class SharedLoad
        {
            // Never disposed: no timer and no links, so there is nothing to release, and the load
            // observes the cancel a tick later (UniTask.Delay's cancelImmediately is false) — after
            // which nothing references it.
            public readonly CancellationTokenSource Cancel = new();
            public readonly UniTaskCompletionSource<Color> Result = new();
            public bool Claimed;
        }

        private readonly Dictionary<int, Color> _cache = new();
        private readonly Dictionary<int, SharedLoad> _shared = new();
        private readonly HashSet<int> _hinted = new();
        private readonly System.Random _random = new(7);

        public int CachedCount => _cache.Count;

        /// <summary>Keys announced and neither bound nor cancelled — mirrors the view's outstanding hints.</summary>
        public int Prefetching => _hinted.Count;

        public int PrefetchRequested { get; private set; }
        public int PrefetchCancelled { get; private set; }

        /// <summary>A shared load is running for this key (QA: sets up "re-bound while the load runs").</summary>
        public bool IsInFlight(int key) => _shared.ContainsKey(key);

        /// <summary>Synchronous hit — what lets a bind show a known picture without a placeholder frame.</summary>
        public bool TryGetCached(int key, out Color picture) => _cache.TryGetValue(key, out picture);

        /// <summary>A hint from the view: start (or keep) a shared load for a key that is not cached yet.</summary>
        public void Prefetch(int key)
        {
            if (!_hinted.Add(key)) return;
            PrefetchRequested++;

            if (_cache.ContainsKey(key)) return;

            // An entry claimed by a bind whose cell has since been recycled: no one waits on it any more.
            if (_shared.TryGetValue(key, out SharedLoad existing)) existing.Claimed = false;
            else Start(key);
        }

        /// <summary>The view no longer wants this key. Tolerates keys it has no record of.</summary>
        public void CancelPrefetch(int key)
        {
            if (!_hinted.Remove(key)) return;
            PrefetchCancelled++;

            if (!_shared.TryGetValue(key, out SharedLoad load) || load.Claimed) return;

            // Removed now, not when the cancelled task unwinds, so the next request never joins a dead load.
            _shared.Remove(key);
            load.Cancel.Cancel();
        }

        /// <summary>A bind for this key started: the hint ends here, and a shared load becomes the bind's.</summary>
        public void Claim(int key)
        {
            _hinted.Remove(key);
            if (_shared.TryGetValue(key, out SharedLoad load)) load.Claimed = true;
        }

        /// <summary>
        /// Joins the key's shared load when one is running, otherwise loads on its own (the v3.7 path).
        /// No cache check here: Naive mode relies on every load taking its random time.
        /// </summary>
        public UniTask<Color> LoadAsync(int key, CancellationToken cancellationToken)
            => _shared.TryGetValue(key, out SharedLoad load)
                ? load.Result.Task.AttachExternalCancellation(cancellationToken)
                : DelayThenStoreAsync(key, cancellationToken);

        /// <summary>
        /// Every shared load is cancelled too, so no prefetch lands in the cache after the clear. Loads a
        /// bind started on its own (a cold jump, a retry) still land — they are for rows on screen.
        /// Hints stay recorded — the view still holds them — so their loads stay stopped until the
        /// caller re-issues them (<c>SetPrefetchHandler</c> again; the showcase's Clear cache does). A
        /// cell waiting on a cancelled load reloads on its own.
        /// </summary>
        public void ClearCache()
        {
            // Copied and emptied before cancelling, so nothing a cancel triggers can see a stale entry.
            var loads = new List<SharedLoad>(_shared.Values);
            _shared.Clear();
            _cache.Clear();
            foreach (SharedLoad load in loads) load.Cancel.Cancel();
        }

        private void Start(int key)
        {
            var load = new SharedLoad();
            _shared[key] = load;
            RunSharedAsync(key, load).Forget();
        }

        private async UniTaskVoid RunSharedAsync(int key, SharedLoad load)
        {
            Color picture;
            try
            {
                picture = await DelayThenStoreAsync(key, load.Cancel.Token);
            }
            catch (OperationCanceledException exception)
            {
                // Whoever cancelled it has usually removed the entry already (and may have started a
                // replacement, which Forget leaves alone) — but a cancel from any other token must not
                // leave a dead load joinable either.
                Forget(key, load);
                load.Result.TrySetCanceled(exception.CancellationToken);
                return;
            }
            catch (Exception exception)
            {
                // A failed load must not stay joinable: every later LoadAsync would wait on it forever.
                Forget(key, load);
                load.Result.TrySetException(exception);
                return;
            }

            // Removed before the waiters resume: the picture is cached now, so nothing should join this.
            Forget(key, load);
            load.Result.TrySetResult(picture);
        }

        /// <summary>Only our own entry: a cancelled load has already been removed, maybe replaced.</summary>
        private void Forget(int key, SharedLoad load)
        {
            if (_shared.TryGetValue(key, out SharedLoad current) && current == load) _shared.Remove(key);
        }

        private async UniTask<Color> DelayThenStoreAsync(int key, CancellationToken cancellationToken)
        {
            await UniTask.Delay(_random.Next(50, 600), ignoreTimeScale: true, cancellationToken: cancellationToken);

            Color picture = ShowcaseCell.ColorFor(key);
            _cache[key] = picture;
            return picture;
        }
    }
}
