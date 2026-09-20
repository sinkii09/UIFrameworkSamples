using System;
using System.Threading;
using AstralChorus.Content;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace AstralChorus.Tests.Content
{
    // The fail-soft wrapper. Its whole contract is "returns null instead of throwing, EXCEPT for
    // cancellation", so both halves of that sentence need a test -- swallowing a cancelled load
    // would report a closed screen's art as missing and send the caller off to draw a placeholder
    // into a dead view.
    public sealed class ContentAssetLoaderTests
    {
        private ContentObjects _objects;
        private FakeAssetLoader _assets;
        private ContentAssetLoader _loader;

        [SetUp]
        public void SetUp()
        {
            _objects = new ContentObjects();
            _assets = new FakeAssetLoader();
            _loader = new ContentAssetLoader(_assets);
        }

        [TearDown]
        public void TearDown() => _objects.Dispose();

        [Test]
        public void TryLoadAsync_AddressPresent_ReturnsTheAsset()
        {
            var definition = _objects.Definition("core:aria");
            _assets.Add("art/aria", definition);

            var loaded = SyncTask.Run(_loader.TryLoadAsync<TestDefinition>("art/aria"));

            Assert.That(loaded, Is.SameAs(definition));
        }

        [Test]
        public void TryLoadAsync_MissingAddress_ReturnsNullAndWarns()
        {
            using (var logs = new LogRecorder())
            {
                var loaded = SyncTask.Run(_loader.TryLoadAsync<TestDefinition>("art/nope"));

                Assert.That(loaded, Is.Null);
                Assert.That(logs.Warnings.Count, Is.EqualTo(1));
                Assert.That(logs.Warnings[0], Does.Contain("art/nope"));
            }
        }

        // An empty IconAddress means "this definition has no icon". Logging it would turn ordinary
        // authored data into console noise, and noise is how real warnings get ignored.
        [Test]
        public void TryLoadAsync_EmptyAddress_ReturnsNullWithoutLogging()
        {
            using (var logs = new LogRecorder())
            {
                Assert.That(SyncTask.Run(_loader.TryLoadAsync<TestDefinition>(string.Empty)), Is.Null);
                Assert.That(SyncTask.Run(_loader.TryLoadAsync<TestDefinition>(null)), Is.Null);

                Assert.That(logs.Warnings, Is.Empty);
                Assert.That(_assets.LoadCount, Is.Zero, "an empty address must not reach the loader");
            }
        }

        [Test]
        public void TryLoadAsync_Cancelled_PropagatesInsteadOfReturningNull()
        {
            var definition = _objects.Definition("core:aria");
            _assets.Add("art/aria", definition);

            var cts = new CancellationTokenSource();
            cts.Cancel();

            var task = _loader.TryLoadAsync<TestDefinition>("art/aria", cts.Token);
            Assert.That(task.Status.IsCompleted(), Is.True);
            Assert.Throws<OperationCanceledException>(() => task.GetAwaiter().GetResult());
        }

        [Test]
        public void TryLoadAsync_WrongType_ReturnsNullAndWarns()
        {
            _assets.Add("art/aria", _objects.Definition<OtherDefinition>("core:aria"));

            using (var logs = new LogRecorder())
            {
                Assert.That(SyncTask.Run(_loader.TryLoadAsync<TestDefinition>("art/aria")), Is.Null);
                Assert.That(logs.Warnings.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void UnloadAsync_KnownAddress_ReachesTheLoader()
        {
            _assets.Add("art/aria", _objects.Definition("core:aria"));

            SyncTask.Run(_loader.UnloadAsync("art/aria"));

            Assert.That(_assets.UnloadCount, Is.EqualTo(1));
        }

        [Test]
        public void UnloadAsync_EmptyAddress_DoesNothing()
        {
            SyncTask.Run(_loader.UnloadAsync(string.Empty));

            Assert.That(_assets.UnloadCount, Is.Zero);
        }

        [Test]
        public void UnloadAsync_UnknownAddress_WarnsInsteadOfThrowing()
        {
            using (var logs = new LogRecorder())
            {
                Assert.DoesNotThrow(() => SyncTask.Run(_loader.UnloadAsync("art/never-loaded")));
                Assert.That(logs.Warnings.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void UnloadAsync_Cancelled_Propagates()
        {
            _assets.Add("art/aria", _objects.Definition("core:aria"));

            var cts = new CancellationTokenSource();
            cts.Cancel();

            var task = _loader.UnloadAsync("art/aria", cts.Token);
            Assert.That(task.Status.IsCompleted(), Is.True);
            Assert.Throws<OperationCanceledException>(() => task.GetAwaiter().GetResult());
        }
    }
}
