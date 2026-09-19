using System;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Spikes.WebGlPersistenceProbe
{
    /// <summary>
    /// Individual platform probes for the persistence redesign spike (plans/260919-1554).
    /// <para>
    /// Every probe answers exactly one question and reports the raw outcome — exception type and
    /// message verbatim, never an interpretation. A probe that HANGS must be distinguishable from
    /// one that threw, which is why the thread-pool probe races a watchdog instead of awaiting
    /// directly: on a platform without threads the await may simply never resume.
    /// </para>
    /// </summary>
    internal static class PersistenceProbes
    {
        private const string Dir = "SpikeProbe";

        private static string Root => Path.Combine(Application.persistentDataPath, Dir);

        /// <summary>P1 — the most basic question: does System.IO work at all here?</summary>
        internal static string FileWriteRead()
        {
            return Guarded(() =>
            {
                Directory.CreateDirectory(Root);
                var path = Path.Combine(Root, "p1.json");
                var payload = "{\"probe\":1,\"stamp\":\"" + DateTime.UtcNow.Ticks + "\"}";
                File.WriteAllText(path, payload);
                var read = File.ReadAllText(path);
                return read == payload ? "OK (round-trip matched)" : $"MISMATCH wrote {payload.Length}B read {read?.Length ?? -1}B";
            });
        }

        /// <summary>
        /// P2 — the atomic-write primitive LocalFileStorageBackend depends on. Highest suspicion:
        /// an atomic three-file replace is the least likely POSIX corner to exist on IDBFS.
        /// </summary>
        internal static string FileReplace()
        {
            return Guarded(() =>
            {
                Directory.CreateDirectory(Root);
                var final = Path.Combine(Root, "p2.json");
                var temp = final + ".tmp";
                var backup = final + ".bak";

                File.WriteAllText(final, "OLD");
                File.WriteAllText(temp, "NEW");
                File.Replace(temp, final, backup, ignoreMetadataErrors: true);

                var f = File.ReadAllText(final);
                var b = File.Exists(backup) ? File.ReadAllText(backup) : "<no backup>";
                return $"OK final='{f}' backup='{b}'";
            });
        }

        /// <summary>P3 — the first-save branch, which uses 2-arg Move rather than Replace.</summary>
        internal static string FileMove()
        {
            return Guarded(() =>
            {
                Directory.CreateDirectory(Root);
                var final = Path.Combine(Root, "p3.json");
                var temp = final + ".tmp";
                if (File.Exists(final)) File.Delete(final);
                File.WriteAllText(temp, "MOVED");
                File.Move(temp, final);
                return $"OK final='{File.ReadAllText(final)}'";
            });
        }

        /// <summary>
        /// P4 — all six I/O paths in LocalFileStorageBackend sit on this. Raced against a watchdog
        /// because "never resumes" is a real and likely outcome here, and an un-raced await would
        /// simply hang the probe with no output at all.
        /// </summary>
        internal static async UniTask<string> RunOnThreadPoolAsync(int watchdogMs)
        {
            try
            {
                var work = UniTask.RunOnThreadPool(() => 42);
                var timeout = UniTask.Delay(watchdogMs, DelayType.Realtime);
                var (winner, result, _) = await UniTask.WhenAny(work, timeout.ContinueWith(() => -1));

                if (winner == 0 && result == 42) return "OK (completed on a pool thread)";
                return $"HUNG — no completion within {watchdogMs}ms (winner={winner})";
            }
            catch (Exception ex)
            {
                return $"THREW {ex.GetType().Name}: {ex.Message}";
            }
        }

        /// <summary>P5 — PlayerPrefs basic round-trip plus an explicit Save().</summary>
        internal static string PlayerPrefsRoundTrip()
        {
            return Guarded(() =>
            {
                const string key = "SpikeProbe_P5";
                var payload = "stamp:" + DateTime.UtcNow.Ticks;
                PlayerPrefs.SetString(key, payload);
                PlayerPrefs.Save();
                var read = PlayerPrefs.GetString(key, "<missing>");
                return read == payload ? "OK (round-trip matched, Save() returned)" : $"MISMATCH read='{read}'";
            });
        }

        /// <summary>
        /// P6 — the size ceiling, and whether exceeding it fails loudly or SILENTLY. A silent
        /// truncation is the worst outcome for the framework: every layer above would believe the
        /// save landed. Escalates until a step fails rather than assuming the documented ~1MB.
        /// </summary>
        internal static string PlayerPrefsCeiling()
        {
            var sizes = new[] { 1_024, 16_384, 131_072, 524_288, 1_048_576, 2_097_152 };
            var report = new StringBuilder();
            var largestOk = 0;

            foreach (var size in sizes)
            {
                const string key = "SpikeProbe_P6";
                try
                {
                    var payload = new string('x', size);
                    PlayerPrefs.SetString(key, payload);
                    PlayerPrefs.Save();
                    var read = PlayerPrefs.GetString(key, string.Empty);

                    if (read.Length == size) largestOk = size;
                    else
                    {
                        report.Append($"[{size}B -> read back {read.Length}B SILENT TRUNCATION] ");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    report.Append($"[{size}B -> THREW {ex.GetType().Name}] ");
                    break;
                }
            }

            PlayerPrefs.DeleteKey("SpikeProbe_P6");
            PlayerPrefs.Save();
            return $"largest verified OK: {largestOk}B. {report}";
        }

        /// <summary>P7 — the orphan-temp sweep in LocalFileStorageBackend uses this glob.</summary>
        internal static string DirectoryGlob()
        {
            return Guarded(() =>
            {
                Directory.CreateDirectory(Root);
                File.WriteAllText(Path.Combine(Root, "sweep.json.tmp"), "x");
                var hits = Directory.GetFiles(Root, "*.tmp");
                return $"OK matched {hits.Length} file(s)";
            });
        }

        private static string Guarded(Func<string> probe)
        {
            try { return probe(); }
            catch (Exception ex) { return $"THREW {ex.GetType().Name}: {ex.Message}"; }
        }
    }
}
