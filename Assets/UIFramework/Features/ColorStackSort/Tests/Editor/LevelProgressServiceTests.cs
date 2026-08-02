using System;
using System.Text.RegularExpressions;
using System.Threading;
using ColorStackSort.Logic;
using NUnit.Framework;
using R3;
using UnityEngine.TestTools;

namespace ColorStackSort.Tests
{
    /// <summary>
    /// Progression persistence. Every failure mode here is one the real service can produce and
    /// none of them can be reached through the file system in an EditMode test, so the fake is the
    /// only way to pin them.
    /// <para>
    /// The tests await synchronously (<c>.GetAwaiter().GetResult()</c>) because
    /// <see cref="FakeSaveService"/> completes inline — see its remarks for why nothing here yields.
    /// </para>
    /// </summary>
    [TestFixture]
    public class LevelProgressServiceTests
    {
        private FakeSaveService _saves;
        private LevelProgressService _service;

        [SetUp]
        public void SetUp()
        {
            _saves = new FakeSaveService();
            _service = new LevelProgressService(_saves);
        }

        private void Load() => _service.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

        [Test]
        public void Load_WithNoSaveFile_StartsAtFirstLevel()
        {
            _saves.Behaviour = FakeSaveService.LoadBehaviour.Missing;

            Load();

            Assert.AreEqual(DifficultyCurve.FirstLevel, _service.Current);
        }

        [Test]
        public void Load_WithSavedLevel_RestoresIt()
        {
            _saves.Behaviour = FakeSaveService.LoadBehaviour.ReturnsStored;
            _saves.Stored = new ColorStackSortSaveData { CurrentLevel = 12 };

            Load();

            Assert.AreEqual(12, _service.Current);
        }

        [Test]
        public void Load_WithCorruptSave_DoesNotThrow()
        {
            // Load-bearing: ColorStackSortBootstrap.StartAsync catches ONLY OperationCanceledException,
            // so anything else escaping here skips the state change and leaves a blank screen.
            _saves.Behaviour = FakeSaveService.LoadBehaviour.Corrupt;
            LogAssert.ignoreFailingMessages = true;

            Assert.DoesNotThrow(Load);
            Assert.AreEqual(DifficultyCurve.FirstLevel, _service.Current);
        }

        [Test]
        public void Load_WithCorruptSave_KeepsSavingEnabledSoTheFileGetsRepaired()
        {
            _saves.Behaviour = FakeSaveService.LoadBehaviour.Corrupt;
            LogAssert.ignoreFailingMessages = true;
            Load();

            _service.Advance();

            Assert.AreEqual(1, _saves.SaveCallCount);
        }

        [Test]
        public void Load_WithNewerSchemaSave_NeverWritesOverIt()
        {
            // The headline safety rule. A save from a newer build holds real progress; overwriting
            // it on the next level-up would destroy that, so saving is dead for the session.
            _saves.Behaviour = FakeSaveService.LoadBehaviour.NewerSchema;
            LogAssert.ignoreFailingMessages = true;
            Load();

            _service.Advance();
            _service.Advance();

            Assert.AreEqual(DifficultyCurve.FirstLevel + 2, _service.Current, "play continues");
            Assert.AreEqual(0, _saves.SaveCallCount, "but nothing may be written");
        }

        [Test]
        public void Advance_BeforeLoad_DoesNotWrite()
        {
            // Fail-closed. Nothing has been read yet, so nothing has earned the right to be
            // overwritten — the invariant belongs here, not to bootstrap ordering.
            _service.Advance();

            Assert.AreEqual(0, _saves.SaveCallCount);
        }

        [Test]
        public void Advance_AfterLoad_PersistsTheNewLevel()
        {
            Load();

            _service.Advance();

            Assert.AreEqual(1, _saves.SaveCallCount);
            Assert.AreEqual(DifficultyCurve.FirstLevel + 1, _saves.LastSaved.CurrentLevel);
        }

        [Test]
        public void Advance_UsesANonCancellableToken()
        {
            // A lifetime-bound token (e.g. Application.exitCancellationToken) would cancel the write
            // exactly at quit, when it most needs to land.
            Load();

            _service.Advance();

            Assert.IsFalse(_saves.LastSaveToken.CanBeCanceled);
        }

        [Test]
        public void Advance_Repeatedly_HandsTheSaveServiceOneMutableObject()
        {
            // JsonSaveService serializes inside its per-key semaphore and release order is not FIFO,
            // so per-write snapshots could land out of order. One shared instance means whichever
            // write wins still carries the newest level.
            Load();

            _service.Advance();
            var first = _saves.LastSaved;
            _service.Advance();

            Assert.AreSame(first, _saves.LastSaved);
            Assert.AreEqual(_service.Current, _saves.LastSaved.CurrentLevel);
        }

        [Test]
        public void Advance_WhenTheWriteFails_DoesNotThrowOrStopProgression()
        {
            Load();
            _saves.ThrowOnSave = true;
            LogAssert.ignoreFailingMessages = true;

            Assert.DoesNotThrow(() => _service.Advance());
            Assert.AreEqual(DifficultyCurve.FirstLevel + 1, _service.Current);
        }

        [Test]
        public void Load_WithLevelZero_ClampsToFirstLevel()
        {
            // Not only tampering: SaveEnvelopeCodec cannot detect an envelope holding a different
            // type and binds it to an all-default instance — which is exactly level 0.
            // DifficultyCurve.ForLevel throws below FirstLevel, so this would crash the first board.
            _saves.Behaviour = FakeSaveService.LoadBehaviour.ReturnsStored;
            _saves.Stored = new ColorStackSortSaveData { CurrentLevel = 0 };

            Load();

            Assert.AreEqual(DifficultyCurve.FirstLevel, _service.Current);
            Assert.DoesNotThrow(() => DifficultyCurve.ForLevel(_service.Current));
        }

        [Test]
        public void Load_WithNegativeLevel_ClampsToFirstLevel()
        {
            _saves.Behaviour = FakeSaveService.LoadBehaviour.ReturnsStored;
            _saves.Stored = new ColorStackSortSaveData { CurrentLevel = -5 };

            Load();

            Assert.AreEqual(DifficultyCurve.FirstLevel, _service.Current);
        }

        [Test]
        public void Advance_AtTheUpperClamp_CannotOverflowIntoNegativeLevels()
        {
            // int.MaxValue + 1 wraps negative, and ForLevel throws there — inside a state's
            // OnEnterAsync, which is deliberately unguarded.
            _saves.Behaviour = FakeSaveService.LoadBehaviour.ReturnsStored;
            _saves.Stored = new ColorStackSortSaveData { CurrentLevel = int.MaxValue };
            Load();

            _service.Advance();

            Assert.AreEqual(LevelProgressService.MaxLevel, _service.Current);
            Assert.DoesNotThrow(() => DifficultyCurve.ForLevel(_service.Current));
        }

        [Test]
        public void CurrentLevel_AfterAdvance_NotifiesSubscribers()
        {
            Load();
            var seen = 0;
            using var sub = _service.CurrentLevel.Subscribe(v => seen = v);

            _service.Advance();

            Assert.AreEqual(_service.Current, seen);
        }

        [Test]
        public void Load_WhenCancelled_LeavesSavingDisabled()
        {
            // The one LoadAsync path allowed to throw, and the one the fail-closed rule exists for:
            // nothing was read, so nothing has earned the right to be written over.
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.Throws<OperationCanceledException>(
                () => _service.LoadAsync(cts.Token).GetAwaiter().GetResult());

            _service.Advance();

            Assert.AreEqual(0, _saves.SaveCallCount);
        }

        [Test]
        public void SaveKey_MatchesTheKeyPatternTheServiceEnforces()
        {
            // JsonSaveService.ValidateKey runs BEFORE any await and throws ArgumentException on a
            // miss. That would land in LoadAsync's blanket catch, which leaves saving ENABLED — so
            // every later write faults too and progress silently never persists. Pinning the key
            // itself is the only cheap way to catch a rename that looks harmless.
            Assert.IsTrue(
                Regex.IsMatch(ColorStackSortSaveData.SaveKey, "^[A-Za-z0-9_-]+$"),
                $"'{ColorStackSortSaveData.SaveKey}' is not a legal save key.");
        }

        [Test]
        public void Load_WithStoredData_AdoptsTheInstanceSoFutureFieldsRoundTrip()
        {
            // Guards the whole-object adoption in LoadAsync. Copying CurrentLevel out instead would
            // pass every other test here and silently drop any field added to the POCO later.
            var stored = new ColorStackSortSaveData { CurrentLevel = 7 };
            _saves.Behaviour = FakeSaveService.LoadBehaviour.ReturnsStored;
            _saves.Stored = stored;
            Load();

            _service.Advance();

            Assert.AreSame(stored, _saves.LastSaved);
        }

        [TearDown]
        public void TearDown() => LogAssert.ignoreFailingMessages = false;
    }
}
