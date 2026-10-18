using System;
using Sinkii09.UIFramework;

namespace RecyclerShowcase
{
    /// <summary>
    /// Forwards a <see cref="RecyclerView"/>'s prefetch hints (v3.8) to a <see cref="FakePictureLoader"/>.
    /// The distance itself is serialized on the view (the scene builder sets 600 px, about five
    /// 124 px rows past the realized cells); this only switches the hook on and off.
    /// </summary>
    public sealed class PicturePrefetcher
    {
        private readonly RecyclerView _list;
        private readonly FakePictureLoader _loader;

        // Cached once: SetPrefetchHandler is called on every toggle, and a method group allocates.
        private readonly Action<int, int> _onPrefetch;
        private readonly Action<int, int> _onCancel;

        public PicturePrefetcher(RecyclerView list, FakePictureLoader loader)
        {
            _list = list;
            _loader = loader;
            _onPrefetch = Prefetch;
            _onCancel = Cancel;
        }

        public bool Enabled { get; private set; }

        /// <summary>
        /// Off cancels every outstanding hint through the loader. Setting it on again — even when
        /// already on — cancels and re-announces, the documented way to re-issue after a cache clear.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            Enabled = enabled;
            if (enabled) _list.SetPrefetchHandler(_onPrefetch, _onCancel);
            else _list.SetPrefetchHandler(null);
        }

        private void Prefetch(int first, int count)
        {
            for (int key = first; key < first + count; key++) _loader.Prefetch(key);
        }

        private void Cancel(int first, int count)
        {
            for (int key = first; key < first + count; key++) _loader.CancelPrefetch(key);
        }
    }
}
