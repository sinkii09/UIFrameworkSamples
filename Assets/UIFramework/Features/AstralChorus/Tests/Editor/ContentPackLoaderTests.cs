using System;
using System.Text.RegularExpressions;
using System.Threading;
using AstralChorus.Content;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AstralChorus.Tests.Content
{
    public sealed class ContentPackLoaderTests
    {
        private ContentObjects _objects;
        private FakeAssetLoader _assets;
        private ContentRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            _objects = new ContentObjects();
            _assets = new FakeAssetLoader();
            _registry = new ContentRegistry();
        }

        [TearDown]
        public void TearDown() => _objects.Dispose();

        private ContentPackLoader Loader(ContentBuildProfile profile)
            => new ContentPackLoader(_registry, profile, _assets);

        private void AddPack(string packId, params ContentDefinition[] entries)
            => _assets.Add("ContentPacks/" + packId, _objects.Manifest(packId, entries));

        [Test]
        public void BuildAsync_PackFailsToLoad_SkipsItAndKeepsGoing()
        {
            var profile = _objects.Profile("core", "tide", "ember");
            AddPack("core", _objects.Definition("core:aria"));
            AddPack("ember", _objects.Definition("ember:kaya"));
            // "tide" is deliberately absent, so the fake throws for it.

            LogAssert.Expect(LogType.Error, new Regex("Pack 'tide' failed to load"));
            SyncTask.Run(Loader(profile).BuildAsync());

            Assert.That(_registry.State, Is.EqualTo(ContentRegistryState.Frozen));
            Assert.That(_registry.IsPackAvailable("ember"), Is.True);
            Assert.That(_registry.TryGet<TestDefinition>("ember:kaya", out _), Is.True);
        }

        // The one that matters most. Loaders call ct.ThrowIfCancellationRequested(), so a bare
        // catch(Exception) in the skip-this-pack path would swallow an aborted boot, keep loading,
        // and then Freeze() a half-built registry -- marking incomplete content COMPLETE.
        [Test]
        public void BuildAsync_CancelledMidBuild_PropagatesAndDoesNotFreeze()
        {
            var cts = new CancellationTokenSource();
            var profile = _objects.Profile("core", "tide", "ember");
            AddPack("core", _objects.Definition("core:aria"));
            AddPack("tide", _objects.Definition("tide:kaya"));
            AddPack("ember", _objects.Definition("ember:sol"));

            _assets.OnLoad = key =>
            {
                if (key == "ContentPacks/tide") cts.Cancel();
            };

            var task = Loader(profile).BuildAsync(cts.Token);
            Assert.That(task.Status.IsCompleted(), Is.True);
            Assert.Throws<OperationCanceledException>(() => task.GetAwaiter().GetResult());

            Assert.That(_registry.State, Is.EqualTo(ContentRegistryState.Failed));
            Assert.That(_assets.LoadCount, Is.EqualTo(2), "the third pack must not have been attempted");
        }

        [Test]
        public void BuildAsync_AlwaysLoadedPackAlsoListedAsEnabled_LoadsItOnce()
        {
            var profile = _objects.Profile("core", "core");
            AddPack("core", _objects.Definition("core:aria"));

            LogAssert.Expect(LogType.Error, new Regex("is the always-loaded pack"));
            SyncTask.Run(Loader(profile).BuildAsync());

            Assert.That(_assets.LoadCount, Is.EqualTo(1));
            Assert.That(_registry.All<TestDefinition>().Count, Is.EqualTo(1));
        }

        [Test]
        public void BuildAsync_DuplicateEnabledId_LoadsItOnce()
        {
            var profile = _objects.Profile("core", "tide", "tide");
            AddPack("core");
            AddPack("tide", _objects.Definition("tide:kaya"));

            LogAssert.Expect(LogType.Error, new Regex("listed twice|Duplicate 'tide'"));
            SyncTask.Run(Loader(profile).BuildAsync());

            Assert.That(_assets.LoadCount, Is.EqualTo(2));
        }

        // Fetching by key makes the file name the identity; PackId is a separate serialized string.
        // Left unchecked, the content would be in the registry while IsPackAvailable said otherwise.
        [Test]
        public void BuildAsync_ManifestPackIdDiffersFromKey_SkipsWholePack()
        {
            var profile = _objects.Profile("core", "tide");
            AddPack("core");
            _assets.Add("ContentPacks/tide", _objects.Manifest("tyde", _objects.Definition("tyde:kaya")));

            LogAssert.Expect(LogType.Error, new Regex("declares PackId 'tyde'"));
            SyncTask.Run(Loader(profile).BuildAsync());

            Assert.That(_registry.IsPackAvailable("tide"), Is.False);
            Assert.That(_registry.TryGet<TestDefinition>("tyde:kaya", out _), Is.False);
        }

        [Test]
        public void BuildAsync_DuplicateIdInsideOnePack_KeepsTheFirst()
        {
            var first = _objects.Definition("core:aria");
            var second = _objects.Definition("core:aria");
            var profile = _objects.Profile("core");
            AddPack("core", first, second);

            LogAssert.Expect(LogType.Error, new Regex("Duplicate id 'core:aria'"));
            SyncTask.Run(Loader(profile).BuildAsync());

            _registry.TryGet<TestDefinition>("core:aria", out var resolved);
            Assert.That(resolved, Is.SameAs(first));
        }

        [Test]
        public void BuildAsync_BadEntries_SkipsOnlyThem()
        {
            var profile = _objects.Profile("core");
            AddPack("core",
                _objects.Definition("core:good"),
                null,
                _objects.Definition("NOT VALID"),
                _objects.Definition("tide:wrongpack"));

            LogAssert.Expect(LogType.Error, new Regex("empty entry slot"));
            LogAssert.Expect(LogType.Error, new Regex("not a valid packId:entityId"));
            LogAssert.Expect(LogType.Error, new Regex("claims another pack"));
            SyncTask.Run(Loader(profile).BuildAsync());

            Assert.That(_registry.All<TestDefinition>().Count, Is.EqualTo(1));
            Assert.That(_registry.TryGet<TestDefinition>("core:good", out _), Is.True);
        }

        [Test]
        public void BuildAsync_CalledAgainAfterFreeze_DoesNothing()
        {
            var profile = _objects.Profile("core");
            AddPack("core", _objects.Definition("core:aria"));
            var loader = Loader(profile);
            SyncTask.Run(loader.BuildAsync());

            LogAssert.Expect(LogType.Error, new Regex("after the registry was frozen"));
            SyncTask.Run(loader.BuildAsync());

            Assert.That(_assets.LoadCount, Is.EqualTo(1));
            Assert.That(_registry.All<TestDefinition>().Count, Is.EqualTo(1));
        }

        // Without the store reset in BeginBuild, the retry would re-add everything the first attempt
        // managed to load and report a duplicate for every single entry: one real failure turned
        // into hundreds of fake ones.
        [Test]
        public void BuildAsync_RetryAfterFailure_ClearsTheStoreAndSucceeds()
        {
            var cts = new CancellationTokenSource();
            var profile = _objects.Profile("core", "tide");
            AddPack("core", _objects.Definition("core:aria"));
            AddPack("tide", _objects.Definition("tide:kaya"));

            _assets.OnLoad = key =>
            {
                if (key == "ContentPacks/tide") cts.Cancel();
            };

            var loader = Loader(profile);
            Assert.Throws<OperationCanceledException>(
                () => loader.BuildAsync(cts.Token).GetAwaiter().GetResult());
            Assert.That(_registry.State, Is.EqualTo(ContentRegistryState.Failed));

            _assets.OnLoad = null;
            SyncTask.Run(loader.BuildAsync());

            Assert.That(_registry.State, Is.EqualTo(ContentRegistryState.Frozen));
            Assert.That(_registry.All<TestDefinition>().Count, Is.EqualTo(2));
        }

        [Test]
        public void All_WhileBuilding_LogsErrorAndDoesNotPoisonTheCache()
        {
            var profile = _objects.Profile("core");
            AddPack("core", _objects.Definition("core:aria"));

            // Re-entering from inside the fake's load callback is the only way to observe the
            // registry mid-build, because the fake completes synchronously. All<T>() rather than
            // TryGet, because TryGet never touches the cache -- the half-populated store is exactly
            // what an All<T>() cache entry built mid-build would freeze in place forever.
            _assets.OnLoad = key => _registry.All<TestDefinition>();

            LogAssert.Expect(LogType.Error, new Regex("Registry read while Building"));
            SyncTask.Run(Loader(profile).BuildAsync());

            Assert.That(_registry.All<TestDefinition>().Count, Is.EqualTo(1));
        }
    }
}
