using System.Collections.Generic;

namespace AstralChorus.Content
{
    public enum ContentRegistryState
    {
        NotStarted,
        Building,
        Frozen,
        Failed
    }

    // Read-only face of the content store. Built once during boot, then frozen.
    //
    // NOTHING here throws. A miss is the NORMAL case -- it is what a disabled pack looks like from
    // the inside -- so every call site is `if (TryGet(...)) { ... } else { skip }` and a disabled
    // pack costs zero lines of special handling anywhere.
    public interface IContentRegistry
    {
        bool TryGet<T>(string id, out T definition) where T : ContentDefinition;

        bool IsPackAvailable(string packId);

        IReadOnlyList<T> All<T>() where T : ContentDefinition;
    }
}
