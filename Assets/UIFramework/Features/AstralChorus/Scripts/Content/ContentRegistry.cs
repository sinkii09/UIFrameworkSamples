using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace AstralChorus.Content
{
    // The store. ContentPackLoader owns the write side (internal); everything else sees
    // IContentRegistry.
    //
    // Threading: built on the main thread and read-only afterwards, so there are no locks. Do not
    // reason from ISaveService here -- that one really does touch the thread pool. This one does
    // not: Resources loads resume on the PlayerLoop.
    //
    // Reads are gated by STATE, not by "the build already awaited". Between two awaits inside
    // BuildAsync the store is publicly readable and half-populated; only the state flag can tell
    // the difference.
    public sealed class ContentRegistry : IContentRegistry
    {
        private const int WarnCap = 256;

        private readonly Dictionary<string, ContentDefinition> _byId = new Dictionary<string, ContentDefinition>();
        private readonly HashSet<string> _packs = new HashSet<string>();
        private readonly Dictionary<Type, object> _allCache = new Dictionary<Type, object>();
        private readonly HashSet<string> _warnedIds = new HashSet<string>();
        private bool _warnCapAnnounced;

        // Frozen means "never complained": reads in that state are legal, so it can never match.
        private ContentRegistryState _complainedAbout = ContentRegistryState.Frozen;

        public ContentRegistryState State { get; private set; } = ContentRegistryState.NotStarted;

        // ---- write side: ContentPackLoader only -------------------------------------------------

        // Clearing here is what makes a retry after Failed safe. Without it, a build that got
        // through packs A and B before dying on C would re-add A and B on the next attempt and
        // report a duplicate-id error for every single entry -- one real failure turned into
        // hundreds of fake ones.
        internal void BeginBuild()
        {
            _byId.Clear();
            _packs.Clear();
            _allCache.Clear();
            _warnedIds.Clear();
            _warnCapAnnounced = false;
            State = ContentRegistryState.Building;
        }

        internal void MarkPackLoaded(string packId) => _packs.Add(packId);

        // First writer wins. The always-loaded pack is loaded first, so on a collision the main
        // content is the one that survives.
        internal bool TryAdd(ContentDefinition definition)
        {
            if (_byId.ContainsKey(definition.Id)) return false;
            _byId.Add(definition.Id, definition);
            return true;
        }

        internal void Freeze() => State = ContentRegistryState.Frozen;

        internal void Fail() => State = ContentRegistryState.Failed;

        // ---- read side --------------------------------------------------------------------------

        public bool TryGet<T>(string id, out T definition) where T : ContentDefinition
        {
            definition = null;
            if (!IsReadable()) return false;

            if (string.IsNullOrEmpty(id) || !_byId.TryGetValue(id, out var found))
            {
                WarnMiss(id);
                return false;
            }

            // `is T` rather than `as T` plus a null check: a DESTROYED definition of the right type
            // compares equal to null through UnityEngine.Object's operator, which would be reported
            // here as a type mismatch and send whoever reads it looking in the wrong place.
            if (found is T typed)
            {
                definition = typed;
                return true;
            }

            // Asking for the wrong type is a programmer error, not absent content, so it does not
            // share the throttled-warning path that disabled packs use.
            Debug.LogError(
                $"[Content] '{id}' exists but is {found.GetType().Name}, not {typeof(T).Name}.");
            return false;
        }

        public bool IsPackAvailable(string packId)
            => IsReadable() && !string.IsNullOrEmpty(packId) && _packs.Contains(packId);

        public IReadOnlyList<T> All<T>() where T : ContentDefinition
        {
            // Returns WITHOUT touching the cache: a cache entry built while the store was still
            // half-populated would stay wrong for the rest of the session.
            if (!IsReadable()) return Array.Empty<T>();

            if (_allCache.TryGetValue(typeof(T), out var cached)) return (IReadOnlyList<T>)cached;

            var matches = new List<T>();
            foreach (var definition in _byId.Values)
            {
                if (definition is T typed) matches.Add(typed);
            }

            // ReadOnlyCollection, not the List and not the array: an IReadOnlyList<T> that is
            // really a List<T> can be cast back and mutated by any caller, and the cache is shared.
            IReadOnlyList<T> snapshot = new ReadOnlyCollection<T>(matches);
            _allCache[typeof(T)] = snapshot;
            return snapshot;
        }

        private bool IsReadable()
        {
            if (State == ContentRegistryState.Frozen) return true;

            // Reported once per state, not once per call. A view reading content before boot
            // finishes may well be doing it from Update, and an unthrottled error at frame rate
            // buries the console -- the same reason the miss-warnings are capped.
            if (_complainedAbout != State)
            {
                _complainedAbout = State;
                Debug.LogError(
                    $"[Content] Registry read while {State}. Await ContentPackLoader.BuildAsync during " +
                    "boot, before anything reads content.");
            }

            return false;
        }

        private void WarnMiss(string id)
        {
            if (_warnedIds.Count >= WarnCap)
            {
                // Capping is right; going silent forever is not. Without this line a genuine
                // content bug after the 257th distinct miss leaves no trace at all.
                if (_warnCapAnnounced) return;
                _warnCapAnnounced = true;
                Debug.LogWarning(
                    $"[Content] {WarnCap} distinct missing ids reported; further misses will not be logged.");
                return;
            }

            if (_warnedIds.Add(id))
            {
                Debug.LogWarning(
                    $"[Content] No definition for '{id}'. The pack that owns it is probably disabled.");
            }
        }
    }
}
