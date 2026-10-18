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
    /// </summary>
    public sealed class FakePictureLoader
    {
        private readonly Dictionary<int, Color> _cache = new();
        private readonly System.Random _random = new(7);

        public int CachedCount => _cache.Count;

        /// <summary>Synchronous hit — what lets a bind show a known picture without a placeholder frame.</summary>
        public bool TryGetCached(int key, out Color picture) => _cache.TryGetValue(key, out picture);

        public async UniTask<Color> LoadAsync(int key, CancellationToken cancellationToken)
        {
            await UniTask.Delay(_random.Next(50, 600), ignoreTimeScale: true, cancellationToken: cancellationToken);

            Color picture = ShowcaseCell.ColorFor(key);
            _cache[key] = picture;
            return picture;
        }

        public void ClearCache() => _cache.Clear();
    }
}
