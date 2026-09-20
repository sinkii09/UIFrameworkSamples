using System.Threading;
using Cysharp.Threading.Tasks;

namespace AstralChorus.Content
{
    // Fail-soft art loading for content. A definition points at its art by string address, never by
    // a Sprite field, because a Sprite field on a core asset pointing into a pack drags that pack
    // into the build and defeats the profile.
    //
    // Async, unlike the synchronous `bool TryLoad(out T)` the architecture doc sketches: a
    // synchronous signature is honest only under Resources. Under Addressables it is a lie, and
    // fixing it later would change this interface for every caller.
    public interface IContentAssetLoader
    {
        // null means "not available" -- render a placeholder. Never throws, with one deliberate
        // exception: cancellation propagates, because a screen that was closed mid-load must not be
        // told its art is missing and go draw a placeholder into a dead view.
        UniTask<T> TryLoadAsync<T>(string address, CancellationToken ct = default)
            where T : UnityEngine.Object;

        UniTask UnloadAsync(string address, CancellationToken ct = default);
    }
}
