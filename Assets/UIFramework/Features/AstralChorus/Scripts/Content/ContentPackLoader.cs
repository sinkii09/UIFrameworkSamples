using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sinkii09.UIFramework;
using UnityEngine;

namespace AstralChorus.Content
{
    // Builds the registry at boot: always-loaded pack first, then whatever the profile enables.
    //
    // Packs are fetched BY KEY rather than by scanning a folder, because IAssetLoader cannot express
    // a folder scan and because by-key works unchanged once packs move to Addressables. The cost is
    // that the manifest's file name becomes its identity, which is why PackId is checked against the
    // key it was requested under.
    //
    // Does NOT run itself. The game's boot sequence awaits it, the same way UIViewPreloader is
    // awaited, because "the registry is built before any view reads it" is an ordering rule that
    // belongs in visible boot code rather than in whatever order a container happens to start things.
    public sealed class ContentPackLoader
    {
        private readonly ContentRegistry _registry;
        private readonly ContentBuildProfile _profile;
        private readonly IAssetLoader _assets;

        public ContentPackLoader(ContentRegistry registry, ContentBuildProfile profile, IAssetLoader assets)
        {
            _registry = registry;
            _profile = profile;
            _assets = assets;
        }

        public async UniTask BuildAsync(CancellationToken ct = default)
        {
            if (_registry.State == ContentRegistryState.Building)
            {
                Debug.LogError("[Content] BuildAsync called while a build is already running; ignored.");
                return;
            }

            if (_registry.State == ContentRegistryState.Frozen)
            {
                Debug.LogError("[Content] BuildAsync called after the registry was frozen; ignored. " +
                               "Changing the profile requires a restart.");
                return;
            }

            _registry.BeginBuild();

            try
            {
                // Named explicitly rather than let through: an empty id would otherwise surface as
                // "Pack '' failed to load", which reads like a missing asset instead of a profile
                // that never named the main content.
                if (string.IsNullOrEmpty(_profile.AlwaysLoadedPackId))
                {
                    Debug.LogError("[Content] The profile names no always-loaded pack, so the main " +
                                   "content will not be loaded at all.");
                }
                else
                {
                    await LoadPackAsync(_profile.AlwaysLoadedPackId, ct);
                }

                foreach (string packId in EnabledPacks())
                {
                    await LoadPackAsync(packId, ct);
                }

                _registry.Freeze();
            }
            // catch/rethrow rather than finally: finally cannot tell success from failure, and an
            // unexpected throw that left the state on Building would strand every later read behind
            // the not-readable error forever, with no way to retry.
            catch (Exception)
            {
                _registry.Fail();
                throw;
            }
        }

        private IEnumerable<string> EnabledPacks()
        {
            var seen = new HashSet<string>();

            foreach (string packId in _profile.EnabledPackIds)
            {
                if (string.IsNullOrEmpty(packId))
                {
                    Debug.LogError("[Content] Empty entry in EnabledPackIds; skipped.");
                    continue;
                }

                if (packId == _profile.AlwaysLoadedPackId)
                {
                    // Listing it is harmless only because of this check. Left unguarded it would
                    // load the main content twice and report every one of its ids as a duplicate.
                    Debug.LogError($"[Content] '{packId}' is the always-loaded pack and must not appear " +
                                   "in EnabledPackIds; skipped.");
                    continue;
                }

                if (!seen.Add(packId))
                {
                    Debug.LogError($"[Content] Duplicate '{packId}' in EnabledPackIds; skipped.");
                    continue;
                }

                yield return packId;
            }
        }

        private async UniTask LoadPackAsync(string packId, CancellationToken ct)
        {
            ContentPackManifest manifest;

            try
            {
                manifest = await _assets.LoadAssetAsync<ContentPackManifest>(_profile.ManifestKey(packId), ct);
            }
            // The filter is load-bearing. Loaders call ct.ThrowIfCancellationRequested(), so a bare
            // catch here would absorb a cancelled boot, keep loading the remaining packs, and then
            // Freeze() a half-built registry -- marking it COMPLETE. A broken pack must be skipped;
            // an aborted boot must not be.
            catch (Exception e) when (!ct.IsCancellationRequested)
            {
                Debug.LogError($"[Content] Pack '{packId}' failed to load and was skipped. {e}");
                return;
            }

            if (manifest == null)
            {
                Debug.LogError($"[Content] Pack '{packId}' resolved to nothing and was skipped.");
                return;
            }

            if (manifest.PackId != packId)
            {
                // Silent divergence otherwise: the content would be in the registry while
                // IsPackAvailable(packId) answered false, so a button would hide for no visible reason.
                Debug.LogError($"[Content] Manifest at key '{_profile.ManifestKey(packId)}' declares " +
                               $"PackId '{manifest.PackId}'; pack skipped. Rename one to match the other.");
                return;
            }

            _registry.MarkPackLoaded(packId);
            AddEntries(manifest);
        }

        private void AddEntries(ContentPackManifest manifest)
        {
            var entries = manifest.Entries;
            if (entries == null) return;

            foreach (var entry in entries)
            {
                if (entry == null)
                {
                    Debug.LogError($"[Content] Pack '{manifest.PackId}' has an empty entry slot; skipped.");
                    continue;
                }

                if (!ContentId.TrySplit(entry.Id, out string entryPackId, out _))
                {
                    Debug.LogError($"[Content] '{entry.name}' in pack '{manifest.PackId}' has id " +
                                   $"'{entry.Id}', which is not a valid packId:entityId; skipped.");
                    continue;
                }

                if (entryPackId != manifest.PackId)
                {
                    Debug.LogError($"[Content] '{entry.Id}' is listed by pack '{manifest.PackId}' but " +
                                   "claims another pack; skipped.");
                    continue;
                }

                if (!_registry.TryAdd(entry))
                {
                    Debug.LogError($"[Content] Duplicate id '{entry.Id}' from pack '{manifest.PackId}'; " +
                                   "the definition loaded first was kept.");
                }
            }
        }
    }
}
