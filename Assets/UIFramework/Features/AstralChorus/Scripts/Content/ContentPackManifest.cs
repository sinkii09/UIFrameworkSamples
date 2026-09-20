using UnityEngine;

namespace AstralChorus.Content
{
    // One per pack, and the ONLY asset that holds hard references to a pack's contents.
    //
    // It must live INSIDE its own pack folder, at Packs/<packId>/Resources/ContentPacks/<packId>.asset.
    // Unity merges every Resources/ folder, so the load key stays "ContentPacks/<packId>" either way --
    // but a manifest parked in a shared Resources/ folder outside the packs would drag EVERY pack's
    // definitions into the build regardless of which packs the profile enables, because Resources/
    // pulls the manifest and the manifest pulls its entries. Deleting a pack folder has to actually
    // delete its bytes, and that only works if the manifest goes with it.
    [CreateAssetMenu(menuName = "AstralChorus/Content Pack Manifest", fileName = "NewPack")]
    public sealed class ContentPackManifest : ScriptableObject
    {
        [Tooltip("Must match this asset's file name -- the loader looks packs up BY KEY, so the file name is the identity. The validator checks both.")]
        [SerializeField] private string _packId;

        [SerializeField] private ContentDefinition[] _entries;

        public string PackId => _packId;

        public ContentDefinition[] Entries => _entries;
    }
}
