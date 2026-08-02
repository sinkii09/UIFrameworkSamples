using System;
using System.Threading;
using ColorStackSort.Logic;
using Cysharp.Threading.Tasks;
using R3;
using Sinkii09.UIFramework;
using UnityEngine;
using VContainer;

namespace ColorStackSort
{
    /// <summary>
    /// Which level the player is on, persisted between sessions via <see cref="ISaveService"/>.
    /// <para>
    /// Deliberately the one piece of feature state that outlives a view: <see cref="BoardViewModel"/>
    /// is rebuilt on every show (the factory resets its scope and bindings), so a level index held
    /// there would reset itself on every restart.
    /// </para>
    /// <para>
    /// Registered <c>Lifetime.Singleton</c>, never <c>Scoped</c>. Each view gets its own child
    /// container, so a scoped registration would hand the board and the win panel two different
    /// services and progression would never advance.
    /// </para>
    /// </summary>
    // Fully qualified: VContainer ships its own PreserveAttribute, so the short name is ambiguous
    // in files that also use VContainer.
    [UnityEngine.Scripting.Preserve]
    public sealed class LevelProgressService
    {
        /// <summary>
        /// Guards against a tampered or garbage save incrementing into negative territory, which
        /// would throw inside <c>ColorStackSortGameplayState.OnEnterAsync</c> — a path deliberately
        /// left unguarded so real failures surface.
        /// </summary>
        public const int MaxLevel = 1_000_000;

        private readonly ISaveService _saveService;

        // One instance, mutated in place, never re-snapshotted per write. JsonSaveService serializes
        // INSIDE its per-key semaphore and SemaphoreSlim release order is not FIFO, so two
        // overlapping writes carrying separate snapshots could persist level 2 after level 3.
        // Sharing one object means whichever write wins still carries the newest value.
        //
        // Not readonly because LoadAsync ADOPTS the deserialized instance. That reassignment happens
        // once, before any save can be issued, so the single-shared-instance rule above still holds.
        private ColorStackSortSaveData _data = new() { CurrentLevel = DifficultyCurve.FirstLevel };

        private readonly ReactiveProperty<int> _currentLevel = new(DifficultyCurve.FirstLevel);

        // Fail-closed: starts disabled and is only opened once LoadAsync has actually run. The
        // "never clobber a save written by a newer build" rule then belongs to this class rather
        // than to bootstrap ordering, so a cancelled or skipped load cannot leave a window where
        // Advance() overwrites a file it never read.
        private bool _savingDisabled = true;

        [Inject]
        public LevelProgressService(ISaveService saveService) => _saveService = saveService;

        public ReadOnlyReactiveProperty<int> CurrentLevel => _currentLevel;

        public int Current => _currentLevel.Value;

        /// <summary>
        /// Restores the saved level. Must be awaited before the first board is built.
        /// <para>
        /// <b>Never throws</b> except <see cref="OperationCanceledException"/>. The bootstrap that
        /// calls this catches only cancellation, so any other exception escaping here would skip the
        /// state change entirely and leave the game on a blank screen — the exact opposite of the
        /// graceful degradation this method exists to provide.
        /// </para>
        /// </summary>
        public async UniTask LoadAsync(CancellationToken ct = default)
        {
            try
            {
                var saved = await _saveService.LoadAsync<ColorStackSortSaveData>(
                    ColorStackSortSaveData.SaveKey, ct);

                // null means exactly one thing: no save yet. Anything present but unreadable throws
                // (the service already tried the backup), so a first run is not an error path.
                //
                // Adopt the whole object rather than copying CurrentLevel out of it. With a single
                // field the two are identical; the moment a second field is added, copying would
                // read it, drop it, and let the next Advance() write the default straight back over
                // it — silent, permanent, and with no diagnostic anywhere.
                if (saved != null) _data = saved;
                SetLevel(_data.CurrentLevel);
                _savingDisabled = false;
            }
            catch (OperationCanceledException)
            {
                // Shutting down mid-boot. Leave saving disabled — nothing has been read, so nothing
                // has earned the right to be written over.
                throw;
            }
            catch (SaveSchemaVersionException ex)
            {
                // Written by a NEWER build of the game. Starting fresh is fine; overwriting is not —
                // the next level-up would destroy real progress. Saving stays off for this session.
                // This is why the framework gives the case its own exception type.
                Debug.LogError($"[LevelProgressService] {ex.Message} Progress will NOT be saved this " +
                               "session, so the newer save is left intact.");
                SetLevel(DifficultyCurve.FirstLevel);
            }
            catch (Exception ex)
            {
                // Corrupt beyond backup recovery. Start over but keep saving enabled, so the next
                // level-up repairs the file rather than leaving it broken forever.
                Debug.LogError($"[LevelProgressService] Could not read saved progress, starting from " +
                               $"level {DifficultyCurve.FirstLevel}: {ex}");
                SetLevel(DifficultyCurve.FirstLevel);
                _savingDisabled = false;
            }
        }

        public void Advance()
        {
            SetLevel(_currentLevel.Value + 1);

            if (_savingDisabled) return;

            // CancellationToken.None, deliberately. A lifetime-bound token such as
            // Application.exitCancellationToken would cancel the write exactly when it most needs to
            // land; JsonSaveService.Dispose declines to abort in-flight saves for the same reason.
            _saveService.SaveAsync(ColorStackSortSaveData.SaveKey, _data, CancellationToken.None)
                .Forget(LogSaveFailure);
        }

        /// <summary>
        /// Clamps both ends. The low end is not only about tampering: <c>SaveEnvelopeCodec</c>
        /// cannot detect an envelope holding a different type and binds it to an all-default
        /// instance, i.e. level 0 — which would throw in <see cref="DifficultyCurve.ForLevel"/>.
        /// </summary>
        private void SetLevel(int level)
        {
            var clamped = Mathf.Clamp(level, DifficultyCurve.FirstLevel, MaxLevel);
            _data.CurrentLevel = clamped;
            _currentLevel.Value = clamped;
        }

        /// <summary>
        /// The single log site for save failures. Deliberately not combined with the service's
        /// <c>OnSaveFailed</c> observable, which would report the same failure twice.
        /// </summary>
        private static void LogSaveFailure(Exception ex)
        {
            if (ex is OperationCanceledException) return;

            Debug.LogError($"[LevelProgressService] Failed to save progress: {ex}");
        }
    }
}
