using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Sinkii09.UIFramework;
using UnityEngine;

namespace Spikes.WebGlPersistenceProbe
{
    /// <summary>
    /// Phase 6 verification: exercises the REAL save stack on WebGL, not raw File calls.
    /// <para>
    /// Phase 1's probes proved the primitives work on IDBFS. They did not prove that
    /// <c>JsonSaveService</c> + <c>LocalFileStorageBackend</c> work end to end, and they wrote to the
    /// root of <c>persistentDataPath</c> while the real backend writes into a <c>Saves/</c>
    /// subdirectory — so <c>Directory.CreateDirectory</c> on IDBFS was never actually measured.
    /// </para>
    /// <para>
    /// Every call here races a watchdog. The WebGL failure this whole sprint exists to fix is a hang
    /// with <b>no exception</b>: a probe that only caught exceptions would sit there forever looking
    /// like it was still working.
    /// </para>
    /// </summary>
    internal static class SaveStackProbes
    {
        internal const string SaveKey = "SpikeSaveStack";

        [Serializable]
        internal sealed class SpikeSave
        {
            public string Stamp { get; set; }
            public int RunNumber { get; set; }
        }

        internal static string SavesSubdirectoryCreate()
        {
            // The gap Phase 1 left open. LocalFileStorageBackend does this on every write.
            try
            {
                var dir = Path.Combine(Application.persistentDataPath, "Saves");
                Directory.CreateDirectory(dir);
                return Directory.Exists(dir) ? $"OK ({dir})" : "FAILED (no exception, but the directory is not there)";
            }
            catch (Exception ex)
            {
                return $"THREW {ex.GetType().Name}: {ex.Message}";
            }
        }

        /// <summary>Reads whatever a previous page load saved. This is the durability verdict.</summary>
        internal static async UniTask<string> ReadPreviousAsync(int watchdogMs)
        {
            return await RaceAsync(watchdogMs, async () =>
            {
                var service = new JsonSaveService(new LocalFileStorageBackend());
                var loaded = await service.LoadAsync<SpikeSave>(SaveKey);
                return loaded == null
                    ? "<none>  (first run, or the save did not survive)"
                    : $"run #{loaded.RunNumber} at {loaded.Stamp}";
            });
        }

        internal static async UniTask<string> WriteAsync(int runNumber, int watchdogMs)
        {
            return await RaceAsync(watchdogMs, async () =>
            {
                var service = new JsonSaveService(new LocalFileStorageBackend());
                await service.SaveAsync(SaveKey, new SpikeSave
                {
                    Stamp = DateTime.UtcNow.ToString("HH:mm:ssZ"),
                    RunNumber = runNumber
                });
                return "OK (written through the real save stack)";
            });
        }

        /// <summary>Slot 1 must not be visible from slot 0, through the real service.</summary>
        internal static async UniTask<string> SlotIsolationAsync(int watchdogMs)
        {
            return await RaceAsync(watchdogMs, async () =>
            {
                var slots = new SaveSlotContext();
                var service = new JsonSaveService(new LocalFileStorageBackend(), SaveMigrationRegistry.Empty, slots);
                const string key = "SpikeSlotProbe";

                slots.ActiveSlot = 1;
                await service.SaveAsync(key, new SpikeSave { Stamp = "slot-one", RunNumber = 1 });

                slots.ActiveSlot = 0;
                var fromSlotZero = await service.LoadAsync<SpikeSave>(key);

                slots.ActiveSlot = 1;
                var fromSlotOne = await service.LoadAsync<SpikeSave>(key);

                await service.DeleteAsync(key);

                if (fromSlotZero != null)
                    return "FAILED (slot 0 could see slot 1's save)";

                return fromSlotOne?.Stamp == "slot-one"
                    ? "OK (isolated; slot 1 round-tripped)"
                    : "FAILED (slot 1 did not round-trip)";
            });
        }

        // Completion within a bound, never "did it throw". On WebGL the pre-fix failure mode was
        // UniTask.RunOnThreadPool never resuming: no exception, no log, no terminal event.
        private static async UniTask<string> RaceAsync(int watchdogMs, Func<UniTask<string>> work)
        {
            try
            {
                var timeout = UniTask.Delay(watchdogMs, DelayType.Realtime)
                    .ContinueWith(() => $"HUNG — nothing completed within {watchdogMs}ms (no exception thrown)");
                var (winner, result, timedOut) = await UniTask.WhenAny(work(), timeout);
                return winner == 0 ? result : timedOut;
            }
            catch (Exception ex)
            {
                return $"THREW {ex.GetType().Name}: {ex.Message}";
            }
        }
    }
}
