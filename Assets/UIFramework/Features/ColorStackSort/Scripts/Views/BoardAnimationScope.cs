using System;
using System.Threading;

namespace ColorStackSort
{
    /// <summary>
    /// The cancellation token every board animation runs under: alive for one showing of the board,
    /// cancelled the moment the view is disabled or destroyed.
    /// <para>
    /// Extracted so the renewal rule below lives in one named place instead of being spread across
    /// three MonoBehaviour callbacks, where it was easy to read as boilerplate and delete.
    /// </para>
    /// </summary>
    internal sealed class BoardAnimationScope : IDisposable
    {
        private readonly CancellationToken _linkedTo;
        private CancellationTokenSource _cts;

        /// <param name="linkedTo">Usually the view's <c>destroyCancellationToken</c>.</param>
        internal BoardAnimationScope(CancellationToken linkedTo) => _linkedTo = linkedTo;

        internal CancellationToken Token => _cts?.Token ?? new CancellationToken(true);

        /// <summary>
        /// True when the token can no longer carry an animation.
        /// <para>
        /// Checked on enable, because <c>OnDisable</c> cancels and only the show path renews. If
        /// anything toggles this GameObject outside the framework's Show/Hide path — a parent
        /// canvas, a layer root — the show path never runs and the token would stay cancelled
        /// forever: every later move is killed on its first registration, the busy flag clears
        /// immediately so taps stay live, and the board keeps mutating with nothing rendering.
        /// Silent and unrecoverable.
        /// </para>
        /// </summary>
        internal bool IsStale => _cts == null || _cts.IsCancellationRequested;

        internal void Renew()
        {
            _cts?.Dispose();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(_linkedTo);
        }

        internal void Cancel() => _cts?.Cancel();

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }
}
