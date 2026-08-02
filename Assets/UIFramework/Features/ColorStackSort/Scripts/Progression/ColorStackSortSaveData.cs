namespace ColorStackSort
{
    /// <summary>
    /// What survives between sessions. A plain POCO: no UnityEngine types, no polymorphism — both
    /// are documented limits of the framework's JSON save service.
    /// <para>
    /// <b>Every field added here must round-trip.</b> <c>LevelProgressService.LoadAsync</c> adopts
    /// the deserialized instance wholesale for exactly that reason — do not change it to copy
    /// individual fields out, or anything not copied is silently overwritten on the next save.
    /// </para>
    /// </summary>
    // [Preserve]: bound reflectively by Newtonsoft through SaveEnvelopeCodec, so an IL2CPP build
    // has no static call site proving the members are used.
    [UnityEngine.Scripting.Preserve]
    public sealed class ColorStackSortSaveData
    {
        /// <summary>
        /// The save key, pinned explicitly rather than left to default to <c>typeof(T).Name</c>.
        /// <para>
        /// The default key IS the on-disk filename, so renaming this class would orphan every
        /// existing save with no diagnostic — the framework's own docs flag it as a footgun for
        /// anything that ships. Must also match <c>^[A-Za-z0-9_-]+$</c>: the service validates keys
        /// at runtime, so a dot here would throw <c>ArgumentException</c> rather than fail to
        /// compile.
        /// </para>
        /// </summary>
        public const string SaveKey = "ColorStackSortProgress";

        /// <summary>
        /// Must be a settable property. A get-only member does not round-trip through Newtonsoft,
        /// and would silently load as 0 every time.
        /// </summary>
        public int CurrentLevel { get; set; }
    }
}
