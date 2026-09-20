using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sinkii09.UIFramework;
using UnityEngine;

namespace AstralChorus.Content
{
    // Thin fail-soft wrapper over the framework's IAssetLoader.
    public sealed class ContentAssetLoader : IContentAssetLoader
    {
        private readonly IAssetLoader _assets;

        public ContentAssetLoader(IAssetLoader assets) => _assets = assets;

        public async UniTask<T> TryLoadAsync<T>(string address, CancellationToken ct = default)
            where T : UnityEngine.Object
        {
            // An empty IconAddress is legal -- it means "this definition has no icon", not a fault,
            // so it must not log.
            if (string.IsNullOrEmpty(address)) return null;

            try
            {
                return await _assets.LoadAssetAsync<T>(address, ct);
            }
            // The filter is what keeps cancellation out of the swallow path: IAssetLoader
            // implementations call ct.ThrowIfCancellationRequested(), and an OperationCanceledException
            // caught here would be reported to the caller as "asset missing".
            catch (Exception e) when (!ct.IsCancellationRequested)
            {
                // Returning a bare null with no trace is how the notification host once rendered
                // nothing at all with no error to go on. The caller still gets null and still falls
                // back; the log is what makes the fallback explainable.
                Debug.LogWarning($"[Content] Asset '{address}' unavailable; using fallback. {e}");
                return null;
            }
        }

        public async UniTask UnloadAsync(string address, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(address)) return;

            try
            {
                await _assets.UnloadAsync(address, ct);
            }
            catch (Exception e) when (!ct.IsCancellationRequested)
            {
                Debug.LogWarning($"[Content] Unload of '{address}' failed. {e}");
            }
        }
    }
}
