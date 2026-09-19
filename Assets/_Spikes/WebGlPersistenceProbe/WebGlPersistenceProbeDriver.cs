using System;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Spikes.WebGlPersistenceProbe
{
    /// <summary>
    /// Drives the persistence spike and renders results on-screen (plans/260919-1554, Phase 1).
    /// <para>
    /// Output goes through IMGUI on purpose: it needs no Canvas, no prefab and no TMP font asset, so
    /// the probe scene is a single GameObject with one component and nothing else can go wrong
    /// between "it built" and "I can read the numbers". The browser console is not enough — one of
    /// the target platforms is mobile Safari, where reading the console is impractical.
    /// </para>
    /// <para>
    /// The cross-session section is the point of the whole exercise. An in-session round-trip proves
    /// nothing about durability: the payload may still be sitting in a buffer that never reaches
    /// IndexedDB. Only a marker written by a PREVIOUS page load and still readable now proves the
    /// write actually landed.
    /// </para>
    /// </summary>
    public sealed class WebGlPersistenceProbeDriver : MonoBehaviour
    {
        private const string RunCountKey = "SpikeProbe_RunCount";
        private const string PrefsMarkerKey = "SpikeProbe_Marker";
        private const int WatchdogMs = 5000;

        private readonly StringBuilder _report = new();
        private Vector2 _scroll;
        private bool _finished;

        private string MarkerFilePath =>
            Path.Combine(Application.persistentDataPath, "SpikeProbe", "marker.txt");

        private void Start() => RunAsync().Forget();

        private async UniTaskVoid RunAsync()
        {
            var runCount = PlayerPrefs.GetInt(RunCountKey, 0) + 1;

            Line("=== WebGL PERSISTENCE SPIKE ===");
            Line($"run #{runCount}   unity {Application.unityVersion}   platform {Application.platform}");
            Line($"persistentDataPath: {Application.persistentDataPath}");
            Line("");

            ReportCrossSession(runCount);

            Line("--- THIS SESSION ---");
            Line($"P1 File.WriteAllText/ReadAllText : {PersistenceProbes.FileWriteRead()}");
            Line($"P2 File.Replace                  : {PersistenceProbes.FileReplace()}");
            Line($"P3 File.Move (2-arg)             : {PersistenceProbes.FileMove()}");
            Line($"P4 UniTask.RunOnThreadPool       : running, watchdog {WatchdogMs}ms...");
            Render();

            var threadPool = await PersistenceProbes.RunOnThreadPoolAsync(WatchdogMs);
            ReplaceLastLine($"P4 UniTask.RunOnThreadPool       : {threadPool}");

            Line($"P5 PlayerPrefs round-trip        : {PersistenceProbes.PlayerPrefsRoundTrip()}");
            Line($"P6 PlayerPrefs ceiling           : {PersistenceProbes.PlayerPrefsCeiling()}");
            Line($"P7 Directory.GetFiles(*.tmp)     : {PersistenceProbes.DirectoryGlob()}");
            Line("");

            WriteMarkers(runCount);

            // --- Phase 6: the REAL save stack, not raw File calls ---------------------------------
            Line("--- REAL SAVE STACK (JsonSaveService + LocalFileStorageBackend) ---");
            Line($"S1 Saves/ subdirectory create    : {SaveStackProbes.SavesSubdirectoryCreate()}");

            Line("S2 load written by PREVIOUS run  : reading...");
            Render();
            var previous = await SaveStackProbes.ReadPreviousAsync(WatchdogMs);
            ReplaceLastLine($"S2 load written by PREVIOUS run  : {previous}");

            Line("S3 slot isolation (0 vs 1)       : running...");
            Render();
            ReplaceLastLine($"S3 slot isolation (0 vs 1)       : {await SaveStackProbes.SlotIsolationAsync(WatchdogMs)}");

            Line("S4 write for the NEXT run        : writing...");
            Render();
            ReplaceLastLine($"S4 write for the NEXT run        : {await SaveStackProbes.WriteAsync(runCount, WatchdogMs)}");

            if (runCount > 1)
                Line("^ S2 must name run #" + (runCount - 1) + ". <none> there means the real stack is NOT durable.");
            Line("");

            Line("--- NEXT STEP (manual) ---");
            Line("1. RELOAD this page  -> the CROSS-SESSION block above should show run #" + runCount);
            Line("2. CLOSE THE TAB immediately after load, then reopen. Tab close is the real");
            Line("   loss path; reload alone does not prove durability.");
            Line("3. Repeat in Safari, and in Safari private mode.");

            _finished = true;
            Render();
        }

        /// <summary>
        /// The durability verdict. Both mechanisms are reported separately because they can differ:
        /// the file may survive while PlayerPrefs does not, or the reverse, and which one survives
        /// decides the backend choice in Phase 2.
        /// </summary>
        private void ReportCrossSession(int runCount)
        {
            Line("--- CROSS-SESSION (written by a PREVIOUS page load) ---");

            var prefsMarker = PlayerPrefs.GetString(PrefsMarkerKey, null);
            Line(string.IsNullOrEmpty(prefsMarker)
                ? "PlayerPrefs marker : <none>  (first run, or PlayerPrefs did not survive)"
                : $"PlayerPrefs marker : {prefsMarker}");

            try
            {
                var fileMarker = File.Exists(MarkerFilePath) ? File.ReadAllText(MarkerFilePath) : null;
                Line(string.IsNullOrEmpty(fileMarker)
                    ? "File marker        : <none>  (first run, or the file did not survive)"
                    : $"File marker        : {fileMarker}");
            }
            catch (Exception ex)
            {
                Line($"File marker        : THREW {ex.GetType().Name}: {ex.Message}");
            }

            if (runCount > 1)
                Line("^ both should name run #" + (runCount - 1) + ". A <none> here means that mechanism is NOT durable.");

            Line("");
        }

        private void WriteMarkers(int runCount)
        {
            var stamp = $"run #{runCount} at {DateTime.UtcNow:HH:mm:ss}Z";

            PlayerPrefs.SetInt(RunCountKey, runCount);
            PlayerPrefs.SetString(PrefsMarkerKey, stamp);
            PlayerPrefs.Save();

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(MarkerFilePath)!);
                File.WriteAllText(MarkerFilePath, stamp);
                Line($"markers written    : {stamp}");
            }
            catch (Exception ex)
            {
                Line($"markers written    : PlayerPrefs only — file marker THREW {ex.GetType().Name}: {ex.Message}");
            }
        }

        private void Line(string text)
        {
            _report.AppendLine(text);
            Debug.Log("[Spike] " + text);
        }

        private void ReplaceLastLine(string text)
        {
            var all = _report.ToString();
            var lastBreak = all.TrimEnd('\r', '\n').LastIndexOf('\n');
            _report.Clear();
            if (lastBreak >= 0) _report.Append(all.Substring(0, lastBreak + 1));
            _report.AppendLine(text);
            Debug.Log("[Spike] " + text);
        }

        private void Render() { /* IMGUI repaints itself; kept for call-site readability */ }

        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                wordWrap = true,
                richText = false
            };

            GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, Screen.height - 20));
            GUILayout.Label(_finished ? "SPIKE COMPLETE" : "SPIKE RUNNING...", new GUIStyle(GUI.skin.label) { fontSize = 22 });
            _scroll = GUILayout.BeginScrollView(_scroll);
            GUILayout.Label(_report.ToString(), style);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
