using System.Collections.Generic;
using AstralChorus.Content;
using UnityEditor;
using UnityEngine;

namespace AstralChorus.Editor
{
    // Gathers project state and hands it to ContentValidationRules. Everything that decides anything
    // lives in that class; this one only reads the AssetDatabase and prints.
    //
    // Run on demand, never from OnValidate: rule 4 walks the dependency edges of the whole project
    // and takes minutes, not milliseconds.
    public static class ContentPackValidator
    {
        [MenuItem("Tools/AstralChorus/Validate Content Packs")]
        public static void Validate()
        {
            var findings = new List<string>();
            bool scanCancelled = false;

            try
            {
                var manifests = GatherManifests();
                if (manifests.Count == 0)
                {
                    Debug.LogWarning("[ContentValidator] No ContentPackManifest assets found. Nothing to check.");
                    return;
                }

                ContentValidationRules.CheckManifests(manifests, findings);

                var profile = LoadSingleProfile(findings);
                if (profile != null)
                {
                    ContentValidationRules.CheckProfile(
                        profile.AlwaysLoadedPackId, profile.EnabledPackIds, manifests, findings);
                }

                var edges = GatherEdges(out scanCancelled);
                ContentValidationRules.CheckReferences(edges, PackRootsOf(manifests), findings);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Report(findings, scanCancelled);
        }

        private static List<ManifestInfo> GatherManifests()
        {
            var result = new List<ManifestInfo>();

            foreach (string guid in AssetDatabase.FindAssets("t:ContentPackManifest"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ContentPackManifest>(path);
                if (asset == null) continue;

                var info = new ManifestInfo
                {
                    PackId = asset.PackId,
                    FileName = System.IO.Path.GetFileNameWithoutExtension(path),
                    AssetPath = path
                };

                if (asset.Entries != null)
                {
                    foreach (var entry in asset.Entries)
                    {
                        info.Entries.Add(entry == null
                            ? null
                            : new EntryInfo { Id = entry.Id, AssetName = entry.name });
                    }
                }

                result.Add(info);
            }

            return result;
        }

        private static ContentBuildProfile LoadSingleProfile(List<string> findings)
        {
            string[] guids = AssetDatabase.FindAssets("t:ContentBuildProfile");

            if (guids.Length == 0)
            {
                findings.Add("[rule 3] No ContentBuildProfile asset exists; nothing decides which packs ship.");
                return null;
            }

            if (guids.Length > 1)
            {
                findings.Add($"[rule 3] {guids.Length} ContentBuildProfile assets exist. Only the one the " +
                             "scope references is used; the others are misleading dead config.");
            }

            return AssetDatabase.LoadAssetAtPath<ContentBuildProfile>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static List<string> PackRootsOf(List<ManifestInfo> manifests)
        {
            var roots = new List<string>();
            foreach (var manifest in manifests)
            {
                // A manifest that is not inside a Resources folder is already a rule 2 finding, but
                // dropping it here would make rule 4 blind to that same pack -- the misplaced pack
                // would be the one nobody is allowed to reference and the one nobody checks. Fall
                // back to the manifest's own folder so it still contributes a root.
                if (!ContentValidationRules.TryGetPackRoot(manifest.AssetPath, out string root))
                {
                    root = System.IO.Path.GetDirectoryName(manifest.AssetPath)?.Replace('\\', '/');
                }

                if (!string.IsNullOrEmpty(root) && !roots.Contains(root)) roots.Add(root);
            }
            return roots;
        }

        private static List<KeyValuePair<string, string>> GatherEdges(out bool cancelled)
        {
            cancelled = false;
            var edges = new List<KeyValuePair<string, string>>();
            string[] all = AssetDatabase.GetAllAssetPaths();

            for (int i = 0; i < all.Length; i++)
            {
                string path = all[i];
                if (!path.StartsWith("Assets/") || IsSourceFile(path)) continue;

                if (i % 200 == 0 &&
                    EditorUtility.DisplayCancelableProgressBar(
                        "Validating content packs", path, (float)i / all.Length))
                {
                    cancelled = true;
                    break;
                }

                // recursive: false is enough. A core -> core -> pack chain is caught because the
                // middle asset is scanned too, and its own direct hop into the pack is an edge here.
                foreach (string dependency in AssetDatabase.GetDependencies(path, false))
                {
                    edges.Add(new KeyValuePair<string, string>(path, dependency));
                }
            }

            return edges;
        }

        private static bool IsSourceFile(string path)
            => path.EndsWith(".cs") || path.EndsWith(".asmdef") || path.EndsWith(".meta");

        private static void Report(List<string> findings, bool scanCancelled)
        {
            foreach (string finding in findings) Debug.LogError("[ContentValidator] " + finding);

            // A cancelled scan checked fewer assets than it claims to, so "0 findings" from one is a
            // false all-clear -- worse than no validator, because somebody will trust it.
            if (scanCancelled)
            {
                Debug.LogWarning($"[ContentValidator] Reference scan was CANCELLED. {findings.Count} " +
                                 "finding(s) so far, but the run is PARTIAL and a clean result proves nothing.");
                return;
            }

            if (findings.Count == 0)
            {
                Debug.Log("[ContentValidator] 0 findings. Note: rule 4 only sees references stored in " +
                          "the asset graph -- it cannot see Resources.Load(\"...\") or an Addressables " +
                          "address built from a string.");
                return;
            }

            Debug.LogError($"[ContentValidator] {findings.Count} finding(s).");
        }
    }
}
