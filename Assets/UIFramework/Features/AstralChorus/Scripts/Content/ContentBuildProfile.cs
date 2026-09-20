using UnityEngine;

namespace AstralChorus.Content
{
    // Which packs ship. Read at RUNTIME, which is the whole trick: Editor Play Mode then matches a
    // build exactly, unlike excluding folders (breaks the developer's own Editor) or
    // defineConstraints (needs an asmdef per pack, and this repo already learned that a hand-placed
    // define is not a guarantee -- ADDRESSABLES was dropped in d87eceb for exactly that reason).
    [CreateAssetMenu(menuName = "AstralChorus/Content Build Profile", fileName = "ContentBuildProfile")]
    public sealed class ContentBuildProfile : ScriptableObject
    {
        [Tooltip("Loaded unconditionally and never listed below. A typo in the enabled list must not be able to erase the main content.")]
        [SerializeField] private string _alwaysLoadedPackId = "core";

        // IDs ONLY. A ContentPackManifest[] here would drag disabled packs back into the build
        // through the asset graph and defeat the entire mechanism. This is the whole trick.
        [Tooltip("Pack ids to load on top of the always-loaded one. IDs only -- never object references.")]
        [SerializeField] private string[] _enabledPackIds = new string[0];

        [Tooltip("Key prefix the loader prepends to a pack id. A trailing slash is added if missing.")]
        [SerializeField] private string _manifestKeyPrefix = "ContentPacks/";

        public string AlwaysLoadedPackId => _alwaysLoadedPackId;

        public string[] EnabledPackIds => _enabledPackIds ?? new string[0];

        // Normalises the trailing slash here rather than trusting whoever typed the field:
        // "ContentPacks" + "core" silently becomes "ContentPackscore", which misses without an error.
        public string ManifestKey(string packId)
        {
            string prefix = _manifestKeyPrefix ?? string.Empty;
            if (prefix.Length > 0 && !prefix.EndsWith("/")) prefix += "/";
            return prefix + packId;
        }
    }
}
