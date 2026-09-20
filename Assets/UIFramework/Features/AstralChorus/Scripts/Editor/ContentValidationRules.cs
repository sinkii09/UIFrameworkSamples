using System.Collections.Generic;
using AstralChorus.Content;

namespace AstralChorus.Editor
{
    // A manifest flattened into plain data, so the rules below can be tested without touching
    // AssetDatabase or instantiating a single ScriptableObject.
    public sealed class ManifestInfo
    {
        public string PackId;
        public string FileName;    // manifest file name, no extension
        public string AssetPath;
        public List<EntryInfo> Entries = new List<EntryInfo>();
    }

    public sealed class EntryInfo
    {
        public string Id;
        public string AssetName;   // only ever used to name the offender in a message
    }

    // Every rule is a pure function over data someone else gathered. That split is the only reason
    // these rules are testable at all: the alternative is creating real assets on disk inside a test.
    public static partial class ContentValidationRules
    {
        // A manifest lives at <packRoot>/Resources/ContentPacks/<packId>.asset, so the pack root is
        // whatever comes before "/Resources/". A manifest outside a Resources folder cannot be
        // loaded by key at all, which is itself a finding.
        public static bool TryGetPackRoot(string manifestAssetPath, out string packRoot)
        {
            packRoot = null;
            if (string.IsNullOrEmpty(manifestAssetPath)) return false;

            int marker = manifestAssetPath.IndexOf("/Resources/", System.StringComparison.Ordinal);
            if (marker < 0) return false;

            packRoot = manifestAssetPath.Substring(0, marker);
            return true;
        }

        // Rules 1 and 2.
        public static void CheckManifests(IReadOnlyList<ManifestInfo> manifests, List<string> findings)
        {
            var seenIds = new Dictionary<string, string>();     // content id -> pack that declared it
            var seenPacks = new Dictionary<string, string>();   // pack id    -> manifest that declared it

            foreach (var manifest in manifests)
            {
                if (string.IsNullOrEmpty(manifest.PackId))
                {
                    findings.Add($"[rule 2] Manifest at '{manifest.AssetPath}' has an empty PackId.");
                    continue;
                }

                // Two packs with one id is worse than it looks: both manifests answer to the same
                // Resources key, so which one the loader gets is decided by import order, and every
                // other rule here is comparing against whichever one happened to win.
                if (seenPacks.TryGetValue(manifest.PackId, out string firstManifest))
                {
                    findings.Add($"[rule 1] Two manifests declare pack '{manifest.PackId}': " +
                                 $"'{firstManifest}' and '{manifest.AssetPath}'.");
                    continue;
                }

                seenPacks.Add(manifest.PackId, manifest.AssetPath);

                // The loader fetches packs BY KEY, so a manifest whose file name differs from its
                // PackId is unreachable or answers IsPackAvailable with the wrong name.
                if (manifest.PackId != manifest.FileName)
                {
                    findings.Add($"[rule 2] Manifest '{manifest.AssetPath}' declares PackId " +
                                 $"'{manifest.PackId}' but its file is named '{manifest.FileName}'.");
                }

                if (!TryGetPackRoot(manifest.AssetPath, out _))
                {
                    findings.Add($"[rule 2] Manifest '{manifest.AssetPath}' is not inside a Resources " +
                                 "folder, so the loader can never fetch it by key.");
                }

                foreach (var entry in manifest.Entries)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.Id))
                    {
                        findings.Add($"[rule 2] Pack '{manifest.PackId}' has an entry with no id.");
                        continue;
                    }

                    if (!ContentId.TrySplit(entry.Id, out string entryPack, out _))
                    {
                        findings.Add($"[rule 2] '{entry.AssetName}' in pack '{manifest.PackId}' has id " +
                                     $"'{entry.Id}', which is not a valid packId:entityId.");
                        continue;
                    }

                    if (entryPack != manifest.PackId)
                    {
                        findings.Add($"[rule 2] '{entry.Id}' is listed by pack '{manifest.PackId}' but " +
                                     $"claims pack '{entryPack}'.");
                        continue;
                    }

                    if (seenIds.TryGetValue(entry.Id, out string owner))
                    {
                        findings.Add($"[rule 1] Duplicate id '{entry.Id}': declared by both '{owner}' " +
                                     $"and '{manifest.PackId}'.");
                        continue;
                    }

                    seenIds.Add(entry.Id, manifest.PackId);
                }
            }
        }

        // Rule 3. Takes the profile's values rather than the asset, again so it can be tested.
        public static void CheckProfile(
            string alwaysLoadedPackId,
            IReadOnlyList<string> enabledPackIds,
            IReadOnlyList<ManifestInfo> manifests,
            List<string> findings)
        {
            var known = new HashSet<string>();
            foreach (var manifest in manifests)
            {
                if (!string.IsNullOrEmpty(manifest.PackId)) known.Add(manifest.PackId);
            }

            if (string.IsNullOrEmpty(alwaysLoadedPackId))
            {
                findings.Add("[rule 3] The profile has no always-loaded pack id.");
            }
            else if (!known.Contains(alwaysLoadedPackId))
            {
                findings.Add($"[rule 3] The always-loaded pack '{alwaysLoadedPackId}' has no manifest.");
            }

            if (enabledPackIds == null) return;

            var seen = new HashSet<string>();
            foreach (string packId in enabledPackIds)
            {
                if (string.IsNullOrEmpty(packId))
                {
                    findings.Add("[rule 3] EnabledPackIds contains an empty entry.");
                    continue;
                }

                // Listing it would load the main content twice, or -- with one typo -- not at all.
                if (packId == alwaysLoadedPackId)
                {
                    findings.Add($"[rule 3] '{packId}' is the always-loaded pack and must not be listed " +
                                 "in EnabledPackIds.");
                    continue;
                }

                if (!seen.Add(packId))
                {
                    findings.Add($"[rule 3] '{packId}' is listed twice in EnabledPackIds.");
                    continue;
                }

                if (!known.Contains(packId))
                {
                    findings.Add($"[rule 3] EnabledPackIds names '{packId}', which has no manifest.");
                }
            }
        }
    }
}
