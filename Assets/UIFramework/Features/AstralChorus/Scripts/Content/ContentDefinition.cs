using UnityEngine;

namespace AstralChorus.Content
{
    // Base type for every authored piece of content. Subclasses (characters, banners, chapters)
    // live here in the game assembly, never in a pack -- that is what keeps a pack a folder of
    // .asset files with no code in it.
    //
    // Three fields, deliberately. Anything a specific content type needs goes on that subclass.
    //
    // No [Preserve] and no OnValidate:
    //   - IL2CPP keeps these types because the manifest holds a SERIALIZED REFERENCE to each one.
    //     [Preserve] is for types reached only by reflection (UIView<> subclasses, save POCOs).
    //   - OnValidate runs the moment an asset is created, when Id is still empty, so a format check
    //     there would spam the console. Validation is the Editor validator's job, on demand.
    public abstract class ContentDefinition : ScriptableObject
    {
        [Tooltip("\"packId:entityId\". This is a key into the player's save file: it is append-only and must never be reused, even for an entity that was deleted.")]
        [SerializeField] private string _id;

        [Tooltip("A key this game resolves to display text. Nothing in the content layer ever resolves it -- localization is deliberately out of scope.")]
        [SerializeField] private string _displayNameKey;

        [Tooltip("Optional. Key for IAssetLoader. Empty is legal and means this definition has no icon.")]
        [SerializeField] private string _iconAddress;

        public string Id => _id;
        public string DisplayNameKey => _displayNameKey;
        public string IconAddress => _iconAddress;
    }
}
