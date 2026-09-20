using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using AstralChorus.Content;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AstralChorus.Tests.Content
{
    // The registry is exercised through ContentPackLoader rather than through its internal write
    // side, so these tests run the same path the game runs.
    public sealed class ContentRegistryTests
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

        private void BuildCore(params ContentDefinition[] definitions)
        {
            var profile = _objects.Profile("core");
            _assets.Add("ContentPacks/core", _objects.Manifest("core", definitions));
            SyncTask.Run(new ContentPackLoader(_registry, profile, _assets).BuildAsync());
            Assert.That(_registry.State, Is.EqualTo(ContentRegistryState.Frozen));
        }

        [Test]
        public void TryGet_BeforeBuild_LogsErrorAndReturnsFalse()
        {
            LogAssert.Expect(LogType.Error, new Regex("Registry read while NotStarted"));

            Assert.That(_registry.TryGet<TestDefinition>("core:aria", out var definition), Is.False);
            Assert.That(definition, Is.Null);
        }

        [Test]
        public void TryGet_MissingId_WarnsOnceForRepeatedLookups()
        {
            BuildCore(_objects.Definition("core:aria"));

            using (var logs = new LogRecorder())
            {
                _registry.TryGet<TestDefinition>("tide:kaya", out _);
                _registry.TryGet<TestDefinition>("tide:kaya", out _);

                Assert.That(logs.Warnings.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void TryGet_ManyMissingIds_StopsAtCapAndSaysSo()
        {
            BuildCore(_objects.Definition("core:aria"));

            using (var logs = new LogRecorder())
            {
                for (int i = 0; i < 300; i++)
                {
                    _registry.TryGet<TestDefinition>($"tide:missing_{i}", out _);
                }

                // 256 distinct misses, then exactly one line saying it has gone quiet. Silence with
                // no announcement would make a genuine content bug after the cap invisible.
                Assert.That(logs.Warnings.Count, Is.EqualTo(257));
                Assert.That(logs.Warnings[256], Does.Contain("will not be logged"));
            }
        }

        [Test]
        public void TryGet_WrongType_LogsErrorRatherThanWarning()
        {
            BuildCore(_objects.Definition("core:aria"));
            LogAssert.Expect(LogType.Error, new Regex("exists but is TestDefinition"));

            using (var logs = new LogRecorder())
            {
                Assert.That(_registry.TryGet<OtherDefinition>("core:aria", out var definition), Is.False);
                Assert.That(definition, Is.Null);

                // Asking for the wrong type is a programmer error, not a disabled pack, so it must
                // not consume the throttled-warning path that disabled packs rely on.
                Assert.That(logs.Warnings, Is.Empty);
            }
        }

        [Test]
        public void All_MatchesSubclassesAndSkipsUnrelatedTypes()
        {
            BuildCore(
                _objects.Definition<TestDefinition>("core:a"),
                _objects.Definition<DerivedDefinition>("core:b"),
                _objects.Definition<OtherDefinition>("core:c"));

            Assert.That(_registry.All<TestDefinition>().Count, Is.EqualTo(2));
            Assert.That(_registry.All<OtherDefinition>().Count, Is.EqualTo(1));
        }

        [Test]
        public void All_NoMatches_ReturnsEmpty()
        {
            BuildCore(_objects.Definition("core:a"));

            Assert.That(_registry.All<OtherDefinition>(), Is.Empty);
        }

        [Test]
        public void All_ReturnsSnapshotThatCallersCannotMutate()
        {
            BuildCore(_objects.Definition("core:a"));

            IReadOnlyList<TestDefinition> all = _registry.All<TestDefinition>();

            // The cache is shared, so handing back a List<T> behind an IReadOnlyList<T> would let any
            // caller cast it back and rewrite content for everyone else.
            Assert.That(all as List<TestDefinition>, Is.Null);
            Assert.That(all, Is.InstanceOf<ReadOnlyCollection<TestDefinition>>());
        }

        [Test]
        public void All_CalledBeforeBuild_DoesNotPoisonTheCache()
        {
            LogAssert.Expect(LogType.Error, new Regex("Registry read while NotStarted"));
            Assert.That(_registry.All<TestDefinition>(), Is.Empty);

            BuildCore(_objects.Definition("core:a"));

            // Would be 0 forever if the empty pre-build answer had been cached.
            Assert.That(_registry.All<TestDefinition>().Count, Is.EqualTo(1));
        }

        [Test]
        public void IsPackAvailable_KnowsLoadedPacksOnly()
        {
            BuildCore(_objects.Definition("core:a"));

            Assert.That(_registry.IsPackAvailable("core"), Is.True);
            Assert.That(_registry.IsPackAvailable("tide"), Is.False);
        }
    }
}
